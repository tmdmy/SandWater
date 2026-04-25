using UnityEngine;
using SandWater.Core;
using SandWater.Inputs;

namespace SandWater.TerrainSystem
{
    public class TerrainEditor : MonoBehaviour
    {
        [SerializeField] private Terrain targetTerrain;
        [SerializeField] private TerrainBrush brush;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float maxRayDistance = 200f;

        private void Awake()
        {
            if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
            if (viewCamera == null) viewCamera = Camera.main;
            if (brush == null) brush = GetComponent<TerrainBrush>();
        }

        private void Update()
        {
            if (targetTerrain == null || brush == null || viewCamera == null) return;
            var input = InputHandler.Instance;
            if (input == null) return;

            bool dig = input.DigHeld;
            bool raise = input.RaiseHeld;
            if (!dig && !raise) return;

            int sign = dig && !raise ? -1 : raise && !dig ? 1 : 0;
            if (sign == 0) return;

            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!TerrainHeightSampler.TryRaycastTerrain(ray, maxRayDistance, out var hit, out var terrain)) return;
            if (terrain != targetTerrain) return;

            ApplyBrush(terrain, hit.point, sign);

            var audio = AudioManager.Instance;
            if (audio != null)
            {
                if (sign < 0) audio.PlayDig();
                else audio.PlayRaise();
            }
        }

        private void ApplyBrush(Terrain terrain, Vector3 worldCenter, int sign)
        {
            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            float cellSize = TerrainHeightSampler.MetersPerHeightmapCell(terrain);

            Vector2 normalized = TerrainHeightSampler.WorldToNormalized(terrain, worldCenter);
            TerrainHeightSampler.NormalizedToHeightmap(terrain, normalized, out int centerX, out int centerZ);

            int radiusCells = Mathf.Max(1, Mathf.CeilToInt(brush.RadiusMeters / cellSize));
            int minX = Mathf.Max(0, centerX - radiusCells);
            int maxX = Mathf.Min(res - 1, centerX + radiusCells);
            int minZ = Mathf.Max(0, centerZ - radiusCells);
            int maxZ = Mathf.Min(res - 1, centerZ + radiusCells);

            int w = maxX - minX + 1;
            int h = maxZ - minZ + 1;
            if (w <= 0 || h <= 0) return;

            float[,] heights = data.GetHeights(minX, minZ, w, h);
            float heightScale = data.size.y;
            float normalizedStrength = (brush.Strength * Time.deltaTime) / heightScale;

            for (int z = 0; z < h; z++)
            {
                int worldZ = minZ + z;
                for (int x = 0; x < w; x++)
                {
                    int worldX = minX + x;
                    float dx = (worldX - centerX) * cellSize;
                    float dz = (worldZ - centerZ) * cellSize;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    float t = dist / brush.RadiusMeters;
                    if (t >= 1f) continue;

                    float falloff = brush.EvaluateFalloff(t);
                    float newHeight = heights[z, x] + sign * normalizedStrength * falloff;
                    heights[z, x] = Mathf.Clamp01(newHeight);
                }
            }

            data.SetHeightsDelayLOD(minX, minZ, heights);
            data.SyncHeightmap();
        }
    }
}
