using System.Collections.Generic;
using UnityEngine;

namespace SandWater.Core
{
    [CreateAssetMenu(menuName = "SandWater/Stage Database", fileName = "StageDatabase")]
    public class StageDatabase : ScriptableObject
    {
        public const string ResourcePath = "StageDatabase";

        [SerializeField] private List<StagePreset> stages = new List<StagePreset>();

        public IReadOnlyList<StagePreset> Stages => stages;
        public int Count => stages != null ? stages.Count : 0;

        public StagePreset Get(int index)
        {
            if (stages == null || index < 0 || index >= stages.Count) return null;
            return stages[index];
        }

        public int IndexOf(StagePreset preset)
        {
            if (stages == null || preset == null) return -1;
            return stages.IndexOf(preset);
        }

        public static StageDatabase LoadFromResources()
        {
            return Resources.Load<StageDatabase>(ResourcePath);
        }
    }
}
