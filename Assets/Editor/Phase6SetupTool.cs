#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SandWater.WaterSim;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class Phase6SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase6SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        static Phase6SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Phase5SetupTool.SetupWaterSystem();
            SetupGoal();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 6 Goal")]
        public static void SetupGoal()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<GoalZone>() != null)
            {
                Debug.Log("[SandWater] Phase 6 goal already set up.");
                return;
            }

            var goalGo = new GameObject("GoalZone");
            SceneManager.MoveGameObjectToScene(goalGo, scene);
            goalGo.transform.position = new Vector3(15f, 6f, 0f);

            var box = goalGo.AddComponent<BoxCollider>();
            box.size = new Vector3(4f, 8f, 4f);
            box.isTrigger = true;

            var goal = goalGo.AddComponent<GoalZone>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "GoalVisual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(goalGo.transform, false);
            visual.transform.localScale = new Vector3(2.2f, 1.5f, 2.2f);
            visual.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            var color = new Color(0.25f, 0.85f, 0.3f, 1f);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var simulator = Object.FindAnyObjectByType<WaterSimulator>();
            if (simulator != null)
            {
                AssignSerializedRef(goal, "simulator", simulator);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 6 goal setup complete.");
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
