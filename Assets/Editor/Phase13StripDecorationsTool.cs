#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SandWater.WaterSim;
using SandWater.WitheredVillage;

namespace SandWater.EditorTools
{
    /// <summary>
    /// Strips all Phase 12 nature decorations (dead trees, withered crops, withered grass,
    /// hidden revival objects) from the Main scene, keeping only the well (children of
    /// GoalZone) and the houses inside [Village]. Used when transitioning to Asset Store
    /// prefabs in Phase 13: cleans the slate so the new prefab-based decorator can run
    /// without colliding with the primitive-built ones.
    ///
    /// This menu does NOT auto-run. Invoke explicitly.
    /// </summary>
    public static class Phase13StripDecorationsTool
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string VillageGoName = "[Village]";
        private const string WastesGoName = "[Wastes]";
        private const string WellChildName = "Well";

        [MenuItem("SandWater/Strip Phase 12 Decorations (keep well + houses)")]
        public static void StripDecorations()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // 1. Remove [Wastes] entirely (it has no houses or other "keep" objects).
            var wastes = GameObject.Find(WastesGoName);
            if (wastes != null)
            {
                Object.DestroyImmediate(wastes);
            }

            // 2. In [Village], keep only House children. Remove DeadTree / WitheredCropPatch / etc.
            int villageRemoved = 0;
            var village = GameObject.Find(VillageGoName);
            if (village != null)
            {
                var toRemove = new List<GameObject>();
                for (int i = 0; i < village.transform.childCount; i++)
                {
                    var child = village.transform.GetChild(i);
                    if (!child.name.StartsWith("House"))
                    {
                        toRemove.Add(child.gameObject);
                    }
                }
                foreach (var go in toRemove)
                {
                    Object.DestroyImmediate(go);
                    villageRemoved++;
                }
            }

            // 3. Remove VillageRevival component on the GoalZone — its references would dangle
            //    and most of the targets are gone anyway.
            var goal = Object.FindAnyObjectByType<GoalZone>();
            if (goal != null)
            {
                var revival = goal.GetComponent<VillageRevival>();
                if (revival != null) Object.DestroyImmediate(revival);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SandWater] Phase 13 strip complete: removed [Wastes], removed {villageRemoved} non-house children from [Village], removed VillageRevival. Well (children of GoalZone) and houses retained.");
        }
    }
}
#endif
