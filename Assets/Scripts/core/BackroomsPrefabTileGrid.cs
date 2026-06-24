using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BackroomsPrefabTileGrid : MonoBehaviour
{
    public GameObject tilePrefab;
    public float mapSize = 200f;
    public int extraBorderTiles = 1;
    public int skipEdgeTiles = 0;
    public bool rebuildOnEnable = false; // set false to preserve the current map

    // MAP LOCKED: the tile grid is frozen as currently authored in the scene.
    // While true, no automatic or manual rebuild/fill runs. Set to false ONLY
    // if you deliberately want to regenerate tiles again.
    private const bool MapLocked = true;

    private const string GeneratedRootName = "Generated_Backrooms_Tile_Grid";
    private const string GeneratedTileName = "Generated_Backrooms_Fill_Tile";

    private void OnEnable()
    {
        if (MapLocked)
        {
            return;
        }

        if (rebuildOnEnable && !Application.isPlaying)
        {
            RebuildGrid();
        }
    }

    // ── One-time tile patch fill ──────────────────────────────────────────────
    // Adjust these then right-click → Fill Missing Tile Patch.
    // Uses a separate root so Rebuild Grid won't wipe these tiles.

    [Header("Missing Patch Fill (run once)")]
    [Tooltip("World-space X min of the missing patch.")]
    public float patchMinX = 48f;
    [Tooltip("World-space X max of the missing patch.")]
    public float patchMaxX = 100f;
    [Tooltip("World-space Z min of the missing patch.")]
    public float patchMinZ = -100f;
    [Tooltip("World-space Z max of the missing patch.")]
    public float patchMaxZ = -96f;
    [Tooltip("Tile step size in X. Leave 0 to auto-detect from prefab bounds.")]
    public float patchStepX = 0f;
    [Tooltip("Tile step size in Z. Leave 0 to auto-detect from prefab bounds.")]
    public float patchStepZ = 0f;

    private const string FillRootName = "Generated_Backrooms_Tile_Fill";

    [ContextMenu("Fill Missing Tile Patch")]
    public void FillMissingTilePatch()
    {
        if (MapLocked)
        {
            Debug.LogWarning("BackroomsPrefabTileGrid: fill is disabled (MapLocked = true). The map is frozen. Set MapLocked = false in BackroomsPrefabTileGrid.cs to change tiles.", this);
            return;
        }

        if (tilePrefab == null)
        {
            Debug.LogWarning("BackroomsPrefabTileGrid: assign tilePrefab first.", this);
            return;
        }

        float stepX = patchStepX;
        float stepZ = patchStepZ;

        // Auto-detect from prefab bounds if not set manually
        if (stepX <= 0.01f || stepZ <= 0.01f)
        {
            if (TryGetPrefabBounds(out Bounds b))
            {
                if (stepX <= 0.01f) stepX = b.size.x;
                if (stepZ <= 0.01f) stepZ = b.size.z;
            }
        }

        if (stepX <= 0.01f || stepZ <= 0.01f)
        {
            Debug.LogWarning("BackroomsPrefabTileGrid: could not determine tile size. Set patchStepX/Z manually.", this);
            return;
        }

        // Find or create a fill root immune to ClearGeneratedGrid()
        Transform fillRoot = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name == FillRootName)
            {
                fillRoot = transform.GetChild(i);
                break;
            }
        }
        if (fillRoot == null)
        {
            GameObject r = new GameObject(FillRootName);
            r.transform.SetParent(transform, false);
            fillRoot = r.transform;
        }

        // Snap start to grid
        float startX = patchMinX;
        float startZ = patchMinZ;

        int placed = 0;
        for (float wx = startX; wx <= patchMaxX + 0.01f; wx += stepX)
        {
            for (float wz = startZ; wz <= patchMaxZ + 0.01f; wz += stepZ)
            {
                GameObject tile = InstantiateTile(fillRoot);
                tile.name = "Fill_Tile_" + wx.ToString("F0") + "_" + wz.ToString("F0");
                // localPosition because the TileGrid parent may be offset
                tile.transform.localPosition = new Vector3(wx, 0f, wz);
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale    = Vector3.one;
                placed++;
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            gameObject.scene);
#endif

        Debug.Log($"[BackroomsPrefabTileGrid] FillMissingTilePatch placed {placed} tiles.");
    }

    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        if (MapLocked)
        {
            Debug.LogWarning("BackroomsPrefabTileGrid: rebuild is disabled (MapLocked = true). The map is frozen. Set MapLocked = false in BackroomsPrefabTileGrid.cs to regenerate.", this);
            return;
        }

        if (tilePrefab == null || mapSize <= 0f)
        {
            return;
        }

        if (!TryGetPrefabBounds(out Bounds bounds))
        {
            Debug.LogWarning("BackroomsPrefabTileGrid could not read tile prefab bounds.", this);
            return;
        }

        float stepX = bounds.size.x;
        float stepZ = bounds.size.z;

        if (stepX <= 0.01f || stepZ <= 0.01f)
        {
            Debug.LogWarning("BackroomsPrefabTileGrid tile prefab bounds are too small.", this);
            return;
        }

        ClearGeneratedGrid();

        Transform root = CreateGeneratedRoot();
        int countX = Mathf.CeilToInt(mapSize / stepX) + (extraBorderTiles * 2);
        int countZ = Mathf.CeilToInt(mapSize / stepZ) + (extraBorderTiles * 2);
        countX = Mathf.Max(1, countX);
        countZ = Mathf.Max(1, countZ);

        float startX = -((countX - 1) * stepX) * 0.5f;
        float startZ = -((countZ - 1) * stepZ) * 0.5f;
        int edgeSkip = Mathf.Clamp(skipEdgeTiles, 0, Mathf.Min(countX, countZ) / 2);
        for (int x = edgeSkip; x < countX - edgeSkip; x++)
        {
            for (int z = edgeSkip; z < countZ - edgeSkip; z++)
            {
                GameObject tile = InstantiateTile(root);
                tile.name = GeneratedTileName + "_" + x + "_" + z;
                tile.transform.localPosition = new Vector3(startX + (x * stepX), 0f, startZ + (z * stepZ));
                tile.transform.localRotation = Quaternion.identity;
                tile.transform.localScale = Vector3.one;
            }
        }
    }

    private bool TryGetPrefabBounds(out Bounds bounds)
    {
        GameObject sample = Instantiate(tilePrefab);
        sample.hideFlags = HideFlags.HideAndDontSave;
        sample.transform.position = Vector3.zero;
        sample.transform.rotation = Quaternion.identity;
        sample.transform.localScale = Vector3.one;

        Renderer[] renderers = sample.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(Vector3.zero, Vector3.zero);

        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        DestroyGeneratedObject(sample);
        return hasBounds;
    }

    private Transform CreateGeneratedRoot()
    {
        GameObject rootObject = new GameObject(GeneratedRootName);
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    private GameObject InstantiateTile(Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject prefabTile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, parent);
            return prefabTile;
        }
#endif

        return Instantiate(tilePrefab, parent);
    }

    private void ClearGeneratedGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedRootName)
            {
                DestroyGeneratedObject(child.gameObject);
            }
        }
    }

    private void DestroyGeneratedObject(Object objectToDestroy)
    {
        if (Application.isPlaying)
        {
            Destroy(objectToDestroy);
        }
        else
        {
            DestroyImmediate(objectToDestroy);
        }
    }
}
