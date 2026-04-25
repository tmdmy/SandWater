using System.Collections.Generic;
using UnityEngine;

namespace SandWater.MarchingCubes
{
    /// <summary>
    /// Extracts a triangle mesh from a FluidDensityField via the Marching Cubes algorithm.
    /// Uses non-shared vertices (3 per triangle) for simplicity. Mesh.RecalculateNormals() is
    /// expected to be called by the caller for flat shading; the resulting flat shading is
    /// acceptable for water-like translucent rendering.
    /// </summary>
    public class MarchingCubesGenerator
    {
        private readonly List<Vector3> _vertices = new List<Vector3>(4096);
        private readonly List<int> _triangles = new List<int>(8192);

        private readonly float[] _cornerDensity = new float[8];
        private readonly Vector3[] _edgeVertices = new Vector3[12];

        public List<Vector3> Vertices => _vertices;
        public List<int> Triangles => _triangles;

        public void Generate(FluidDensityField field, float isoValue)
        {
            _vertices.Clear();
            _triangles.Clear();

            var density = field.Density;
            if (density == null || density.Length == 0) return;

            int resX = field.Resolution.x;
            int resY = field.Resolution.y;
            int resZ = field.Resolution.z;
            if (resX < 2 || resY < 2 || resZ < 2) return;

            Vector3 origin = field.Origin;
            Vector3 cellSize = field.CellSize;

            var cornerOffsets = MarchingCubesTables.CornerOffsets;
            var edgeConnections = MarchingCubesTables.EdgeConnections;
            var edgeTable = MarchingCubesTables.EdgeTable;
            var triTable = MarchingCubesTables.TriTable;

            int sliceXY = resX * resY;

            for (int z = 0; z < resZ - 1; z++)
            {
                for (int y = 0; y < resY - 1; y++)
                {
                    for (int x = 0; x < resX - 1; x++)
                    {
                        int cubeIndex = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            int cx = x + cornerOffsets[i, 0];
                            int cy = y + cornerOffsets[i, 1];
                            int cz = z + cornerOffsets[i, 2];
                            float d = density[cx + resX * cy + sliceXY * cz];
                            _cornerDensity[i] = d;
                            if (d >= isoValue) cubeIndex |= (1 << i);
                        }

                        int crossedEdges = edgeTable[cubeIndex];
                        if (crossedEdges == 0) continue;

                        for (int e = 0; e < 12; e++)
                        {
                            if ((crossedEdges & (1 << e)) == 0) continue;
                            int ai = edgeConnections[e, 0];
                            int bi = edgeConnections[e, 1];

                            Vector3 aPos = new Vector3(
                                origin.x + (x + cornerOffsets[ai, 0]) * cellSize.x,
                                origin.y + (y + cornerOffsets[ai, 1]) * cellSize.y,
                                origin.z + (z + cornerOffsets[ai, 2]) * cellSize.z);
                            Vector3 bPos = new Vector3(
                                origin.x + (x + cornerOffsets[bi, 0]) * cellSize.x,
                                origin.y + (y + cornerOffsets[bi, 1]) * cellSize.y,
                                origin.z + (z + cornerOffsets[bi, 2]) * cellSize.z);

                            _edgeVertices[e] = InterpolateEdge(aPos, bPos, _cornerDensity[ai], _cornerDensity[bi], isoValue);
                        }

                        for (int t = 0; t < 16; t += 3)
                        {
                            int e0 = triTable[cubeIndex, t];
                            if (e0 == -1) break;
                            int e1 = triTable[cubeIndex, t + 1];
                            int e2 = triTable[cubeIndex, t + 2];

                            int baseIdx = _vertices.Count;
                            _vertices.Add(_edgeVertices[e0]);
                            _vertices.Add(_edgeVertices[e1]);
                            _vertices.Add(_edgeVertices[e2]);
                            _triangles.Add(baseIdx);
                            _triangles.Add(baseIdx + 1);
                            _triangles.Add(baseIdx + 2);
                        }
                    }
                }
            }
        }

        private static Vector3 InterpolateEdge(Vector3 a, Vector3 b, float densA, float densB, float iso)
        {
            float diff = densB - densA;
            if (Mathf.Abs(diff) < 1e-5f) return (a + b) * 0.5f;
            float t = Mathf.Clamp01((iso - densA) / diff);
            return a + (b - a) * t;
        }
    }
}
