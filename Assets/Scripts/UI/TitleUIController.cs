using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SandWater.Core;

namespace SandWater.UI
{
    public class TitleUIController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private RectTransform stageButtonContainer;
        [SerializeField] private Button stageButtonTemplate;
        [SerializeField] private Text stageDescriptionText;

        private StageDatabase _database;
        private StagePreset _selected;
        private readonly List<Button> _stageButtons = new List<Button>();

        private void Awake()
        {
            if (startButton == null)
            {
                startButton = GetComponentInChildren<Button>(true);
            }
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }

            _database = StageDatabase.LoadFromResources();
            BuildStageButtons();
            SelectDefaultStage();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }
        }

        private void BuildStageButtons()
        {
            if (_database == null || stageButtonContainer == null || stageButtonTemplate == null) return;

            stageButtonTemplate.gameObject.SetActive(false);

            for (int i = 0; i < _database.Count; i++)
            {
                StagePreset stage = _database.Get(i);
                if (stage == null) continue;

                Button btn = Instantiate(stageButtonTemplate, stageButtonContainer);
                btn.gameObject.SetActive(true);
                btn.name = "StageButton_" + stage.stageName;

                var label = btn.GetComponentInChildren<Text>(true);
                if (label != null) label.text = stage.stageName;

                StagePreset captured = stage;
                btn.onClick.AddListener(() => SelectStage(captured));
                _stageButtons.Add(btn);
            }
        }

        private void SelectDefaultStage()
        {
            if (_database == null || _database.Count == 0) return;
            SelectStage(_database.Get(0));
        }

        private void SelectStage(StagePreset stage)
        {
            _selected = stage;
            if (stageDescriptionText != null && stage != null)
            {
                stageDescriptionText.text = string.IsNullOrEmpty(stage.description)
                    ? stage.stageName
                    : stage.stageName + "\n" + stage.description;
            }
            UpdateStageButtonVisuals();
        }

        private void UpdateStageButtonVisuals()
        {
            if (_database == null) return;
            for (int i = 0; i < _stageButtons.Count; i++)
            {
                StagePreset stage = _database.Get(i);
                bool isSelected = stage == _selected;
                var img = _stageButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.color = isSelected
                        ? new Color(0.9f, 0.7f, 0.2f, 1f)
                        : new Color(0.2f, 0.5f, 0.9f, 1f);
                }
            }
        }

        public void OnStartClicked()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.State != null)
            {
                gm.State.SelectedStage = _selected;
            }
            SceneLoader.LoadMain();
        }
    }
}
