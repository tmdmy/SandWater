#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SandWater.Player;
using SandWater.TerrainSystem;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class Phase4SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase4SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string TerrainDataPath = "Assets/Settings/MainTerrainData.asset";
        private const string SandLayerPath = "Assets/TerrainSampleAssets/TerrainLayers/Sand_TerrainLayer.terrainlayer";

        private const int HeightmapResolution = 257;
        private const float TerrainWidth = 100f;
        private const float TerrainLength = 100f;
        private const float TerrainMaxHeight = 30f;
        private const float InitialNormalizedHeight = 0.3f;

        static Phase4SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Phase3SetupTool.SetupMainScene();
            SetupMainTerrain();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Reset Main Terrain Data")]
        public static void ResetMainTerrainData()
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                Debug.LogWarning("[SandWater] MainTerrainData.asset not found. Run Setup Phase 4 Terrain first.");
                return;
            }

            int res = data.heightmapResolution;
            float[,] heights = new float[res, res];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                    heights[z, x] = InitialNormalizedHeight;
            data.SetHeights(0, 0, heights);

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Main terrain data reset to baseline.");
        }

        [MenuItem("SandWater/Setup Phase 4 Terrain")]
        public static void SetupMainTerrain()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<TerrainEditor>() != null)
            {
                Debug.Log("[SandWater] Phase 4 terrain already set up.");
                return;
            }

            RemoveExistingGround(scene);

            TerrainData data = LoadOrCreateTerrainData();

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            SceneManager.MoveGameObjectToScene(terrainGo, scene);
            terrainGo.transform.position = new Vector3(-TerrainWidth * 0.5f, 0f, -TerrainLength * 0.5f);

            var terrain = terrainGo.GetComponent<Terrain>();

            var systemGo = new GameObject("[TerrainSystem]");
            SceneManager.MoveGameObjectToScene(systemGo, scene);
            var brush = systemGo.AddComponent<TerrainBrush>();
            var editor = systemGo.AddComponent<TerrainEditor>();
            var resetter = systemGo.AddComponent<TerrainResetter>();
            var line = systemGo.AddComponent<LineRenderer>();
            var indicator = systemGo.AddComponent<BrushIndicator>();

            AssignSerializedRef(editor, "targetTerrain", terrain);
            AssignSerializedRef(editor, "brush", brush);
            AssignSerializedRef(resetter, "targetTerrain", terrain);
            AssignSerializedRef(indicator, "targetTerrain", terrain);
            AssignSerializedRef(indicator, "brush", brush);

            RelocatePlayerOnTerrain(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 4 terrain setup complete.");
        }

        private static void RemoveExistingGround(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Ground")
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static TerrainData LoadOrCreateTerrainData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (existing != null) return existing;

            var data = new TerrainData();
            data.heightmapResolution = HeightmapResolution;
            data.baseMapResolution = 512;
            data.SetDetailResolution(512, 16);
            data.size = new Vector3(TerrainWidth, TerrainMaxHeight, TerrainLength);

            int res = data.heightmapResolution;
            float[,] heights = new float[res, res];
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    heights[z, x] = InitialNormalizedHeight;
                }
            }
            data.SetHeights(0, 0, heights);

            var sandLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(SandLayerPath);
            if (sandLayer != null)
            {
                data.terrainLayers = new[] { sandLayer };
            }

            AssetDatabase.CreateAsset(data, TerrainDataPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static void RelocatePlayerOnTerrain(Scene scene)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            float surfaceY = TerrainMaxHeight * InitialNormalizedHeight + 0.5f;
            player.transform.position = new Vector3(0f, surfaceY, 0f);
        }

        private static void AssignSerializedRef(Object target, string propertyName, Object reference)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null) return;
            prop.objectReferenceValue = reference;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
