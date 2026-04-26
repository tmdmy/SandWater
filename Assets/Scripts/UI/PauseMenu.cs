using UnityEngine;
using UnityEngine.UI;
using SandWater.Core;
using SandWater.Inputs;
using UnityEngine.SceneManagement;

namespace SandWater.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button stageSelectButton;
        [SerializeField] private Button quitButton;

        private CursorLockMode _savedLockMode;
        private bool _savedCursorVisible;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (stageSelectButton != null) stageSelectButton.onClick.AddListener(OnStageSelect);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnEnable()
        {
            var input = InputHandler.Instance;
            if (input != null) input.PauseToggled += TogglePause;
        }

        private void OnDisable()
        {
            var input = InputHandler.Instance;
            if (input != null) input.PauseToggled -= TogglePause;
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
            if (stageSelectButton != null) stageSelectButton.onClick.RemoveListener(OnStageSelect);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        public void TogglePause()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State == null) return;

            switch (gm.State.Phase)
            {
                case GamePhase.Playing:
                    gm.Pause();
                    ShowPanel(true);
                    break;
                case GamePhase.Paused:
                    OnResume();
                    break;
            }
        }

        private void ShowPanel(bool show)
        {
            if (panel != null) panel.SetActive(show);

            if (show)
            {
                _savedLockMode = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = _savedLockMode == CursorLockMode.None ? CursorLockMode.Locked : _savedLockMode;
                Cursor.visible = false;
            }
        }

        private void OnResume()
        {
            GameManager.Instance?.Resume();
            ShowPanel(false);
        }

        private void OnRetry()
        {
            ShowPanel(false);
            GameManager.Instance?.RetryMain();
        }

        private void OnStageSelect()
        {
            // Reset time scale and cursor lock so the Title scene runs normally.
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowPanel(false);
            SceneLoader.LoadTitle();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
