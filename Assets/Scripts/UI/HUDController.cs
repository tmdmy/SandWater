using UnityEngine;
using UnityEngine.UI;
using SandWater.Core;
using SandWater.TerrainSystem;
using SandWater.WaterSim;

namespace SandWater.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Text timeText;
        [SerializeField] private Text brushText;
        [SerializeField] private Text goalText;
        [SerializeField] private Image goalGaugeFill;
        [SerializeField] private TerrainBrush brush;
        [SerializeField] private GoalZone goal;

        private void Awake()
        {
            if (brush == null) brush = FindAnyObjectByType<TerrainBrush>();
            if (goal == null) goal = FindAnyObjectByType<GoalZone>();
        }

        private void Update()
        {
            var state = GameManager.Instance?.State;
            if (state != null && timeText != null)
            {
                if (state.TimeLimit > 0f)
                {
                    int remain = Mathf.CeilToInt(state.RemainingTime);
                    timeText.text = $"Time  {remain / 60:00}:{remain % 60:00}";
                    timeText.color = remain <= 10 ? new Color(1f, 0.4f, 0.3f) : Color.white;
                }
                else
                {
                    int totalSec = Mathf.FloorToInt(state.ElapsedTime);
                    timeText.text = $"Time  {totalSec / 60:00}:{totalSec % 60:00}";
                    timeText.color = Color.white;
                }
            }

            if (brush != null && brushText != null)
            {
                brushText.text = $"Brush  r {brush.RadiusMeters:0.0}m   power {brush.Strength:0.0}";
            }

            if (goal != null)
            {
                if (goalText != null)
                {
                    goalText.text = $"Goal  {goal.Reached}/{goal.RequiredCount}";
                }
                if (goalGaugeFill != null)
                {
                    goalGaugeFill.fillAmount = goal.Progress;
                }
            }
        }
    }
}
