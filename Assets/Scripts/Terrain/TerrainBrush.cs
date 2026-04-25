using UnityEngine;
using SandWater.Inputs;

namespace SandWater.TerrainSystem
{
    public class TerrainBrush : MonoBehaviour
    {
        [SerializeField] private float radiusMeters = 4f;
        [SerializeField] private float minRadius = 0.5f;
        [SerializeField] private float maxRadius = 20f;
        [SerializeField] private float radiusScrollSpeed = 2f;

        [SerializeField] private float strength = 8f;
        [SerializeField] private float minStrength = 0.5f;
        [SerializeField] private float maxStrength = 30f;
        [SerializeField] private float strengthStep = 2f;

        [SerializeField]
        private AnimationCurve falloff = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, 0f));

        public float RadiusMeters => radiusMeters;
        public float Strength => strength;
        public AnimationCurve Falloff => falloff;

        private void OnEnable()
        {
            var input = InputHandler.Instance;
            if (input == null) return;
            input.BrushStrengthIncreased += IncreaseStrength;
            input.BrushStrengthDecreased += DecreaseStrength;
        }

        private void OnDisable()
        {
            var input = InputHandler.Instance;
            if (input == null) return;
            input.BrushStrengthIncreased -= IncreaseStrength;
            input.BrushStrengthDecreased -= DecreaseStrength;
        }

        private void Update()
        {
            var input = InputHandler.Instance;
            if (input == null) return;

            float scroll = input.BrushResizeDelta;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                radiusMeters = Mathf.Clamp(
                    radiusMeters + Mathf.Sign(scroll) * radiusScrollSpeed * Time.deltaTime * 10f,
                    minRadius, maxRadius);
            }
        }

        private void IncreaseStrength()
        {
            strength = Mathf.Clamp(strength + strengthStep, minStrength, maxStrength);
        }

        private void DecreaseStrength()
        {
            strength = Mathf.Clamp(strength - strengthStep, minStrength, maxStrength);
        }

        public float EvaluateFalloff(float normalizedDistance)
        {
            return Mathf.Clamp01(falloff.Evaluate(Mathf.Clamp01(normalizedDistance)));
        }

        public void Configure(float initialRadius, float initialStrength)
        {
            radiusMeters = Mathf.Clamp(initialRadius, minRadius, maxRadius);
            strength = Mathf.Clamp(initialStrength, minStrength, maxStrength);
        }
    }
}
