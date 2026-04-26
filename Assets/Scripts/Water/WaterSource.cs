using UnityEngine;

namespace SandWater.WaterSim
{
    public class WaterSource : MonoBehaviour
    {
        [SerializeField] private WaterSimulator simulator;
        [SerializeField] private float spawnRatePerSecond = 80f;
        [Tooltip("Random horizontal offset radius around the source position when spawning. 0 = exact same point (tight stream).")]
        [SerializeField, Range(0f, 1f)] private float spawnSpread = 0.05f;
        [SerializeField] private Vector3 initialVelocity = new Vector3(4f, 5f, 0f);
        [Tooltip("Half-angle (degrees) of the random cone that perturbs each particle's velocity direction. 0 = perfectly focused stream.")]
        [SerializeField, Range(0f, 30f)] private float velocitySpreadDegrees = 3f;

        private float _spawnAccumulator;

        private void Awake()
        {
            if (simulator == null) simulator = FindAnyObjectByType<WaterSimulator>();
        }

        public void Configure(Vector3 initialVelocity, float spawnRatePerSecond)
        {
            this.initialVelocity = initialVelocity;
            this.spawnRatePerSecond = spawnRatePerSecond;
            _spawnAccumulator = 0f;
        }

        private void Update()
        {
            if (simulator == null || spawnRatePerSecond <= 0f) return;

            _spawnAccumulator += Time.deltaTime * spawnRatePerSecond;
            while (_spawnAccumulator >= 1f)
            {
                _spawnAccumulator -= 1f;
                if (!simulator.CanSpawn) { _spawnAccumulator = 0f; break; }

                Vector3 offset = Random.insideUnitSphere * spawnSpread;
                offset.y = 0f;

                Vector3 spawnVelocity = initialVelocity;
                if (velocitySpreadDegrees > 0f && initialVelocity.sqrMagnitude > 0.001f)
                {
                    Quaternion jitter = Quaternion.Euler(
                        Random.Range(-velocitySpreadDegrees, velocitySpreadDegrees),
                        Random.Range(-velocitySpreadDegrees, velocitySpreadDegrees),
                        0f);
                    spawnVelocity = jitter * initialVelocity;
                }

                simulator.Spawn(transform.position + offset, spawnVelocity);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.55f, 0.9f, 0.6f);
            Gizmos.DrawSphere(transform.position, 0.4f);
        }
    }
}
