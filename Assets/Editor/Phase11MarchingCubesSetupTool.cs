#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SandWater.MarchingCubes;
using SandWater.WaterSim;

namespace SandWater.EditorTools
{
    /// <summary>
    /// Phase 11 (experiment branch only): wires up the Marching Cubes fluid mesh.
    ///
    /// Steps:
    ///   1. Create Assets/MarchingCubes/FluidMc.mat (URP/Lit, transparent, water-blue) if missing.
    ///   2. Open Main scene, ensure a [FluidMesh] GameObject exists with FluidMeshRenderer.
    ///   3. Assign the material to its MeshRenderer and the WaterSimulator reference.
    ///   4. Toggle WaterSimulator.hideParticleMesh = true so individual spheres are no longer drawn
    ///      (their colliders remain so GoalZone clear judgement is unaffected).
    /// </summary>
    [InitializeOnLoad]
    public static class Phase11MarchingCubesSetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase11MarchingCubesSetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string McFolder = "Assets/MarchingCubes";
        private const string FluidMaterialPath = "Assets/MarchingCubes/FluidMc.mat";
        private const string FluidMeshGoName = "[FluidMesh]";

        static Phase11MarchingCubesSetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SetupMarchingCubes();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 11 Marching Cubes")]
        public static void SetupMarchingCubes()
        {
            Material fluidMat = LoadOrCreateFluidMaterial();
            if (fluidMat == null) return;

            ApplyToMainScene(fluidMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SandWater] Phase 11 Marching Cubes setup complete. Press Play in Main scene to verify.");
        }

        [MenuItem("SandWater/Reset Phase 11 Setup Flag")]
        public static void ResetFlag()
        {
            EditorPrefs.DeleteKey(SetupFlagKey);
            Debug.Log("[SandWater] Phase 11 setup flag cleared.");
        }

        // ---------- Material ----------

        private static Material LoadOrCreateFluidMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(FluidMaterialPath);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[SandWater] URP Lit shader not found.");
                return null;
            }

            EnsureFolder(McFolder);

            var mat = new Material(shader) { name = "FluidMc" };
            ConfigureUrpLitTransparent(mat);
            AssetDatabase.CreateAsset(mat, FluidMaterialPath);
            return mat;
        }

        private static void ConfigureUrpLitTransparent(Material mat)
        {
            // URP/Lit transparent surface configuration. These keys/keywords match URP 17.
            mat.SetFloat("_Surface", 1f);              // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);                // 0 = Alpha
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_Cull", (float)CullMode.Off);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            mat.renderQueue = (int)RenderQueue.Transparent;

            mat.SetColor("_BaseColor", new Color(0.18f, 0.55f, 0.95f, 0.7f));
            mat.SetFloat("_Smoothness", 0.92f);
            mat.SetFloat("_Metallic", 0f);
        }

        // ---------- Scene wiring ----------

        private static void ApplyToMainScene(Material fluidMat)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            var sim = Object.FindAnyObjectByType<WaterSimulator>();
            if (sim == null)
            {
                Debug.LogWarning("[SandWater] WaterSimulator not found in Main scene.");
                return;
            }

            // Ensure individual spheres are hidden (collider stays for GoalZone trigger).
            var simSO = new SerializedObject(sim);
            var hideProp = simSO.FindProperty("hideParticleMesh");
            if (hideProp != null) hideProp.boolValue = true;
            simSO.ApplyModifiedProperties();

            // Find or create [FluidMesh] GameObject
            var fluidGo = GameObject.Find(FluidMeshGoName);
            if (fluidGo == null)
            {
                fluidGo = new GameObject(FluidMeshGoName);
                Undo.RegisterCreatedObjectUndo(fluidGo, "Create FluidMesh");
            }

            var renderer = fluidGo.GetComponent<FluidMeshRenderer>();
            if (renderer == null)
            {
                renderer = fluidGo.AddComponent<FluidMeshRenderer>();
            }

            var meshRenderer = fluidGo.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = fluidGo.AddComponent<MeshRenderer>();
            }
            meshRenderer.sharedMaterial = fluidMat;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // Wire WaterSimulator reference on FluidMeshRenderer
            var rSO = new SerializedObject(renderer);
            var simProp = rSO.FindProperty("simulator");
            if (simProp != null) simProp.objectReferenceValue = sim;
            rSO.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------- Helpers ----------

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
            var leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
