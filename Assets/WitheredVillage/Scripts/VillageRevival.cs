using System.Collections.Generic;
using UnityEngine;
using SandWater.WaterSim;

namespace SandWater.WitheredVillage
{
    /// <summary>
    /// Watches GoalZone progress; when the threshold is crossed (typically full), triggers a
    /// one-shot transformation that turns the withered village green: swaps "withered" materials
    /// to "alive" materials on every renderer found in the supplied roots, and activates a list
    /// of objects that were hidden until revival (extra plants, tree leaves, flowers).
    ///
    /// Designed to fire once. The transformation is instant — the dramatic "all at once"
    /// feel is the point.
    /// </summary>
    public class VillageRevival : MonoBehaviour
    {
        [SerializeField] private GoalZone goalZone;
        [SerializeField] private Transform[] revivableRoots;

        [Header("Material swaps")]
        [SerializeField] private Material witheredMaterial;
        [SerializeField] private Material aliveMaterial;

        [Header("Reveal-on-Revival")]
        [Tooltip("These GameObjects are deactivated at Start and re-activated when revival fires.")]
        [SerializeField] private List<GameObject> hiddenOnStart = new List<GameObject>();

        [Header("Hide-on-Revival")]
        [Tooltip("These GameObjects are visible at start and deactivated when revival fires (e.g. withered prefabs that should disappear when the oasis returns).")]
        [SerializeField] private List<GameObject> hideOnRevival = new List<GameObject>();

        [Header("Tuning")]
        [SerializeField, Range(0.5f, 1f)] private float revivalThreshold = 0.99f;

        private bool _revived;

        private void Start()
        {
            int hiddenCount = 0;
            for (int i = 0; i < hiddenOnStart.Count; i++)
            {
                if (hiddenOnStart[i] != null)
                {
                    hiddenOnStart[i].SetActive(false);
                    hiddenCount++;
                }
            }
            Debug.Log($"[VillageRevival] Start: hidden {hiddenCount}/{hiddenOnStart.Count} objects, threshold={revivalThreshold}, goalZone={(goalZone != null ? "OK" : "NULL")}, withered={(witheredMaterial != null ? witheredMaterial.name : "NULL")}, alive={(aliveMaterial != null ? aliveMaterial.name : "NULL")}, roots={revivableRoots?.Length ?? 0}");
        }

        private void Update()
        {
            if (_revived || goalZone == null) return;
            if (goalZone.Progress >= revivalThreshold)
            {
                Revive();
                _revived = true;
            }
        }

        private void Revive()
        {
            int swapped = 0;
            int swapCandidates = 0;

            if (witheredMaterial != null && aliveMaterial != null)
            {
                for (int i = 0; i < revivableRoots.Length; i++)
                {
                    var root = revivableRoots[i];
                    if (root == null) continue;
                    var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                    swapCandidates += renderers.Length;
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        var mr = renderers[r];
                        if (mr.sharedMaterial == null) continue;
                        // Match either by reference (same Material asset) or by name (after scene
                        // reload Unity may rebind to a new instance with the same name).
                        bool match = mr.sharedMaterial == witheredMaterial
                            || (mr.sharedMaterial.name != null && mr.sharedMaterial.name.StartsWith(witheredMaterial.name));
                        if (match)
                        {
                            mr.sharedMaterial = aliveMaterial;
                            swapped++;
                        }
                    }
                }
            }

            int activated = 0;
            for (int i = 0; i < hiddenOnStart.Count; i++)
            {
                if (hiddenOnStart[i] != null)
                {
                    hiddenOnStart[i].SetActive(true);
                    activated++;
                }
            }

            int deactivated = 0;
            for (int i = 0; i < hideOnRevival.Count; i++)
            {
                if (hideOnRevival[i] != null)
                {
                    hideOnRevival[i].SetActive(false);
                    deactivated++;
                }
            }

            Debug.Log($"[VillageRevival] Revived: swapped {swapped}/{swapCandidates} materials, activated {activated}/{hiddenOnStart.Count} hidden, deactivated {deactivated}/{hideOnRevival.Count} withered.");
        }
    }
}
