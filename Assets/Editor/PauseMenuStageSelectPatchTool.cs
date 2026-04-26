#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SandWater.UI;

namespace SandWater.EditorTools
{
    /// <summary>
    /// Patches the existing PauseMenu (created by Phase7SetupTool) to add a "Stage Select"
    /// button between Retry and Quit. Duplicates the Retry button to inherit its visual style,
    /// repositions all four buttons evenly, and wires the new button into PauseMenu.stageSelectButton.
    /// Idempotent: if the button is already wired, does nothing.
    /// </summary>
    public static class PauseMenuStageSelectPatchTool
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("SandWater/Add Stage Select Button to Pause Menu")]
        public static void AddStageSelectButton()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            var menu = Object.FindAnyObjectByType<PauseMenu>();
            if (menu == null)
            {
                Debug.LogWarning("[SandWater] PauseMenu not found in Main scene.");
                return;
            }

            var so = new SerializedObject(menu);
            var stageSelectProp = so.FindProperty("stageSelectButton");
            if (stageSelectProp != null && stageSelectProp.objectReferenceValue != null)
            {
                Debug.Log("[SandWater] Stage Select button already wired up.");
                return;
            }

            var retryProp = so.FindProperty("retryButton");
            var resumeProp = so.FindProperty("resumeButton");
            var quitProp = so.FindProperty("quitButton");

            var retryButton = retryProp != null ? retryProp.objectReferenceValue as Button : null;
            var resumeButton = resumeProp != null ? resumeProp.objectReferenceValue as Button : null;
            var quitButton = quitProp != null ? quitProp.objectReferenceValue as Button : null;

            if (retryButton == null)
            {
                Debug.LogWarning("[SandWater] Retry button reference missing — cannot duplicate.");
                return;
            }

            // Duplicate Retry as the template for Stage Select.
            var newGo = Object.Instantiate(retryButton.gameObject, retryButton.transform.parent);
            newGo.name = "StageSelectButton";
            var label = newGo.GetComponentInChildren<Text>();
            if (label != null) label.text = "Stage Select";
            var newButton = newGo.GetComponent<Button>();

            // Strip any listeners copied from Retry — PauseMenu.Awake re-wires its own.
            if (newButton != null) newButton.onClick.RemoveAllListeners();

            // Reposition all four buttons evenly on Y axis.
            // Resume 0.55, Retry 0.45, StageSelect 0.35, Quit 0.25.
            SetAnchorY(resumeButton,    0.55f);
            SetAnchorY(retryButton,     0.45f);
            SetAnchorY(newButton,       0.35f);
            SetAnchorY(quitButton,      0.25f);

            // Wire the new button into PauseMenu.stageSelectButton.
            stageSelectProp.objectReferenceValue = newButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Stage Select button added and wired up.");
        }

        private static void SetAnchorY(Button button, float y)
        {
            if (button == null) return;
            var rt = button.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, y);
            rt.anchorMax = new Vector2(0.5f, y);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
#endif
