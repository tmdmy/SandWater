#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using SandWater.SsfRendering;
using SandWater.WaterSim;

namespace SandWater.EditorTools
{
    /// <summary>
    /// Phase 10 (experiment branch only): wires up the Screen-Space Fluid (SSF) renderer feature
    /// for the metaball-style water visual.
    ///
    /// Steps:
    ///   1. Ensure a "WaterFluid" Layer exists in TagManager.asset (added if missing).
    ///   2. Create Assets/SsfRendering/WaterDrop_Ssf.mat from the SsfElement shader (if missing).
    ///   3. Assign the material + layer name to WaterSimulator in Main scene.
    ///   4. Add SsfRendererFeature as a sub-asset of PC_Renderer.asset (and Mobile_Renderer if present),
    ///      configured to filter for the WaterFluid layer.
    ///
    /// Re-running the menu item is safe — it updates existing settings rather than creating duplicates.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase10MetaballSetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase10MetaballSetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string SsfFolder = "Assets/SsfRendering";
        private const string SsfMaterialPath = "Assets/SsfRendering/WaterDrop_Ssf.mat";
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
        private const string FluidLayerName = "WaterFluid";

        static Phase10MetaballSetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SetupMetaball();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 10 Metaball Visual")]
        public static void SetupMetaball()
        {
            int waterFluidLayer = EnsureLayer(FluidLayerName);
            Material ssfMaterial = LoadOrCreateSsfMaterial();
            if (ssfMaterial == null) return;

            AssignSimulatorFields(ssfMaterial, FluidLayerName);

            AddOrUpdateRendererFeature(PcRendererPath, waterFluidLayer);
            if (System.IO.File.Exists(MobileRendererPath))
            {
                AddOrUpdateRendererFeature(MobileRendererPath, waterFluidLayer);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SandWater] Phase 10 SSF (metaball visual) setup complete. Press Play in Main scene to verify.");
        }

        [MenuItem("SandWater/Reset Phase 10 Setup Flag")]
        public static void ResetFlag()
        {
            EditorPrefs.DeleteKey(SetupFlagKey);
            Debug.Log("[SandWater] Phase 10 setup flag cleared. Will re-run on next domain reload.");
        }

        // ---------- Layer ----------

        private static int EnsureLayer(string layerName)
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogWarning("[SandWater] TagManager.asset not found.");
                return -1;
            }

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null) return -1;

            int firstFreeUserSlot = -1;
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                string name = layersProp.GetArrayElementAtIndex(i).stringValue;
                if (name == layerName) return i;
                if (i >= 8 && string.IsNullOrEmpty(name) && firstFreeUserSlot < 0)
                {
                    firstFreeUserSlot = i;
                }
            }

            if (firstFreeUserSlot >= 0)
            {
                layersProp.GetArrayElementAtIndex(firstFreeUserSlot).stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return firstFreeUserSlot;
            }

            Debug.LogWarning($"[SandWater] No free user-layer slot for '{layerName}'.");
            return -1;
        }

        // ---------- Material ----------

        private static Material LoadOrCreateSsfMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(SsfMaterialPath);
            if (existing != null)
            {
                EnsureSsfMaterialDefaults(existing);
                return existing;
            }

            var shader = Shader.Find("SampleSsf/SsfElement");
            if (shader == null)
            {
                Debug.LogError("[SandWater] Shader 'SampleSsf/SsfElement' not found. Did Unity finish compiling SsfElement.shader?");
                return null;
            }

            EnsureFolder(SsfFolder);
            var mat = new Material(shader) { name = "WaterDrop_Ssf" };
            EnsureSsfMaterialDefaults(mat);
            AssetDatabase.CreateAsset(mat, SsfMaterialPath);
            return mat;
        }

        private static void EnsureSsfMaterialDefaults(Material mat)
        {
            if (mat.HasProperty("_Radius") && mat.GetFloat("_Radius") <= 0f)
            {
                mat.SetFloat("_Radius", 0.1f);
            }
        }

        // ---------- Scene WaterSimulator wiring ----------

        private static void AssignSimulatorFields(Material material, string layerName)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var sim = Object.FindAnyObjectByType<WaterSimulator>();
            if (sim == null)
            {
                Debug.LogWarning("[SandWater] WaterSimulator not found in Main scene.");
                return;
            }

            var so = new SerializedObject(sim);
            var matProp = so.FindProperty("customParticleMaterial");
            var layerProp = so.FindProperty("fluidLayerName");
            if (matProp != null) matProp.objectReferenceValue = material;
            if (layerProp != null) layerProp.stringValue = layerName;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------- URP Renderer Asset wiring ----------

        private static void AddOrUpdateRendererFeature(string rendererAssetPath, int waterFluidLayer)
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererAssetPath);
            if (rendererData == null)
            {
                Debug.LogWarning($"[SandWater] Renderer asset not found: {rendererAssetPath}");
                return;
            }

            // Look for an existing SsfRendererFeature in this asset
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(rendererAssetPath);
            SsfRendererFeature existing = null;
            foreach (var sub in subAssets)
            {
                if (sub is SsfRendererFeature feature)
                {
                    existing = feature;
                    break;
                }
            }

            if (existing != null)
            {
                ConfigureFeatureSettings(existing, waterFluidLayer);
                EnsureFeatureInList(rendererData, existing);
                EditorUtility.SetDirty(rendererData);
                EditorUtility.SetDirty(existing);
                return;
            }

            // Create new
            var newFeature = ScriptableObject.CreateInstance<SsfRendererFeature>();
            newFeature.name = "Ssf";
            newFeature.hideFlags = HideFlags.HideInHierarchy;

            ConfigureFeatureSettings(newFeature, waterFluidLayer);

            AssetDatabase.AddObjectToAsset(newFeature, rendererData);
            EnsureFeatureInList(rendererData, newFeature);

            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(newFeature);
        }

        private static void ConfigureFeatureSettings(SsfRendererFeature feature, int waterFluidLayer)
        {
            var so = new SerializedObject(feature);
            var settingsProp = so.FindProperty("settings");
            if (settingsProp == null)
            {
                so.ApplyModifiedProperties();
                return;
            }

            var shaderProp = settingsProp.FindPropertyRelative("SsfShader");
            if (shaderProp != null && shaderProp.objectReferenceValue == null)
            {
                shaderProp.objectReferenceValue = Shader.Find("SampleSsf/Hidden/Ssf");
            }

            var layerMaskProp = settingsProp.FindPropertyRelative("LayerMask");
            if (layerMaskProp != null && waterFluidLayer >= 0)
            {
                layerMaskProp.intValue = 1 << waterFluidLayer;
            }

            so.ApplyModifiedProperties();
        }

        private static void EnsureFeatureInList(ScriptableRendererData rendererData, SsfRendererFeature feature)
        {
            var so = new SerializedObject(rendererData);
            var listProp = so.FindProperty("m_RendererFeatures");
            var mapProp = so.FindProperty("m_RendererFeatureMap");
            if (listProp == null || mapProp == null)
            {
                Debug.LogWarning("[SandWater] Could not find m_RendererFeatures field on renderer data.");
                so.ApplyModifiedProperties();
                return;
            }

            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == feature)
                {
                    so.ApplyModifiedProperties();
                    return;
                }
            }

            int insertIdx = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(insertIdx);
            listProp.GetArrayElementAtIndex(insertIdx).objectReferenceValue = feature;

            mapProp.InsertArrayElementAtIndex(insertIdx);
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId))
            {
                mapProp.GetArrayElementAtIndex(insertIdx).longValue = localId;
            }

            so.ApplyModifiedProperties();
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
