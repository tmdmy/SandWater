using UnityEngine;

namespace SandWater.TerrainSystem
{
    [RequireComponent(typeof(LineRenderer))]
    public class BrushIndicator : MonoBehaviour
    {
        [SerializeField] private TerrainBrush brush;
        [SerializeField] private Terrain targetTerrain;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private int segments = 48;
        [SerializeField] private float heightOffset = 0.05f;
        [SerializeField] private float maxRayDistance = 200f;
        [SerializeField] private Color lineColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float lineWidth = 0.08f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.loop = true;
            _line.useWorldSpace = true;
            _line.positionCount = segments;
            _line.widthMultiplier = lineWidth;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startColor = _line.endColor = lineColor;

            if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
            if (viewCamera == null) viewCamera = Camera.main;
            if (brush == null) brush = FindAnyObjectByType<TerrainBrush>();
        }

        private void LateUpdate()
        {
            if (brush == null || targetTerrain == null || viewCamera == null)
            {
                _line.enabled = false;
                return;
            }

            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!TerrainHeightSampler.TryRaycastTerrain(ray, maxRayDistance, out var hit, out var terrain) || terrain != targetTerrain)
            {
                _line.enabled = false;
                return;
            }

            _line.enabled = true;
            float radius = brush.RadiusMeters;
            Vector3 center = hit.point;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                float y = targetTerrain.SampleHeight(new Vector3(x, 0f, z)) + targetTerrain.transform.position.y + heightOffset;
                _line.SetPosition(i, new Vector3(x, y, z));
            }
        }
    }
}
