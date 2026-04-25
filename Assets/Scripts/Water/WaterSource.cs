using UnityEngine;

namespace SandWater.WaterSim
{
    public class WaterSource : MonoBehaviour
    {
        [SerializeField] private WaterSimulator simulator;
        [SerializeField] private float spawnRatePerSecond = 40f;
        [SerializeField] private float spawnSpread = 0.15f;
        [SerializeField] private Vector3 initialVelocity = new Vector3(5f, -1f, 0f);

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
                simulator.Spawn(transform.position + offset, initialVelocity);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.55f, 0.9f, 0.6f);
            Gizmos.DrawSphere(transform.position, 0.4f);
        }
    }
}
