#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SandWater.Core;
using SandWater.UI;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class Phase9_1SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase9_1SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const float DefaultTimeLimit = 120f;

        private static Font LegacyFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Phase9_1SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Phase8SetupTool.SetupPolish();
            SetupTimeLimit();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 9.1 Time Limit")]
        public static void SetupTimeLimit()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            EnsureSessionConfig(scene);
            EnsureFailScreen(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 9.1 time limit setup complete.");
        }

        private static void EnsureSessionConfig(Scene scene)
        {
            if (Object.FindAnyObjectByType<SessionConfig>() != null) return;
            var go = new GameObject("[SessionConfig]");
            SceneManager.MoveGameObjectToScene(go, scene);
            var cfg = go.AddComponent<SessionConfig>();
            var so = new SerializedObject(cfg);
            so.FindProperty("timeLimitSeconds").floatValue = DefaultTimeLimit;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFailScreen(Scene scene)
        {
            if (Object.FindAnyObjectByType<FailScreen>() != null) return;

            var canvasGo = new GameObject("[FailScreen]",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var fail = canvasGo.AddComponent<FailScreen>();

            var panel = CreateImage(canvasGo.transform, "Panel", new Color(0.25f, 0.05f, 0.05f, 0.85f));
            StretchFull(panel.rectTransform);

            var title = CreateText(panel.transform, "Title", "TIME UP", 120);
            AnchorCenter(title.rectTransform, new Vector2(0.5f, 0.7f), new Vector2(800f, 160f));
            title.color = new Color(1f, 0.6f, 0.3f, 1f);

            var timeText = CreateText(panel.transform, "TimeText", "Time limit 02:00 elapsed", 42);
            AnchorCenter(timeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 70f));

            var retry = CreateButton(panel.transform, "RetryButton", "Retry");
            AnchorCenter(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.3f), new Vector2(320f, 80f));

            AssignRef(fail, "panel", panel.gameObject);
            AssignRef(fail, "timeText", timeText);
            AssignRef(fail, "retryButton", retry);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = LegacyFont;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var labelText = labelGo.GetComponent<Text>();
            labelText.text = label;
            labelText.font = LegacyFont;
            labelText.fontSize = 40;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            return go.GetComponent<Button>();
        }

        private static void AnchorCenter(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AssignRef(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(propertyName);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
#endif
