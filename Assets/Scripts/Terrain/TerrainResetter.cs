using UnityEngine;
using SandWater.Inputs;

namespace SandWater.TerrainSystem
{
    public class TerrainResetter : MonoBehaviour
    {
        [SerializeField] private Terrain targetTerrain;

        private float[,] _initialHeights;
        private int _res;
        private TerrainData _runtimeData;

        private void Awake()
        {
            if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
            if (targetTerrain == null) return;

            _runtimeData = Instantiate(targetTerrain.terrainData);
            targetTerrain.terrainData = _runtimeData;
            var collider = targetTerrain.GetComponent<TerrainCollider>();
            if (collider != null) collider.terrainData = _runtimeData;

            _res = _runtimeData.heightmapResolution;
            _initialHeights = _runtimeData.GetHeights(0, 0, _res, _res);
        }

        private void OnDestroy()
        {
            if (_runtimeData != null) Destroy(_runtimeData);
        }

        private void OnEnable()
        {
            var input = InputHandler.Instance;
            if (input != null) input.RetryPressed += ResetTerrain;
        }

        private void OnDisable()
        {
            var input = InputHandler.Instance;
            if (input != null) input.RetryPressed -= ResetTerrain;
        }

        public void ResetTerrain()
        {
            if (_runtimeData == null || _initialHeights == null) return;
            _runtimeData.SetHeights(0, 0, _initialHeights);
        }

        public void SetInitialHeight(float normalized)
        {
            if (_runtimeData == null || _initialHeights == null) return;
            float v = Mathf.Clamp01(normalized);
            for (int z = 0; z < _res; z++)
                for (int x = 0; x < _res; x++)
                    _initialHeights[z, x] = v;
            _runtimeData.SetHeights(0, 0, _initialHeights);
        }
    }
}
