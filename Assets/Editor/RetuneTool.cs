#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SandWater.Core;
using SandWater.Player;
using SandWater.TerrainSystem;
using SandWater.WaterSim;

namespace SandWater.EditorTools
{
    public static class RetuneTool
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string TerrainDataPath = "Assets/Settings/MainTerrainData.asset";
        private const float TerrainMaxHeight = 30f;
        private const string StageDatabaseAssetPath = "Assets/Resources/StageDatabase.asset";

        [MenuItem("SandWater/Retune for Easier Play")]
        public static void RetuneEasy()
        {
            RetuneToStageIndex(0);
        }

        [MenuItem("SandWater/Retune to Stage 2")]
        public static void RetuneStage2()
        {
            RetuneToStageIndex(1);
        }

        [MenuItem("SandWater/Retune to Stage 3")]
        public static void RetuneToStage3()
        {
            RetuneToStageIndex(2);
        }

        private static void RetuneToStageIndex(int index)
        {
            var db = AssetDatabase.LoadAssetAtPath<StageDatabase>(StageDatabaseAssetPath);
            if (db == null || db.Count == 0)
            {
                Debug.LogWarning("[SandWater] StageDatabase not found. Run Setup Phase 9.2 Stages first.");
                return;
            }
            StagePreset stage = db.Get(index);
            if (stage == null)
            {
                Debug.LogWarning("[SandWater] Stage index " + index + " is out of range.");
                return;
            }
            ApplyStageToMainScene(stage);
        }

        private static void ApplyStageToMainScene(StagePreset stage)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            ResetTerrainData(stage.initialTerrainHeight);

            float surfaceY = TerrainMaxHeight * stage.initialTerrainHeight;

            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var pos = stage.playerStartPosition;
                if (pos.y < surfaceY + 0.5f) pos.y = surfaceY + 1f;
                player.transform.position = pos;
            }

            var source = Object.FindAnyObjectByType<WaterSource>();
            if (source != null)
            {
                source.transform.position = stage.waterSourcePosition;
                SetFloat(source, "spawnRatePerSecond", stage.waterSpawnRatePerSecond);
                SetFloat(source, "spawnSpread", 0.15f);
                SetVector3(source, "initialVelocity", stage.waterInitialVelocity);
            }

            var sim = Object.FindAnyObjectByType<WaterSimulator>();
            if (sim != null)
            {
                SetInt(sim, "maxParticles", 800);
                SetFloat(sim, "particleRadius", 0.1f);
                SetFloat(sim, "particleMass", 0.02f);
            }

            var goal = Object.FindAnyObjectByType<GoalZone>();
            if (goal != null)
            {
                goal.transform.position = stage.goalPosition;
                SetInt(goal, "requiredCount", stage.goalRequiredCount);
                var box = goal.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.size = stage.goalBoxSize;
                    box.isTrigger = true;
                }
                foreach (var childCol in goal.GetComponentsInChildren<Collider>(true))
                {
                    if (childCol.gameObject != goal.gameObject)
                    {
                        Object.DestroyImmediate(childCol);
                    }
                }
            }

            var brush = Object.FindAnyObjectByType<TerrainBrush>();
            if (brush != null)
            {
                SetFloat(brush, "radiusMeters", stage.brushInitialRadius);
                SetFloat(brush, "strength", stage.brushInitialStrength);
                SetFloat(brush, "maxStrength", 30f);
                SetFloat(brush, "strengthStep", 2f);
            }

            var session = Object.FindAnyObjectByType<SessionConfig>();
            if (session != null)
            {
                SetFloat(session, "timeLimitSeconds", stage.timeLimitSeconds);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SandWater] Retune complete for " + stage.stageName + ".");
        }

        private static void ResetTerrainData(float normalizedHeight)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null) return;

            int res = data.heightmapResolution;
            float[,] heights = new float[res, res];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                    heights[z, x] = normalizedHeight;
            data.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(data);
        }

        private static void SetFloat(Object target, string prop, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p != null) { p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetInt(Object target, string prop, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p != null) { p.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void SetVector3(Object target, string prop, Vector3 value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p != null) { p.vector3Value = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
#endif
