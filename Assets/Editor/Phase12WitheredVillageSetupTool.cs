#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SandWater.WaterSim;
using SandWater.WitheredVillage;

namespace SandWater.EditorTools
{
    /// <summary>
    /// Phase 12 (experiment branch only): replaces the abstract green-cylinder goal with a
    /// thematic "withered desert village" — a stone well at the goal that fills as water arrives,
    /// surrounded by simple houses, dead trees, and withered crop patches built from primitives.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase12WitheredVillageSetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase12WitheredVillageSetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        private const string WellChildName = "Well";
        private const string VillageGoName = "[Village]";
        private const string WastesGoName = "[Wastes]";
        private const string ExistingGoalVisualName = "GoalVisual";

        // Reusable shared materials, created once per setup invocation.
        private static Material _matStone;
        private static Material _matWood;
        private static Material _matWater;
        private static Material _matWallA;
        private static Material _matWallB;
        private static Material _matRoofRed;
        private static Material _matDeadBark;
        private static Material _matWithered;
        private static Material _matCrackedGround;

        static Phase12WitheredVillageSetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SetupVillage();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 12 Withered Village")]
        public static void SetupVillage()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            CreateSharedMaterials();

            var goal = Object.FindAnyObjectByType<GoalZone>();
            if (goal == null)
            {
                Debug.LogWarning("[SandWater] Phase 12: GoalZone not found. Run Phase 6 first.");
                return;
            }

            HideExistingGoalVisual(goal.transform);
            BuildWellAt(goal);

            var source = Object.FindAnyObjectByType<WaterSource>();
            BuildVillageAround(goal.transform.position, source != null ? source.transform.position : Vector3.zero);
            BuildWastesNear(source);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 12 withered village setup complete.");
        }

        [MenuItem("SandWater/Reset Phase 12 Setup Flag")]
        public static void ResetFlag()
        {
            EditorPrefs.DeleteKey(SetupFlagKey);
            Debug.Log("[SandWater] Phase 12 setup flag cleared.");
        }

        // ---------------- Materials ----------------

        private static void CreateSharedMaterials()
        {
            _matStone = NewLitMaterial(new Color(0.55f, 0.53f, 0.50f), name: "Mat_Stone");
            _matWood = NewLitMaterial(new Color(0.40f, 0.25f, 0.12f), name: "Mat_Wood");
            _matWater = NewLitMaterial(new Color(0.20f, 0.55f, 0.85f, 0.85f), name: "Mat_Water");
            _matWallA = NewLitMaterial(new Color(0.85f, 0.78f, 0.62f), name: "Mat_HouseWallA");
            _matWallB = NewLitMaterial(new Color(0.78f, 0.66f, 0.50f), name: "Mat_HouseWallB");
            _matRoofRed = NewLitMaterial(new Color(0.55f, 0.30f, 0.22f), name: "Mat_RoofRed");
            _matDeadBark = NewLitMaterial(new Color(0.32f, 0.22f, 0.15f), name: "Mat_DeadBark");
            _matWithered = NewLitMaterial(new Color(0.62f, 0.50f, 0.30f), name: "Mat_Withered");
            _matCrackedGround = NewLitMaterial(new Color(0.55f, 0.42f, 0.30f), name: "Mat_CrackedGround");
        }

        private static Material NewLitMaterial(Color color, string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
            return mat;
        }

        // ---------------- Well ----------------

        private static void HideExistingGoalVisual(Transform goalTransform)
        {
            var existing = goalTransform.Find(ExistingGoalVisualName);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }
        }

        private static void BuildWellAt(GoalZone goal)
        {
            // Remove any pre-existing well from a previous run (idempotent re-run).
            var oldWell = goal.transform.Find(WellChildName);
            if (oldWell != null) Object.DestroyImmediate(oldWell.gameObject);

            var well = new GameObject(WellChildName);
            well.transform.SetParent(goal.transform, false);

            // Base slab
            CreatePrimitiveChild(well.transform, "Base", PrimitiveType.Cylinder,
                localPos: new Vector3(0f, -0.5f, 0f),
                localScale: new Vector3(2.6f, 0.2f, 2.6f),
                material: _matStone);

            // Outer wall (rim)
            CreatePrimitiveChild(well.transform, "Wall", PrimitiveType.Cylinder,
                localPos: new Vector3(0f, 0.0f, 0f),
                localScale: new Vector3(2.0f, 0.4f, 2.0f),
                material: _matStone);

            // Animated water cylinder (bottom anchored at -0.5 in local space)
            var water = CreatePrimitiveChild(well.transform, "Water", PrimitiveType.Cylinder,
                localPos: new Vector3(0f, -0.475f, 0f),
                localScale: new Vector3(1.7f, 0.025f, 1.7f),
                material: _matWater);
            // Disable collider (this is purely visual)
            var waterCol = water.GetComponent<Collider>();
            if (waterCol != null) Object.DestroyImmediate(waterCol);

            // Four wooden posts at the corners of the rim, holding up the roof
            float postX = 0.95f;
            for (int i = 0; i < 4; i++)
            {
                float sx = (i & 1) == 0 ? postX : -postX;
                float sz = (i & 2) == 0 ? postX : -postX;
                CreatePrimitiveChild(well.transform, "Post" + i, PrimitiveType.Cube,
                    localPos: new Vector3(sx, 1.1f, sz),
                    localScale: new Vector3(0.12f, 1.6f, 0.12f),
                    material: _matWood);
            }

            // Crossbar (horizontal beam between two posts, where the bucket hangs)
            CreatePrimitiveChild(well.transform, "Crossbar", PrimitiveType.Cube,
                localPos: new Vector3(0f, 1.85f, 0f),
                localScale: new Vector3(0.1f, 0.1f, 2.0f),
                material: _matWood);

            // Simple peaked roof (two tilted slabs)
            var roof1 = CreatePrimitiveChild(well.transform, "RoofLeft", PrimitiveType.Cube,
                localPos: new Vector3(-0.55f, 2.25f, 0f),
                localScale: new Vector3(1.4f, 0.08f, 2.6f),
                material: _matRoofRed);
            roof1.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

            var roof2 = CreatePrimitiveChild(well.transform, "RoofRight", PrimitiveType.Cube,
                localPos: new Vector3(0.55f, 2.25f, 0f),
                localScale: new Vector3(1.4f, 0.08f, 2.6f),
                material: _matRoofRed);
            roof2.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);

            // Bucket hanging from the crossbar
            CreatePrimitiveChild(well.transform, "BucketRope", PrimitiveType.Cube,
                localPos: new Vector3(0f, 1.4f, 0f),
                localScale: new Vector3(0.04f, 0.7f, 0.04f),
                material: _matWood);

            CreatePrimitiveChild(well.transform, "Bucket", PrimitiveType.Cylinder,
                localPos: new Vector3(0f, 1.0f, 0f),
                localScale: new Vector3(0.5f, 0.25f, 0.5f),
                material: _matWood);

            // Wire WellWaterLevel script
            var level = goal.GetComponent<WellWaterLevel>();
            if (level == null) level = goal.gameObject.AddComponent<WellWaterLevel>();
            AssignSerializedRef(level, "goalZone", goal);
            AssignSerializedRef(level, "waterCylinder", water.transform);
        }

        // ---------------- Village (around goal) ----------------

        private static void BuildVillageAround(Vector3 goalPos, Vector3 sourcePos)
        {
            var existing = GameObject.Find(VillageGoName);
            if (existing != null) Object.DestroyImmediate(existing);

            var village = new GameObject(VillageGoName);
            village.transform.position = Vector3.zero;

            var terrain = Object.FindAnyObjectByType<Terrain>();

            // Direction toward source; place houses to the "back" of the goal so the player
            // has unobstructed view from the source approach.
            Vector3 toSource = (sourcePos - goalPos);
            toSource.y = 0;
            if (toSource.sqrMagnitude < 0.01f) toSource = Vector3.left;
            toSource.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, toSource);

            // Three houses placed in a small arc behind the well (away from player approach).
            BuildHouse(village.transform, goalPos + (-toSource) * 5.5f + right * -3.5f, terrain, _matWallA);
            BuildHouse(village.transform, goalPos + (-toSource) * 5.5f + right * 3.5f,  terrain, _matWallB);
            BuildHouse(village.transform, goalPos + (-toSource) * 7.5f, terrain, _matWallA);

            // Two dead trees flanking the village
            BuildDeadTree(village.transform, goalPos + right * -6f, terrain);
            BuildDeadTree(village.transform, goalPos + right * 6f, terrain);

            // Withered crop patches in the foreground (on the path toward the source)
            for (int i = 0; i < 4; i++)
            {
                float t = (i + 1) / 5f; // 0.2 .. 0.8
                Vector3 patchPos = Vector3.Lerp(goalPos, sourcePos, t * 0.45f);
                patchPos += right * (i % 2 == 0 ? -2.5f : 2.5f);
                BuildCropPatch(village.transform, patchPos, terrain);
            }
        }

        private static void BuildHouse(Transform parent, Vector3 worldPos, Terrain terrain, Material wallMat)
        {
            float baseY = SampleTerrainY(terrain, worldPos);
            var go = new GameObject("House");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldPos.x, baseY, worldPos.z);

            // Body
            CreatePrimitiveChild(go.transform, "Body", PrimitiveType.Cube,
                localPos: new Vector3(0f, 0.6f, 0f),
                localScale: new Vector3(1.6f, 1.2f, 1.6f),
                material: wallMat);

            // Peaked roof - two tilted slabs
            var rL = CreatePrimitiveChild(go.transform, "RoofLeft", PrimitiveType.Cube,
                localPos: new Vector3(-0.55f, 1.45f, 0f),
                localScale: new Vector3(1.2f, 0.08f, 1.7f),
                material: _matRoofRed);
            rL.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            var rR = CreatePrimitiveChild(go.transform, "RoofRight", PrimitiveType.Cube,
                localPos: new Vector3(0.55f, 1.45f, 0f),
                localScale: new Vector3(1.2f, 0.08f, 1.7f),
                material: _matRoofRed);
            rR.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);

            // Door
            CreatePrimitiveChild(go.transform, "Door", PrimitiveType.Cube,
                localPos: new Vector3(0f, 0.4f, 0.81f),
                localScale: new Vector3(0.45f, 0.8f, 0.05f),
                material: _matWood);

            go.transform.rotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);
        }

        private static void BuildDeadTree(Transform parent, Vector3 worldPos, Terrain terrain)
        {
            float baseY = SampleTerrainY(terrain, worldPos);
            var go = new GameObject("DeadTree");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldPos.x, baseY, worldPos.z);

            // Trunk
            CreatePrimitiveChild(go.transform, "Trunk", PrimitiveType.Cylinder,
                localPos: new Vector3(0f, 1.2f, 0f),
                localScale: new Vector3(0.25f, 1.2f, 0.25f),
                material: _matDeadBark);

            // A few angled branches (bare, no leaves)
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + Random.Range(-15f, 15f);
                var b = CreatePrimitiveChild(go.transform, "Branch" + i, PrimitiveType.Cube,
                    localPos: Vector3.zero,
                    localScale: new Vector3(0.08f, 0.7f, 0.08f),
                    material: _matDeadBark);
                b.transform.localPosition = new Vector3(0f, 2.0f, 0f);
                b.transform.localRotation = Quaternion.Euler(0f, angle, 55f);
                b.transform.localPosition = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.4f, 2.0f, Mathf.Cos(angle * Mathf.Deg2Rad) * 0.4f);
            }
        }

        private static void BuildCropPatch(Transform parent, Vector3 worldPos, Terrain terrain)
        {
            var go = new GameObject("WitheredCropPatch");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            // Many small dead plants. Each samples its own terrain Y so the patch follows
            // heightmap undulations instead of hovering as a flat plate.
            const int plantCount = 16;
            const float patchRadius = 1.4f;
            for (int i = 0; i < plantCount; i++)
            {
                // Sunflower / golden-angle spiral for even spacing
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float r = Mathf.Sqrt((i + 0.5f) / plantCount) * patchRadius;
                float plantX = worldPos.x + Mathf.Cos(angle) * r;
                float plantZ = worldPos.z + Mathf.Sin(angle) * r;
                float plantY = SampleTerrainY(terrain, new Vector3(plantX, 0f, plantZ));

                float height = Random.Range(0.18f, 0.45f);
                float thickness = Random.Range(0.03f, 0.06f);

                CreateGroundedPlant(go.transform, "Plant" + i,
                    worldPos: new Vector3(plantX, plantY, plantZ),
                    height: height, thickness: thickness, material: _matWithered);
            }
        }

        /// <summary>
        /// Creates a single small upright cube whose visible portion has the supplied height,
        /// but which extends BuryDepth units below the terrain anchor. The deep underground
        /// extension hides any small mismatch between editor-time and runtime terrain Y, and
        /// keeps the root from popping out when the player digs around the base.
        /// </summary>
        private const float PlantBuryDepth = 2.0f;
        private static void CreateGroundedPlant(Transform parent, string name, Vector3 worldPos,
                                                float height, float thickness, Material material)
        {
            float totalHeight = height + PlantBuryDepth;

            var plant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plant.name = name;
            var col = plant.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            plant.transform.SetParent(parent, true);
            // Center the cube so visible top ends at worldPos.y + height and the buried end
            // reaches worldPos.y - PlantBuryDepth.
            plant.transform.position = new Vector3(
                worldPos.x,
                worldPos.y + (height - PlantBuryDepth) * 0.5f,
                worldPos.z);
            plant.transform.rotation = Quaternion.Euler(
                Random.Range(-8f, 8f),
                Random.Range(0f, 360f),
                Random.Range(-8f, 8f));
            plant.transform.localScale = new Vector3(thickness, totalHeight, thickness);
            var mr = plant.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = material;
        }

        // ---------------- Wastes (near source) ----------------

        private static void BuildWastesNear(WaterSource source)
        {
            if (source == null) return;
            var existing = GameObject.Find(WastesGoName);
            if (existing != null) Object.DestroyImmediate(existing);

            var wastes = new GameObject(WastesGoName);
            wastes.transform.position = Vector3.zero;
            var terrain = Object.FindAnyObjectByType<Terrain>();

            Vector3 sp = source.transform.position;

            // Couple of dead trees flanking the spring
            BuildDeadTree(wastes.transform, sp + new Vector3(2f, 0f, 2.5f), terrain);
            BuildDeadTree(wastes.transform, sp + new Vector3(-1.5f, 0f, -3f), terrain);

            // Dense dead-grass tufts around the spring (per-tuft terrain sampling, no floating plates).
            BuildDeadGrassPatch(wastes.transform, sp + new Vector3(1.5f, 0f, 0f), terrain, radius: 2.5f, count: 24);
            BuildDeadGrassPatch(wastes.transform, sp + new Vector3(-2.5f, 0f, 1.5f), terrain, radius: 1.6f, count: 14);
        }

        private static void BuildDeadGrassPatch(Transform parent, Vector3 centerPos, Terrain terrain,
                                                float radius, int count)
        {
            var go = new GameObject("DeadGrassPatch");
            go.transform.SetParent(parent, false);
            go.transform.position = centerPos;

            for (int i = 0; i < count; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float r = Mathf.Sqrt((i + 0.5f) / count) * radius;
                float x = centerPos.x + Mathf.Cos(angle) * r;
                float z = centerPos.z + Mathf.Sin(angle) * r;
                float y = SampleTerrainY(terrain, new Vector3(x, 0f, z));

                float h = Random.Range(0.10f, 0.28f);
                CreateGroundedPlant(go.transform, "Tuft" + i,
                    worldPos: new Vector3(x, y, z),
                    height: h, thickness: 0.04f, material: _matWithered);
            }
        }

        // ---------------- Helpers ----------------

        private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType type,
                                                      Vector3 localPos, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // Strip default colliders — we don't need physics on these decorations.
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = material;
            return go;
        }

        private static float SampleTerrainY(Terrain terrain, Vector3 worldPos)
        {
            if (terrain == null) return worldPos.y;
            return terrain.SampleHeight(new Vector3(worldPos.x, 0f, worldPos.z)) + terrain.transform.position.y;
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
