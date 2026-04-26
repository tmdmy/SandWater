using UnityEngine;

namespace SandWater.WitheredVillage
{
    /// <summary>
    /// At runtime Start (after StageApplier has reapplied terrain heights), snaps each direct
    /// child of this GameObject's Y position to the current terrain surface. Use to keep
    /// houses / decorations on the ground when stages change initial terrain height.
    ///
    /// Add this component to:
    ///   - [Village] (snaps House children to terrain)
    ///   - [CustomDecorations] (snaps each Pandazole prefab to terrain)
    ///
    /// Each instance only touches its OWN direct children, so nested decorations are handled
    /// independently and the parent's transform stays put.
    /// </summary>
    [DefaultExecutionOrder(200)] // After StageApplier (100)
    public class VillageGroundAnchor : MonoBehaviour
    {
        [Tooltip("Optional. Auto-found at Start if left empty.")]
        [SerializeField] private Terrain terrain;
        [Tooltip("Extra Y offset applied after terrain snap (e.g. 0.05 to lift slightly).")]
        [SerializeField] private float yOffset = 0f;

        private void Start()
        {
            if (terrain == null) terrain = FindAnyObjectByType<Terrain>();
            if (terrain == null) return;

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                Vector3 p = child.position;
                float terrainY = terrain.SampleHeight(new Vector3(p.x, 0f, p.z)) + terrain.transform.position.y;
                child.position = new Vector3(p.x, terrainY + yOffset, p.z);
            }
        }
    }
}
