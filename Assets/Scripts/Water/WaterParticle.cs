using UnityEngine;

namespace SandWater.WaterSim
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class WaterParticle : MonoBehaviour
    {
        private WaterSimulator _owner;
        private float _remainingLifetime;
        private Rigidbody _rb;
        private float _despawnBelowY;

        public Rigidbody Body => _rb != null ? _rb : _rb = GetComponent<Rigidbody>();

        public void Launch(WaterSimulator owner, Vector3 position, Vector3 velocity, float lifetime, float despawnBelowY)
        {
            _owner = owner;
            _remainingLifetime = lifetime;
            _despawnBelowY = despawnBelowY;

            var rb = Body;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            rb.position = position;
            rb.rotation = Quaternion.identity;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            _remainingLifetime -= Time.fixedDeltaTime;
            if (_remainingLifetime <= 0f || transform.position.y < _despawnBelowY)
            {
                _owner?.Recycle(this);
            }
        }
    }
}
