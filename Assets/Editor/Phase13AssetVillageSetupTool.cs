#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SandWater.WaterSim;
using SandWater.WitheredVillage;

namespace SandWater.EditorTools
{
    /// <summary>
    /// Phase 13: replaces Phase 12's primitive nature decorations with prefabs from the
    /// Pandazole - Nature Environment Low Poly Pack. Withered prefabs (TreeTrunk / Bones /
    /// Thorns) are placed visibly; alive prefabs (Bush / Cactus / Flower / Grass / Tree-Spring)
    /// are hidden and revealed by VillageRevival when the goal is reached.
    ///
    /// Does NOT touch the well or houses — those remain as Phase 12 primitives.
    /// </summary>
    public static class Phase13AssetVillageSetupTool
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string PandazoleFolder = "Assets/Pandazole_Ultimate_Pack/Pandazole Nature Environment Pack/Prefabs";
        private const string VillageGoName = "[Village]";
        private const string DecorationsName = "[NatureDecorations]";
        private const string WastesGoName = "[Wastes]";

        // Withered (always visible) — wasteland markers
        private static GameObject[] _bonePrefabs;
        private static GameObject[] _thornsPrefabs;
        private static GameObject[] _trunkPrefabs;
        // Alive (hidden until revival) — lush oasis
        private static GameObject[] _bushPrefabs;
        private static GameObject[] _cactusPrefabs;
        private static GameObject[] _flowerPrefabs;
        private static GameObject[] _grassPrefabs;
        private static GameObject[] _aliveTreePrefabs;

        [MenuItem("SandWater/Setup Phase 13 Asset Village")]
        public static void SetupAssetVillage()
        {
            if (!LoadPandazolePrefabs())
            {
                Debug.LogError($"[SandWater] Phase 13: Pandazole prefab folder not found at '{PandazoleFolder}'. Import the pack first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            var goal = Object.FindAnyObjectByType<GoalZone>();
            if (goal == null)
            {
                Debug.LogError("[SandWater] Phase 13: GoalZone not found in Main scene.");
                return;
            }
            var source = Object.FindAnyObjectByType<WaterSource>();
            var terrain = Object.FindAnyObjectByType<Terrain>();

            // Strip previous Phase 13 decorations (idempotent re-run) AND any stale Phase 12 leftovers.
            StripPreviousDecorations();

            // Re-find / create the [Village] root container; houses inside are kept untouched.
            var village = GameObject.Find(VillageGoName);
            if (village == null) village = new GameObject(VillageGoName);

            // Container for all Phase-13-managed decorations.
            var decorations = new GameObject(DecorationsName);
            decorations.transform.SetParent(village.transform, false);

            var hidden = new List<GameObject>();
            var withered = new List<GameObject>();

            // Compute orientation: place wasteland items along the path between source and goal.
            Vector3 goalPos = goal.transform.position;
            Vector3 sourcePos = source != null ? source.transform.position : goalPos + Vector3.left * 20f;
            Vector3 toSource = sourcePos - goalPos;
            toSource.y = 0f;
            if (toSource.sqrMagnitude < 0.01f) toSource = Vector3.left;
            toSource.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, toSource);

            // ===================================================================
            // WITHERED layer — visible from start (wasteland feel)
            // ===================================================================

            // Withered tree trunks scattered widely around the village.
            ScatterPrefabs(decorations.transform, _trunkPrefabs, goalPos, terrain,
                count: 6, minRadius: 4f, maxRadius: 10f, scaleMin: 0.7f, scaleMax: 1.1f, collected: withered);

            // Bones around the source (death scene)
            ScatterPrefabs(decorations.transform, _bonePrefabs, sourcePos, terrain,
                count: 5, minRadius: 1.5f, maxRadius: 5f, scaleMin: 0.8f, scaleMax: 1.2f, collected: withered);

            // Thorns along the path between source and goal.
            for (int i = 0; i < 14; i++)
            {
                float t = Random.Range(0.15f, 0.85f);
                Vector3 p = Vector3.Lerp(goalPos, sourcePos, t) + right * Random.Range(-5f, 5f);
                p.y = SampleTerrainY(terrain, p);
                var thorn = InstantiatePrefab(decorations.transform, _thornsPrefabs, p, scaleMin: 0.7f, scaleMax: 1.2f);
                if (thorn != null) withered.Add(thorn);
            }

            // ===================================================================
            // ALIVE layer — hidden, revealed when revival fires
            // ===================================================================

            // Bushes around the well (lush ring once revived)
            ScatterPrefabs(decorations.transform, _bushPrefabs, goalPos, terrain,
                count: 12, minRadius: 2.5f, maxRadius: 7f, scaleMin: 0.7f, scaleMax: 1.3f, hidden: hidden);

            // Grass scattered widely (becomes a green carpet on revival)
            ScatterPrefabs(decorations.transform, _grassPrefabs, goalPos, terrain,
                count: 25, minRadius: 2f, maxRadius: 10f, scaleMin: 0.7f, scaleMax: 1.3f, hidden: hidden);

            // Flowers around the well and along the path
            ScatterPrefabs(decorations.transform, _flowerPrefabs, goalPos, terrain,
                count: 18, minRadius: 1.5f, maxRadius: 8f, scaleMin: 0.7f, scaleMax: 1.2f, hidden: hidden);

            // Cacti (desert flair — alive plants suited to revived oasis)
            ScatterPrefabs(decorations.transform, _cactusPrefabs, sourcePos, terrain,
                count: 5, minRadius: 1.5f, maxRadius: 6f, scaleMin: 0.8f, scaleMax: 1.3f, hidden: hidden);
            ScatterPrefabs(decorations.transform, _cactusPrefabs, goalPos, terrain,
                count: 3, minRadius: 4f, maxRadius: 8f, scaleMin: 0.8f, scaleMax: 1.2f, hidden: hidden);

            // Alive trees: standing among the dead trunks for visual contrast on revival
            ScatterPrefabs(decorations.transform, _aliveTreePrefabs, goalPos, terrain,
                count: 4, minRadius: 4f, maxRadius: 8f, scaleMin: 0.9f, scaleMax: 1.3f, hidden: hidden);

            // ===================================================================
            // Wire VillageRevival — alive prefabs activate, withered prefabs deactivate.
            // ===================================================================
            WireRevival(goal, village.transform, hidden, withered);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SandWater] Phase 13 asset village setup complete. Withered visible, {hidden.Count} alive prefabs hidden until revival.");
        }

        // ---------------- Pandazole prefab loading ----------------

        private static bool LoadPandazolePrefabs()
        {
            if (!AssetDatabase.IsValidFolder(PandazoleFolder)) return false;

            _bonePrefabs    = LoadByPrefix("Bones");
            _thornsPrefabs  = LoadByPrefix("Thorns");
            _trunkPrefabs   = LoadByPrefix("TreeTrunk");
            _bushPrefabs    = LoadByPrefix("Bush");
            _cactusPrefabs  = LoadByPrefix("Cactus"); // includes CactusFlower_*
            _flowerPrefabs  = LoadByPrefix("Flower");
            _grassPrefabs   = LoadByPrefix("Grass");
            _aliveTreePrefabs = LoadByPattern(n => n.IndexOf("Spring", System.StringComparison.OrdinalIgnoreCase) >= 0);

            int total = _bonePrefabs.Length + _thornsPrefabs.Length + _trunkPrefabs.Length +
                        _bushPrefabs.Length + _cactusPrefabs.Length + _flowerPrefabs.Length +
                        _grassPrefabs.Length + _aliveTreePrefabs.Length;
            return total > 0;
        }

        private static GameObject[] LoadByPrefix(string prefix)
        {
            return LoadByPattern(n => n.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase));
        }

        private static GameObject[] LoadByPattern(System.Func<string, bool> matcher)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PandazoleFolder });
            var results = new List<GameObject>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (matcher(name))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) results.Add(prefab);
                }
            }
            return results.ToArray();
        }

        // ---------------- Stripping ----------------

        private static void StripPreviousDecorations()
        {
            var village = GameObject.Find(VillageGoName);
            if (village != null)
            {
                var deco = village.transform.Find(DecorationsName);
                if (deco != null) Object.DestroyImmediate(deco.gameObject);
            }
            var wastes = GameObject.Find(WastesGoName);
            if (wastes != null) Object.DestroyImmediate(wastes);

            var goal = Object.FindAnyObjectByType<GoalZone>();
            if (goal != null)
            {
                var revival = goal.GetComponent<VillageRevival>();
                if (revival != null) Object.DestroyImmediate(revival);
            }
        }

        // ---------------- Placement helpers ----------------

        private static void ScatterPrefabs(Transform parent, GameObject[] prefabs, Vector3 center, Terrain terrain,
                                           int count, float minRadius, float maxRadius,
                                           float scaleMin, float scaleMax,
                                           List<GameObject> hidden = null,
                                           List<GameObject> collected = null)
        {
            if (prefabs == null || prefabs.Length == 0) return;
            for (int i = 0; i < count; i++)
            {
                float angle = ((i + Random.value) / count) * Mathf.PI * 2f;
                float r = Random.Range(minRadius, maxRadius);
                Vector3 p = center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                p.y = SampleTerrainY(terrain, p);
                var go = InstantiatePrefab(parent, prefabs, p, scaleMin, scaleMax);
                if (go == null) continue;
                if (hidden != null) hidden.Add(go);
                if (collected != null) collected.Add(go);
            }
        }

        private static GameObject InstantiatePrefab(Transform parent, GameObject[] prefabs, Vector3 worldPos,
                                                    float scaleMin, float scaleMax)
        {
            if (prefabs == null || prefabs.Length == 0) return null;
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (go == null) return null;
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float s = Random.Range(scaleMin, scaleMax);
            go.transform.localScale = new Vector3(s, s, s);
            return go;
        }

        private static float SampleTerrainY(Terrain terrain, Vector3 worldPos)
        {
            if (terrain == null) return worldPos.y;
            return terrain.SampleHeight(new Vector3(worldPos.x, 0f, worldPos.z)) + terrain.transform.position.y;
        }

        // ---------------- Revival wiring ----------------

        private static void WireRevival(GoalZone goal, Transform villageRoot,
                                        List<GameObject> hidden, List<GameObject> withered)
        {
            var revival = goal.GetComponent<VillageRevival>();
            if (revival == null) revival = goal.gameObject.AddComponent<VillageRevival>();

            var so = new SerializedObject(revival);

            var goalProp = so.FindProperty("goalZone");
            if (goalProp != null) goalProp.objectReferenceValue = goal;

            // Phase 13 doesn't swap materials — prefabs themselves are the swap.
            var witheredProp = so.FindProperty("witheredMaterial");
            if (witheredProp != null) witheredProp.objectReferenceValue = null;
            var aliveProp = so.FindProperty("aliveMaterial");
            if (aliveProp != null) aliveProp.objectReferenceValue = null;

            var rootsProp = so.FindProperty("revivableRoots");
            if (rootsProp != null)
            {
                rootsProp.arraySize = villageRoot != null ? 1 : 0;
                if (villageRoot != null) rootsProp.GetArrayElementAtIndex(0).objectReferenceValue = villageRoot;
            }

            var hiddenProp = so.FindProperty("hiddenOnStart");
            if (hiddenProp != null)
            {
                hiddenProp.arraySize = hidden.Count;
                for (int i = 0; i < hidden.Count; i++)
                {
                    hiddenProp.GetArrayElementAtIndex(i).objectReferenceValue = hidden[i];
                }
            }

            var hideProp = so.FindProperty("hideOnRevival");
            if (hideProp != null)
            {
                hideProp.arraySize = withered.Count;
                for (int i = 0; i < withered.Count; i++)
                {
                    hideProp.GetArrayElementAtIndex(i).objectReferenceValue = withered[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
