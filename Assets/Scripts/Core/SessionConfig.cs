using UnityEngine;

namespace SandWater.Core
{
    public class SessionConfig : MonoBehaviour
    {
        [SerializeField, Tooltip("0 = unlimited")] private float timeLimitSeconds = 120f;

        public float TimeLimitSeconds => timeLimitSeconds;

        private void Start()
        {
            ApplyToState();
        }

        public void ApplyToState()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State == null) return;
            gm.State.TimeLimit = timeLimitSeconds;
        }

        public void SetTimeLimit(float seconds)
        {
            timeLimitSeconds = seconds;
            ApplyToState();
        }
    }
}
