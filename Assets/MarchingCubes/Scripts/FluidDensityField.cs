using System.Collections.Generic;
using UnityEngine;

namespace SandWater.MarchingCubes
{
    /// <summary>
    /// 3D scalar field that accumulates a smooth metaball-like contribution from each particle.
    /// The field is sampled by the Marching Cubes generator to extract an isosurface.
    ///
    /// Cell layout: index = x + resolution.x * (y + resolution.y * z)
    /// World position of a cell corner: origin + (cell index) * cellSize
    /// </summary>
    public class FluidDensityField
    {
        private float[] _density;

        public Vector3Int Resolution { get; private set; }
        public Vector3 Origin { get; private set; }
        public Vector3 CellSize { get; private set; }
        public float[] Density => _density;
        public int CellCount => Resolution.x * Resolution.y * Resolution.z;

        public void Resize(Vector3Int resolution, Vector3 origin, Vector3 cellSize)
        {
            Resolution = new Vector3Int(Mathf.Max(2, resolution.x), Mathf.Max(2, resolution.y), Mathf.Max(2, resolution.z));
            Origin = origin;
            CellSize = cellSize;
            int count = Resolution.x * Resolution.y * Resolution.z;
            if (_density == null || _density.Length != count)
            {
                _density = new float[count];
            }
        }

        public void Clear()
        {
            if (_density == null) return;
            System.Array.Clear(_density, 0, _density.Length);
        }

        public float Sample(int x, int y, int z)
        {
            return _density[x + Resolution.x * (y + Resolution.y * z)];
        }

        /// <summary>
        /// Splat each particle into the field as a smooth metaball:
        ///   contribution(r) = (1 - (r/influenceRadius)^2)^2  for r &lt; influenceRadius, else 0.
        /// </summary>
        public void AccumulateParticles(IList<Vector3> worldPositions, float influenceRadius)
        {
            if (worldPositions == null || worldPositions.Count == 0 || _density == null) return;
            if (influenceRadius <= 0f) return;

            float invCellX = 1f / CellSize.x;
            float invCellY = 1f / CellSize.y;
            float invCellZ = 1f / CellSize.z;
            float r2 = influenceRadius * influenceRadius;
            float invR2 = 1f / r2;

            int resX = Resolution.x;
            int resY = Resolution.y;
            int resZ = Resolution.z;

            for (int p = 0; p < worldPositions.Count; p++)
            {
                Vector3 wp = worldPositions[p];
                Vector3 local = wp - Origin;

                int minX = Mathf.Max(0, Mathf.FloorToInt((local.x - influenceRadius) * invCellX));
                int maxX = Mathf.Min(resX - 1, Mathf.CeilToInt((local.x + influenceRadius) * invCellX));
                int minY = Mathf.Max(0, Mathf.FloorToInt((local.y - influenceRadius) * invCellY));
                int maxY = Mathf.Min(resY - 1, Mathf.CeilToInt((local.y + influenceRadius) * invCellY));
                int minZ = Mathf.Max(0, Mathf.FloorToInt((local.z - influenceRadius) * invCellZ));
                int maxZ = Mathf.Min(resZ - 1, Mathf.CeilToInt((local.z + influenceRadius) * invCellZ));

                if (minX > maxX || minY > maxY || minZ > maxZ) continue;

                for (int z = minZ; z <= maxZ; z++)
                {
                    float cz = Origin.z + z * CellSize.z - wp.z;
                    float cz2 = cz * cz;
                    int zStride = Resolution.x * Resolution.y * z;

                    for (int y = minY; y <= maxY; y++)
                    {
                        float cy = Origin.y + y * CellSize.y - wp.y;
                        float cy2 = cy * cy;
                        int yStride = Resolution.x * y;

                        for (int x = minX; x <= maxX; x++)
                        {
                            float cx = Origin.x + x * CellSize.x - wp.x;
                            float distSq = cx * cx + cy2 + cz2;
                            if (distSq >= r2) continue;

                            float t = 1f - distSq * invR2;
                            _density[x + yStride + zStride] += t * t;
                        }
                    }
                }
            }
        }
    }
}
