using System.Collections.Generic;
using UnityEngine;

namespace SandWater.MarchingCubes
{
    /// <summary>
    /// Extracts a triangle mesh from a FluidDensityField via the Marching Cubes algorithm.
    /// Vertex normals are computed from the negated density gradient at each corner and
    /// linearly interpolated along edges (matching the position interpolation parameter).
    /// This produces smoothly shaded surfaces without per-vertex sharing or smoothing groups.
    /// </summary>
    public class MarchingCubesGenerator
    {
        private readonly List<Vector3> _vertices = new List<Vector3>(4096);
        private readonly List<int> _triangles = new List<int>(8192);
        private readonly List<Vector3> _normals = new List<Vector3>(4096);

        private readonly float[] _cornerDensity = new float[8];
        private readonly Vector3[] _cornerGradient = new Vector3[8];
        private readonly Vector3[] _edgeVertices = new Vector3[12];
        private readonly Vector3[] _edgeNormals = new Vector3[12];

        public List<Vector3> Vertices => _vertices;
        public List<int> Triangles => _triangles;
        public List<Vector3> Normals => _normals;

        public void Generate(FluidDensityField field, float isoValue)
        {
            _vertices.Clear();
            _triangles.Clear();
            _normals.Clear();

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
                            _cornerGradient[i] = field.SampleGradient(cx, cy, cz);
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

                            float densA = _cornerDensity[ai];
                            float densB = _cornerDensity[bi];
                            float t = ComputeT(densA, densB, isoValue);

                            _edgeVertices[e] = aPos + (bPos - aPos) * t;
                            Vector3 nrm = _cornerGradient[ai] + (_cornerGradient[bi] - _cornerGradient[ai]) * t;
                            float magnitude = nrm.magnitude;
                            _edgeNormals[e] = magnitude > 1e-6f ? nrm / magnitude : Vector3.up;
                        }

                        for (int ti = 0; ti < 16; ti += 3)
                        {
                            int e0 = triTable[cubeIndex, ti];
                            if (e0 == -1) break;
                            int e1 = triTable[cubeIndex, ti + 1];
                            int e2 = triTable[cubeIndex, ti + 2];

                            int baseIdx = _vertices.Count;
                            _vertices.Add(_edgeVertices[e0]);
                            _vertices.Add(_edgeVertices[e1]);
                            _vertices.Add(_edgeVertices[e2]);
                            _normals.Add(_edgeNormals[e0]);
                            _normals.Add(_edgeNormals[e1]);
                            _normals.Add(_edgeNormals[e2]);
                            _triangles.Add(baseIdx);
                            _triangles.Add(baseIdx + 1);
                            _triangles.Add(baseIdx + 2);
                        }
                    }
                }
            }
        }

        private static float ComputeT(float densA, float densB, float iso)
        {
            float diff = densB - densA;
            if (Mathf.Abs(diff) < 1e-5f) return 0.5f;
            return Mathf.Clamp01((iso - densA) / diff);
        }
    }
}
