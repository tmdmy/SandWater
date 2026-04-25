using UnityEngine;
using SandWater.WaterSim;

namespace SandWater.WitheredVillage
{
    /// <summary>
    /// Drives the visual water level inside the well as GoalZone progress increases.
    /// Animates a child Cylinder transform's localScale.y and localPosition.y so that
    /// the cylinder's bottom remains anchored at <see cref="bottomLocalY"/> while the
    /// top rises toward <see cref="bottomLocalY + fullHeight"/> when the goal is full.
    /// </summary>
    public class WellWaterLevel : MonoBehaviour
    {
        [SerializeField] private GoalZone goalZone;
        [SerializeField] private Transform waterCylinder;

        [Header("Water Level (local space, well's frame)")]
        [Tooltip("Local Y at which the cylinder's bottom is anchored (well floor).")]
        [SerializeField] private float bottomLocalY = -0.5f;
        [Tooltip("Cylinder height (along Y in world units) when the well is empty.")]
        [SerializeField, Range(0.01f, 0.3f)] private float emptyHeight = 0.05f;
        [Tooltip("Cylinder height (along Y in world units) when the well is full.")]
        [SerializeField, Range(0.3f, 3f)] private float fullHeight = 0.8f;
        [Tooltip("How fast the water visual catches up to the target height per second.")]
        [SerializeField, Range(0.1f, 30f)] private float lerpSpeed = 4f;

        private float _currentHeight;

        private void Awake()
        {
            if (goalZone == null) goalZone = GetComponent<GoalZone>();
            _currentHeight = emptyHeight;
            ApplyHeight(_currentHeight);
        }

        private void Update()
        {
            if (goalZone == null || waterCylinder == null) return;
            float target = Mathf.Lerp(emptyHeight, fullHeight, goalZone.Progress);
            _currentHeight = Mathf.MoveTowards(_currentHeight, target, lerpSpeed * Time.deltaTime);
            ApplyHeight(_currentHeight);
        }

        private void ApplyHeight(float height)
        {
            // Unity primitive Cylinder has height 2 at localScale.y = 1; convert world height to localScale.y.
            float scaleY = height * 0.5f;

            var s = waterCylinder.localScale;
            waterCylinder.localScale = new Vector3(s.x, scaleY, s.z);

            var p = waterCylinder.localPosition;
            waterCylinder.localPosition = new Vector3(p.x, bottomLocalY + height * 0.5f, p.z);
        }
    }
}
