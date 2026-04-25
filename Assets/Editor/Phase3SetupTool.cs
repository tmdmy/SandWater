#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using SandWater.Player;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class Phase3SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase3SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        static Phase3SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SetupMainScene();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 3 Main Scene")]
        public static void SetupMainScene()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<PlayerController>() != null)
            {
                Debug.Log("[SandWater] Phase 3 Main scene already set up.");
                return;
            }

            RemoveDefaultMainCamera(scene);
            CreateGround(scene);
            CreatePlayer(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 3 Main scene setup complete.");
        }

        private static void RemoveDefaultMainCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Main Camera")
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void CreateGround(Scene scene)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            SceneManager.MoveGameObjectToScene(ground, scene);
        }

        private static void CreatePlayer(Scene scene)
        {
            var player = new GameObject("Player");
            SceneManager.MoveGameObjectToScene(player, scene);
            player.transform.position = new Vector3(0f, 2f, 0f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 55f;
            cc.stepOffset = 0.3f;

            var controller = player.AddComponent<PlayerController>();

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);

            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.fieldOfView = 70f;

            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();

            var playerCam = camGo.AddComponent<PlayerCamera>();
            var so = new SerializedObject(playerCam);
            so.FindProperty("playerBody").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
