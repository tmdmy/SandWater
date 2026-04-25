using System.Collections.Generic;
using UnityEngine;

namespace SandWater.WaterSim
{
    public class WaterSimulator : MonoBehaviour
    {
        [SerializeField] private int maxParticles = 800;
        [SerializeField] private float particleRadius = 0.1f;
        [SerializeField] private float particleLifetime = 15f;
        [SerializeField] private float particleMass = 0.02f;
        [SerializeField] private float particleDrag = 0.1f;
        [SerializeField] private float despawnBelowY = -30f;
        [SerializeField] private Color waterColor = new Color(0.2f, 0.55f, 0.9f, 1f);

        [Header("SSF (Phase 10 Metaball Visual)")]
        [Tooltip("If set, used as the source material for water particles instead of URP/Lit. Assign an SsfElement-based material to enable Screen-Space Fluid rendering.")]
        [SerializeField] private Material customParticleMaterial;
        [Tooltip("Optional layer name applied to spawned particles. Used by SsfRendererFeature's LayerMask to isolate fluid renderers.")]
        [SerializeField] private string fluidLayerName;

        private readonly Stack<WaterParticle> _pool = new Stack<WaterParticle>();
        private int _activeCount;
        private Material _sharedMaterial;
        private PhysicsMaterial _physicsMaterial;
        private Transform _particleRoot;

        public int ActiveCount => _activeCount;
        public int MaxParticles => maxParticles;
        public bool CanSpawn => _activeCount < maxParticles;
        public float ParticleLifetime => particleLifetime;

        private void Awake()
        {
            _particleRoot = new GameObject("WaterParticles").transform;
            _particleRoot.SetParent(transform, false);

            _physicsMaterial = new PhysicsMaterial("WaterLowFriction")
            {
                dynamicFriction = 0.05f,
                staticFriction = 0.05f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            if (customParticleMaterial != null)
            {
                _sharedMaterial = new Material(customParticleMaterial);
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                _sharedMaterial = new Material(shader) { color = waterColor };
            }

            if (_sharedMaterial.HasProperty("_BaseColor"))
            {
                _sharedMaterial.SetColor("_BaseColor", waterColor);
            }
            if (_sharedMaterial.HasProperty("_Radius"))
            {
                _sharedMaterial.SetFloat("_Radius", particleRadius);
            }
        }

        private void OnDestroy()
        {
            if (_sharedMaterial != null) Destroy(_sharedMaterial);
            if (_physicsMaterial != null) Destroy(_physicsMaterial);
        }

        public WaterParticle Spawn(Vector3 position, Vector3 velocity)
        {
            if (!CanSpawn) return null;

            WaterParticle particle = _pool.Count > 0 ? _pool.Pop() : CreateParticle();
            particle.Launch(this, position, velocity, particleLifetime, despawnBelowY);
            _activeCount++;
            return particle;
        }

        public void Recycle(WaterParticle particle)
        {
            if (particle == null) return;
            particle.gameObject.SetActive(false);
            particle.Body.linearVelocity = Vector3.zero;
            particle.Body.angularVelocity = Vector3.zero;
            _pool.Push(particle);
            _activeCount = Mathf.Max(0, _activeCount - 1);
        }

        private WaterParticle CreateParticle()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "WaterDrop";
            go.transform.SetParent(_particleRoot, false);
            go.transform.localScale = Vector3.one * (particleRadius * 2f);

            if (!string.IsNullOrEmpty(fluidLayerName))
            {
                int layer = LayerMask.NameToLayer(fluidLayerName);
                if (layer >= 0) go.layer = layer;
            }

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _sharedMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var sc = go.GetComponent<SphereCollider>();
            sc.radius = 0.5f;
            sc.material = _physicsMaterial;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = particleMass;
            rb.linearDamping = particleDrag;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            go.SetActive(false);
            return go.AddComponent<WaterParticle>();
        }
    }
}
