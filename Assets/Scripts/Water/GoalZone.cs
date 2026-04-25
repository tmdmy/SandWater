using UnityEngine;
using SandWater.Core;

namespace SandWater.WaterSim
{
    [RequireComponent(typeof(Collider))]
    public class GoalZone : MonoBehaviour
    {
        [SerializeField] private int requiredCount = 15;
        [SerializeField] private WaterSimulator simulator;

        private int _reached;
        private bool _cleared;

        public int Reached => _reached;
        public int RequiredCount => requiredCount;
        public float Progress => requiredCount > 0 ? Mathf.Clamp01((float)_reached / requiredCount) : 0f;

        private void Awake()
        {
            foreach (var col in GetComponents<Collider>())
            {
                col.isTrigger = true;
            }
            if (simulator == null) simulator = FindAnyObjectByType<WaterSimulator>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_cleared) return;
            var particle = other.GetComponent<WaterParticle>();
            if (particle == null) return;

            _reached++;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.NotifyWaterReached();
                if (_reached >= requiredCount)
                {
                    _cleared = true;
                    gm.NotifyCleared();
                }
            }

            if (simulator != null) simulator.Recycle(particle);
        }

        public void ResetGoal()
        {
            _reached = 0;
            _cleared = false;
        }

        public void Configure(int required, Vector3 boxSize)
        {
            requiredCount = Mathf.Max(1, required);
            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                box.size = boxSize;
                box.isTrigger = true;
            }
            _reached = 0;
            _cleared = false;
        }
    }
}
