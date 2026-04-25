using UnityEngine;
using SandWater.Player;
using SandWater.TerrainSystem;
using SandWater.WaterSim;

namespace SandWater.Core
{
    [DefaultExecutionOrder(100)]
    public class StageApplier : MonoBehaviour
    {
        [SerializeField] private StagePreset fallbackStage;

        private void Start()
        {
            var gm = GameManager.Instance;
            var stage = gm != null && gm.State != null ? gm.State.SelectedStage : null;
            if (stage == null) stage = fallbackStage;
            if (stage == null) return;

            Apply(stage);
        }

        private void Apply(StagePreset stage)
        {
            var resetter = FindAnyObjectByType<TerrainResetter>();
            if (resetter != null)
            {
                resetter.SetInitialHeight(stage.initialTerrainHeight);
            }

            var brush = FindAnyObjectByType<TerrainBrush>();
            if (brush != null)
            {
                brush.Configure(stage.brushInitialRadius, stage.brushInitialStrength);
            }

            var source = FindAnyObjectByType<WaterSource>();
            if (source != null)
            {
                source.transform.position = stage.waterSourcePosition;
                source.Configure(stage.waterInitialVelocity, stage.waterSpawnRatePerSecond);
            }

            var goal = FindAnyObjectByType<GoalZone>();
            if (goal != null)
            {
                goal.transform.position = stage.goalPosition;
                goal.Configure(stage.goalRequiredCount, stage.goalBoxSize);
            }

            var session = FindAnyObjectByType<SessionConfig>();
            if (session != null)
            {
                session.SetTimeLimit(stage.timeLimitSeconds);
            }
            else
            {
                var gm = GameManager.Instance;
                if (gm != null && gm.State != null)
                {
                    gm.State.TimeLimit = stage.timeLimitSeconds;
                }
            }

            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                WarpPlayer(player, stage.playerStartPosition);
            }
        }

        private static void WarpPlayer(PlayerController player, Vector3 position)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = position;
            if (cc != null) cc.enabled = true;
        }
    }
}
