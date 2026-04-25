using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SandWater.WaterSim;

namespace SandWater.MarchingCubes
{
    /// <summary>
    /// Coordinates the Marching Cubes pipeline:
    /// each LateUpdate, gather active particle positions from WaterSimulator,
    /// rebuild the density field, run MC, and update the attached Mesh.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FluidMeshRenderer : MonoBehaviour
    {
        [SerializeField] private WaterSimulator simulator;

        [Header("Field Volume (world space)")]
        [Tooltip("Center of the volume that the density field covers. Should enclose both the water source above ground and the flow area below.")]
        [SerializeField] private Vector3 fieldCenter = new Vector3(0f, 7f, 0f);
        [Tooltip("Size (width / height / depth) of the volume.")]
        [SerializeField] private Vector3 fieldSize = new Vector3(40f, 14f, 40f);

        [Header("Grid Resolution")]
        [Tooltip("Number of cells in each axis. Higher = smoother surface but slower.")]
        [SerializeField] private Vector3Int gridResolution = new Vector3Int(48, 28, 48);

        [Header("Surface")]
        [Tooltip("Radius (world units) over which each particle contributes to the field. Should be larger than cell size (fieldSize/gridResolution) so particles bridge multiple cells.")]
        [SerializeField, Range(0.2f, 4f)] private float particleInfluenceRadius = 1.0f;
        [Tooltip("Density threshold for the iso-surface. Lower = puffier surface, higher = thinner.")]
        [SerializeField, Range(0.05f, 3f)] private float isoValue = 0.15f;

        [Header("Debug")]
        [SerializeField] private bool drawFieldGizmo = true;

        private FluidDensityField _field;
        private MarchingCubesGenerator _generator;
        private Mesh _mesh;
        private MeshFilter _filter;
        private readonly List<Vector3> _positionBuffer = new List<Vector3>(800);

        private void Awake()
        {
            _field = new FluidDensityField();
            _generator = new MarchingCubesGenerator();

            _filter = GetComponent<MeshFilter>();
            _mesh = new Mesh { name = "FluidMcMesh" };
            _mesh.indexFormat = IndexFormat.UInt32;
            _mesh.MarkDynamic();
            _filter.sharedMesh = _mesh;

            if (simulator == null) simulator = FindAnyObjectByType<WaterSimulator>();
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        private void LateUpdate()
        {
            if (simulator == null || _mesh == null) return;

            Vector3 origin = fieldCenter - fieldSize * 0.5f;
            int rx = Mathf.Max(2, gridResolution.x);
            int ry = Mathf.Max(2, gridResolution.y);
            int rz = Mathf.Max(2, gridResolution.z);
            Vector3 cellSize = new Vector3(
                fieldSize.x / (rx - 1),
                fieldSize.y / (ry - 1),
                fieldSize.z / (rz - 1));

            _field.Resize(new Vector3Int(rx, ry, rz), origin, cellSize);
            _field.Clear();

            simulator.GetActiveParticlePositions(_positionBuffer);
            _field.AccumulateParticles(_positionBuffer, particleInfluenceRadius);

            _generator.Generate(_field, isoValue);

            _mesh.Clear();
            if (_generator.Vertices.Count > 0)
            {
                _mesh.SetVertices(_generator.Vertices);
                _mesh.SetTriangles(_generator.Triangles, 0);
                _mesh.RecalculateNormals();
                _mesh.RecalculateBounds();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawFieldGizmo) return;
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
            Gizmos.DrawWireCube(fieldCenter, fieldSize);
        }
    }
}
