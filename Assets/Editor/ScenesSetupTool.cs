#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SandWater.UI;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class ScenesSetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase2SetupDone";
        private const string TitleScenePath = "Assets/Scenes/Title.unity";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        static ScenesSetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SetupPhase2Scenes();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 2 Scenes")]
        public static void SetupPhase2Scenes()
        {
            SetupBootstrapScene();
            SetupTitleScene();
            EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 2 scenes setup complete. Press Play on Bootstrap scene.");
        }

        private static void SetupBootstrapScene()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupTitleScene()
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<TitleUIController>() != null)
            {
                return;
            }

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(es, scene);
            }

            var canvasGo = new GameObject("TitleCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(TitleUIController));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var titleTextGo = new GameObject("TitleLabel", typeof(RectTransform), typeof(Text));
            titleTextGo.transform.SetParent(canvasGo.transform, false);
            var titleRt = titleTextGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.75f);
            titleRt.anchorMax = new Vector2(0.5f, 0.75f);
            titleRt.sizeDelta = new Vector2(800, 200);
            titleRt.anchoredPosition = Vector2.zero;
            var titleText = titleTextGo.GetComponent<Text>();
            titleText.text = "SandWater";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontSize = 96;
            titleText.color = Color.white;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var buttonGo = new GameObject("StartButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var buttonRt = buttonGo.GetComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0.5f, 0.35f);
            buttonRt.anchorMax = new Vector2(0.5f, 0.35f);
            buttonRt.sizeDelta = new Vector2(360, 100);
            buttonRt.anchoredPosition = Vector2.zero;
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var button = buttonGo.GetComponent<Button>();

            var buttonLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            buttonLabelGo.transform.SetParent(buttonGo.transform, false);
            var labelRt = buttonLabelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var labelText = buttonLabelGo.GetComponent<Text>();
            labelText.text = "Start";
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.fontSize = 48;
            labelText.color = Color.white;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var controller = canvasGo.GetComponent<TitleUIController>();
            var so = new SerializedObject(controller);
            so.FindProperty("startButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
