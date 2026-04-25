#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
    public static class Phase9_2SetupTool
    {
        private const string SetupFlagKey = "SandWater.Phase9_2SetupDone";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string TitleScenePath = "Assets/Scenes/Title.unity";
        private const string StagesFolder = "Assets/Stages";
        private const string ResourcesFolder = "Assets/Resources";
        private const string StageDatabaseAssetPath = "Assets/Resources/StageDatabase.asset";

        private static Font LegacyFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Phase9_2SetupTool()
        {
            EditorApplication.delayCall += TryAutoSetup;
        }

        private static void TryAutoSetup()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Phase9_1SetupTool.SetupTimeLimit();
            SetupStages();
            EditorPrefs.SetBool(SetupFlagKey, true);
        }

        [MenuItem("SandWater/Setup Phase 9.2 Stages")]
        public static void SetupStages()
        {
            StageDatabase db = EnsureStageAssets();
            ApplyStageApplierToMain(db);
            ApplyStageSelectorToTitle(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SandWater] Phase 9.2 stage setup complete.");
        }

        private static StageDatabase EnsureStageAssets()
        {
            EnsureFolder(StagesFolder);
            EnsureFolder(ResourcesFolder);

            var stage1 = LoadOrCreateStage(
                "Stage1",
                preset =>
                {
                    preset.stageName = "Stage 1 - Easy Channel";
                    preset.description = "Carve a short channel between source and goal.";
                    preset.initialTerrainHeight = 0.3f;
                    preset.playerStartPosition = new Vector3(0f, 10f, 0f);
                    preset.waterSourcePosition = new Vector3(-15f, 11f, 0f);
                    preset.waterInitialVelocity = new Vector3(5f, -1f, 0f);
                    preset.waterSpawnRatePerSecond = 40f;
                    preset.goalPosition = new Vector3(8f, 9f, 0f);
                    preset.goalBoxSize = new Vector3(4f, 8f, 4f);
                    preset.goalRequiredCount = 10;
                    preset.timeLimitSeconds = 120f;
                    preset.brushInitialRadius = 4f;
                    preset.brushInitialStrength = 8f;
                });

            var stage2 = LoadOrCreateStage(
                "Stage2",
                preset =>
                {
                    preset.stageName = "Stage 2 - Longer Run";
                    preset.description = "Goal is farther away. Budget your strokes.";
                    preset.initialTerrainHeight = 0.35f;
                    preset.playerStartPosition = new Vector3(0f, 11.5f, 0f);
                    preset.waterSourcePosition = new Vector3(-20f, 13f, -5f);
                    preset.waterInitialVelocity = new Vector3(5f, -1f, 1f);
                    preset.waterSpawnRatePerSecond = 35f;
                    preset.goalPosition = new Vector3(18f, 10f, 5f);
                    preset.goalBoxSize = new Vector3(4f, 8f, 4f);
                    preset.goalRequiredCount = 15;
                    preset.timeLimitSeconds = 150f;
                    preset.brushInitialRadius = 3.5f;
                    preset.brushInitialStrength = 7f;
                });

            var stage3 = LoadOrCreateStage(
                "Stage3",
                preset =>
                {
                    preset.stageName = "Stage 3 - High Plateau";
                    preset.description = "Taller sand, tighter clock, and a small goal.";
                    preset.initialTerrainHeight = 0.45f;
                    preset.playerStartPosition = new Vector3(-5f, 14.5f, -5f);
                    preset.waterSourcePosition = new Vector3(-22f, 16f, 0f);
                    preset.waterInitialVelocity = new Vector3(6f, -1f, 0f);
                    preset.waterSpawnRatePerSecond = 30f;
                    preset.goalPosition = new Vector3(20f, 12f, 0f);
                    preset.goalBoxSize = new Vector3(3f, 8f, 3f);
                    preset.goalRequiredCount = 20;
                    preset.timeLimitSeconds = 180f;
                    preset.brushInitialRadius = 3f;
                    preset.brushInitialStrength = 6f;
                });

            var db = AssetDatabase.LoadAssetAtPath<StageDatabase>(StageDatabaseAssetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<StageDatabase>();
                AssetDatabase.CreateAsset(db, StageDatabaseAssetPath);
            }
            var so = new SerializedObject(db);
            var list = so.FindProperty("stages");
            list.ClearArray();
            AppendToList(list, stage1);
            AppendToList(list, stage2);
            AppendToList(list, stage3);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            return db;
        }

        private static StagePreset LoadOrCreateStage(string fileBase, System.Action<StagePreset> init)
        {
            string path = StagesFolder + "/" + fileBase + ".asset";
            var preset = AssetDatabase.LoadAssetAtPath<StagePreset>(path);
            bool created = false;
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<StagePreset>();
                AssetDatabase.CreateAsset(preset, path);
                created = true;
            }
            if (created)
            {
                init(preset);
                EditorUtility.SetDirty(preset);
            }
            return preset;
        }

        private static void AppendToList(SerializedProperty list, Object obj)
        {
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = obj;
        }

        private static void ApplyStageApplierToMain(StageDatabase db)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            var existing = Object.FindAnyObjectByType<StageApplier>();
            if (existing == null)
            {
                var go = new GameObject("[StageApplier]");
                SceneManager.MoveGameObjectToScene(go, scene);
                existing = go.AddComponent<StageApplier>();
            }

            StagePreset fallback = db != null && db.Count > 0 ? db.Get(0) : null;
            if (fallback != null)
            {
                AssignRef(existing, "fallbackStage", fallback);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyStageSelectorToTitle(StageDatabase db)
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

            var controller = Object.FindAnyObjectByType<TitleUIController>();
            if (controller == null)
            {
                Debug.LogWarning("[SandWater] TitleUIController not found; run Phase 2 setup first.");
                return;
            }

            var canvasGo = controller.gameObject;

            var startBtnSo = new SerializedObject(controller);
            var existingContainer = startBtnSo.FindProperty("stageButtonContainer").objectReferenceValue;
            var existingTemplate = startBtnSo.FindProperty("stageButtonTemplate").objectReferenceValue;

            RectTransform container;
            if (existingContainer != null)
            {
                container = (RectTransform)existingContainer;
            }
            else
            {
                container = CreateStageContainer(canvasGo.transform);
            }

            Button template;
            if (existingTemplate != null)
            {
                template = (Button)existingTemplate;
            }
            else
            {
                template = CreateStageButtonTemplate(container);
            }

            Text description;
            var existingDescProp = startBtnSo.FindProperty("stageDescriptionText");
            if (existingDescProp.objectReferenceValue != null)
            {
                description = (Text)existingDescProp.objectReferenceValue;
            }
            else
            {
                description = CreateStageDescription(canvasGo.transform);
            }

            AssignRef(controller, "stageButtonContainer", container);
            AssignRef(controller, "stageButtonTemplate", template);
            AssignRef(controller, "stageDescriptionText", description);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static RectTransform CreateStageContainer(Transform parent)
        {
            var go = new GameObject("StageSelectContainer",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.55f);
            rt.anchorMax = new Vector2(0.5f, 0.55f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1200f, 120f);
            rt.anchoredPosition = Vector2.zero;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            return rt;
        }

        private static Button CreateStageButtonTemplate(Transform parent)
        {
            var go = new GameObject("StageButtonTemplate",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(260f, 100f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.9f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "Stage";
            label.font = LegacyFont;
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            go.SetActive(false);
            return go.GetComponent<Button>();
        }

        private static Text CreateStageDescription(Transform parent)
        {
            var go = new GameObject("StageDescription", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.46f);
            rt.anchorMax = new Vector2(0.5f, 0.46f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000f, 100f);
            rt.anchoredPosition = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.text = "";
            t.font = LegacyFont;
            t.fontSize = 28;
            t.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
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
