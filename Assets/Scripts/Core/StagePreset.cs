using UnityEngine;

namespace SandWater.Core
{
    [CreateAssetMenu(menuName = "SandWater/Stage Preset", fileName = "StagePreset")]
    public class StagePreset : ScriptableObject
    {
        [Header("Identification")]
        public string stageName = "Stage";
        [TextArea] public string description = "";

        [Header("Terrain")]
        [Tooltip("Normalized initial height of the sand terrain (0-1).")]
        [Range(0f, 1f)] public float initialTerrainHeight = 0.3f;

        [Header("Player")]
        public Vector3 playerStartPosition = new Vector3(0f, 10f, 0f);

        [Header("Water Source")]
        public Vector3 waterSourcePosition = new Vector3(-15f, 11f, 0f);
        public Vector3 waterInitialVelocity = new Vector3(5f, -1f, 0f);
        public float waterSpawnRatePerSecond = 40f;

        [Header("Goal")]
        public Vector3 goalPosition = new Vector3(8f, 9f, 0f);
        public Vector3 goalBoxSize = new Vector3(4f, 8f, 4f);
        public int goalRequiredCount = 10;

        [Header("Session")]
        [Tooltip("0 = unlimited")] public float timeLimitSeconds = 120f;

        [Header("Brush")]
        public float brushInitialRadius = 4f;
        public float brushInitialStrength = 8f;
    }
}
