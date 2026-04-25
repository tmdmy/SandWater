using System;
using UnityEngine;

namespace SandWater.Core
{
    public enum GamePhase
    {
        Boot,
        Title,
        Playing,
        Paused,
        Cleared,
        Failed
    }

    [Serializable]
    public class GameState
    {
        public GamePhase Phase { get; set; } = GamePhase.Boot;
        public float ElapsedTime { get; set; }
        public float TimeLimit { get; set; }
        public int WaterReachedCount { get; set; }
        public bool IsCleared { get; set; }
        public bool IsFailed { get; set; }
        public StagePreset SelectedStage { get; set; }

        public float RemainingTime => TimeLimit > 0f ? Mathf.Max(0f, TimeLimit - ElapsedTime) : 0f;

        public void ResetForNewPlay()
        {
            Phase = GamePhase.Playing;
            ElapsedTime = 0f;
            WaterReachedCount = 0;
            IsCleared = false;
            IsFailed = false;
        }
    }
}
