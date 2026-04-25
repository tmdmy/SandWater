using UnityEngine;
using UnityEngine.UI;
using SandWater.Core;

namespace SandWater.UI
{
    public class ClearScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text timeText;
        [SerializeField] private Button retryButton;

        private bool _shown;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        }

        private void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
        }

        private void Update()
        {
            var state = GameManager.Instance?.State;
            if (state == null) return;

            if (!_shown && state.IsCleared)
            {
                Show(state.ElapsedTime);
            }
        }

        private void Show(float elapsed)
        {
            _shown = true;
            if (panel != null) panel.SetActive(true);
            if (timeText != null)
            {
                int total = Mathf.FloorToInt(elapsed);
                timeText.text = $"Cleared in {total / 60:00}:{total % 60:00}";
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnRetry()
        {
            GameManager.Instance?.RetryMain();
        }
    }
}
