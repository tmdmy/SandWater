#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SandWater.TerrainSystem;
using SandWater.UI;
using SandWater.WaterSim;

namespace SandWater.EditorTools
{
    [InitializeOnLoad]
    public static class Phase7SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase7SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        private static Font LegacyFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Phase7SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Phase6SetupTool.SetupGoal();
            SetupUI();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 7 UI")]
        public static void SetupUI()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            if (Object.FindAnyObjectByType<HUDController>() != null)
            {
                Debug.Log("[SandWater] Phase 7 UI already set up.");
                return;
            }

            EnsureEventSystem(scene);
            var hud = BuildHUD(scene);
            BuildPauseMenu(scene);
            BuildClearScreen(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Phase 7 UI setup complete.");
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(es, scene);
        }

        private static HUDController BuildHUD(Scene scene)
        {
            var canvasGo = CreateCanvas(scene, "[GameHUD]", sortOrder: 0);
            var hud = canvasGo.AddComponent<HUDController>();

            var crosshair = CreateImage(canvasGo.transform, "Crosshair", new Color(1, 1, 1, 0.8f));
            var crossRt = crosshair.rectTransform;
            crossRt.anchorMin = crossRt.anchorMax = new Vector2(0.5f, 0.5f);
            crossRt.sizeDelta = new Vector2(10f, 10f);
            crossRt.anchoredPosition = Vector2.zero;

            var timeText = CreateText(canvasGo.transform, "TimeText", "Time  00:00", 36);
            AnchorTo(timeText.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(400f, 60f), TextAnchor.UpperRight);

            var goalText = CreateText(canvasGo.transform, "GoalText", "Goal  0/0", 32);
            AnchorTo(goalText.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -100f), new Vector2(400f, 48f), TextAnchor.UpperRight);

            var gaugeBg = CreateImage(canvasGo.transform, "GoalGaugeBg", new Color(0f, 0f, 0f, 0.5f));
            AnchorTo(gaugeBg.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -160f), new Vector2(400f, 24f), TextAnchor.UpperRight, pivotRight: true);

            var gaugeFill = CreateImage(gaugeBg.transform, "Fill", new Color(0.25f, 0.75f, 1f, 1f));
            gaugeFill.type = Image.Type.Filled;
            gaugeFill.fillMethod = Image.FillMethod.Horizontal;
            gaugeFill.fillAmount = 0f;
            var fillRt = gaugeFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var brushText = CreateText(canvasGo.transform, "BrushText", "Brush  r 0.0m   power 0.0", 28);
            AnchorTo(brushText.rectTransform, new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(500f, 40f), TextAnchor.LowerLeft, pivotLeft: true);

            AssignRef(hud, "timeText", timeText);
            AssignRef(hud, "brushText", brushText);
            AssignRef(hud, "goalText", goalText);
            AssignRef(hud, "goalGaugeFill", gaugeFill);

            return hud;
        }

        private static void BuildPauseMenu(Scene scene)
        {
            var canvasGo = CreateCanvas(scene, "[PauseMenu]", sortOrder: 10);
            var menu = canvasGo.AddComponent<PauseMenu>();

            var panel = CreateImage(canvasGo.transform, "Panel", new Color(0f, 0f, 0f, 0.7f));
            StretchFull(panel.rectTransform);

            var title = CreateText(panel.transform, "Title", "PAUSED", 80);
            AnchorTo(title.rectTransform, new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(600f, 120f), TextAnchor.MiddleCenter);

            var resume = CreateButton(panel.transform, "ResumeButton", "Resume");
            AnchorTo(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 80f), TextAnchor.MiddleCenter);

            var retry = CreateButton(panel.transform, "RetryButton", "Retry");
            AnchorTo(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(320f, 80f), TextAnchor.MiddleCenter);

            var quit = CreateButton(panel.transform, "QuitButton", "Quit");
            AnchorTo(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 0.26f), Vector2.zero, new Vector2(320f, 80f), TextAnchor.MiddleCenter);

            AssignRef(menu, "panel", panel.gameObject);
            AssignRef(menu, "resumeButton", resume);
            AssignRef(menu, "retryButton", retry);
            AssignRef(menu, "quitButton", quit);
        }

        private static void BuildClearScreen(Scene scene)
        {
            var canvasGo = CreateCanvas(scene, "[ClearScreen]", sortOrder: 20);
            var clear = canvasGo.AddComponent<ClearScreen>();

            var panel = CreateImage(canvasGo.transform, "Panel", new Color(0.05f, 0.1f, 0.2f, 0.85f));
            StretchFull(panel.rectTransform);

            var title = CreateText(panel.transform, "Title", "CLEAR!", 120);
            AnchorTo(title.rectTransform, new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(800f, 160f), TextAnchor.MiddleCenter);
            title.color = new Color(1f, 0.9f, 0.3f, 1f);

            var timeText = CreateText(panel.transform, "TimeText", "Cleared in 00:00", 48);
            AnchorTo(timeText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 80f), TextAnchor.MiddleCenter);

            var retry = CreateButton(panel.transform, "RetryButton", "Retry");
            AnchorTo(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(320f, 80f), TextAnchor.MiddleCenter);

            AssignRef(clear, "panel", panel.gameObject);
            AssignRef(clear, "timeText", timeText);
            AssignRef(clear, "retryButton", retry);
        }

        private static GameObject CreateCanvas(Scene scene, string name, int sortOrder)
        {
            var go = new GameObject(name,
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            return go;
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
            img.color = new Color(0.2f, 0.5f, 0.9f, 1f);
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

        private static void AnchorTo(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size, TextAnchor align, bool pivotLeft = false, bool pivotRight = false)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            if (pivotLeft) rt.pivot = new Vector2(0f, rt.pivot.y);
            if (pivotRight) rt.pivot = new Vector2(1f, rt.pivot.y);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var text = rt.GetComponent<Text>();
            if (text != null) text.alignment = align;
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
