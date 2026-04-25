using UnityEngine;

namespace SandWater.TerrainSystem
{
    public static class TerrainHeightSampler
    {
        public static bool TryRaycastTerrain(Ray ray, float maxDistance, out RaycastHit hit, out Terrain terrain)
        {
            terrain = null;
            if (Physics.Raycast(ray, out hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                terrain = hit.collider.GetComponent<Terrain>();
                return terrain != null;
            }
            return false;
        }

        public static Vector2 WorldToNormalized(Terrain terrain, Vector3 worldPos)
        {
            Vector3 local = worldPos - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            return new Vector2(
                Mathf.Clamp01(local.x / size.x),
                Mathf.Clamp01(local.z / size.z));
        }

        public static void NormalizedToHeightmap(Terrain terrain, Vector2 normalized, out int mapX, out int mapZ)
        {
            int res = terrain.terrainData.heightmapResolution;
            mapX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (res - 1)), 0, res - 1);
            mapZ = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (res - 1)), 0, res - 1);
        }

        public static float MetersPerHeightmapCell(Terrain terrain)
        {
            int res = terrain.terrainData.heightmapResolution;
            return terrain.terrainData.size.x / (res - 1);
        }
    }
}
