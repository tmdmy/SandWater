#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SandWater.WaterSim;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class Phase5SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase5SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        static Phase5SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Phase4SetupTool.SetupMainTerrain();
            SetupWaterSystem();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 5 Water")]
        public static void SetupWaterSystem()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<WaterSimulator>() != null)
            {
                Debug.Log("[SandWater] Phase 5 water already set up.");
                return;
            }

            var simGo = new GameObject("[WaterSystem]");
            SceneManager.MoveGameObjectToScene(simGo, scene);
            var simulator = simGo.AddComponent<WaterSimulator>();

            var sourceGo = new GameObject("WaterSource");
            SceneManager.MoveGameObjectToScene(sourceGo, scene);
            sourceGo.transform.position = new Vector3(-15f, 11f, 0f);
            var source = sourceGo.AddComponent<WaterSource>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "SourceVisual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(sourceGo.transform, false);
            visual.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            var color = new Color(0.1f, 0.4f, 0.85f, 1f);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

            AssignSerializedRef(source, "simulator", simulator);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 5 water setup complete.");
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
