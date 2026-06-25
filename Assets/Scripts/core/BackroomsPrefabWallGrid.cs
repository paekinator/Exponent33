using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BackroomsPrefabWallGrid : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject sidePrefab;
    public GameObject cornerPrefab;

    [Header("Grid")]
    public float mapSize = 200f;
    public float segmentLength = 4f;
    public bool rebuildOnEnable = false; // set false to preserve the current map

    // MAP LOCKED: the wall grid is frozen as currently authored in the scene.
    // While true, no automatic or manual rebuild/fill runs. Set to false ONLY
    // if you deliberately want to regenerate the wall ring again.
    private static readonly bool MapLocked = true;

    private const string GeneratedRootName = "Generated_Backrooms_Wall_Grid";
    private const string GeneratedPieceName = "Generated_Backrooms_Edge_Tile";

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

    // ── One-time SE corner/edge fill ─────────────────────────────────────────
    // Adjust these in the Inspector then right-click → Fill Missing SE Edge.
    // Tiles are parented under a separate root so Rebuild Grid won't touch them.

    [Header("Missing Edge Fill (run once)")]
    [Tooltip("World X to start placing south edge tiles.")]
    public float fillStartX        = 50f;
    [Tooltip("World X to stop (inclusive, snapped to grid).")]
    public float fillEndX          = 94f;
    [Tooltip("World Z of the south edge.")]
    public float fillEdgeZ         = -98f;
    [Tooltip("Y rotation for south-edge side tiles (0 = facing south).")]
    public float fillSideRotation  = 0f;
    [Tooltip("If true, also places the SE corner piece at (fillEndX + segmentLength, 0, fillEdgeZ).")]
    public bool  fillPlaceSECorner = true;

    private const string FillRootName = "Generated_Backrooms_Wall_Fill";

    [ContextMenu("Fill Missing SE Edge")]
    public void FillMissingSEEdge()
    {
        if (MapLocked)
        {
            Debug.LogWarning("BackroomsPrefabWallGrid: fill is disabled (MapLocked = true). The map is frozen. Set MapLocked = false in BackroomsPrefabWallGrid.cs to change walls.", this);
            return;
        }

        if (sidePrefab == null || cornerPrefab == null)
        {
            Debug.LogWarning("BackroomsPrefabWallGrid: assign sidePrefab and cornerPrefab first.", this);
            return;
        }

        float step = segmentLength > 0f ? segmentLength : 4f;

        // Find or create a fill root that is immune to ClearGeneratedGrid()
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

        int placed = 0;

        // Side tiles along the south edge
        float x = fillStartX;
        while (x <= fillEndX + 0.01f)
        {
            string tag = "SouthFill_" + x.ToString("F0");
            GameObject tile = InstantiatePrefab(sidePrefab, fillRoot);
            tile.name       = "Fill_South_" + tag;
            tile.transform.localPosition = new Vector3(x, 0f, fillEdgeZ);
            tile.transform.localRotation = Quaternion.Euler(0f, fillSideRotation, 0f);
            tile.transform.localScale    = Vector3.one;
            placed++;
            x += step;
        }

        // SE corner piece
        if (fillPlaceSECorner)
        {
            float cornerX = fillEndX + step;  // 98 when fillEndX=94 and step=4
            GameObject corner = InstantiatePrefab(cornerPrefab, fillRoot);
            corner.name       = "Fill_Corner_SE";
            corner.transform.localPosition = new Vector3(cornerX, 0f, fillEdgeZ);
            corner.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            corner.transform.localScale    = Vector3.one;
            placed++;
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            gameObject.scene);
#endif

        Debug.Log($"[BackroomsPrefabWallGrid] FillMissingSEEdge placed {placed} pieces.");
    }

    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
        if (MapLocked)
        {
            Debug.LogWarning("BackroomsPrefabWallGrid: rebuild is disabled (MapLocked = true). The map is frozen. Set MapLocked = false in BackroomsPrefabWallGrid.cs to regenerate.", this);
            return;
        }

        if (sidePrefab == null || cornerPrefab == null || mapSize <= 0f)
        {
            return;
        }

        if (segmentLength <= 0f)
        {
            segmentLength = 4f;
        }

        ClearGeneratedGrid();

        Transform root = CreateGeneratedRoot();
        int cellsPerSide = Mathf.Max(2, Mathf.RoundToInt(mapSize / segmentLength));
        float firstCenter = -((cellsPerSide - 1) * segmentLength) * 0.5f;
        float lastCenter = -firstCenter;

        CreatePiece(root, cornerPrefab, "Corner_NorthWest", new Vector3(firstCenter, 0f, lastCenter), 180f);
        CreatePiece(root, cornerPrefab, "Corner_NorthEast", new Vector3(lastCenter, 0f, lastCenter), 270f);
        CreatePiece(root, cornerPrefab, "Corner_SouthEast", new Vector3(lastCenter, 0f, firstCenter), 0f);
        CreatePiece(root, cornerPrefab, "Corner_SouthWest", new Vector3(firstCenter, 0f, firstCenter), 90f);

        for (int i = 1; i < cellsPerSide - 1; i++)
        {
            float position = firstCenter + (i * segmentLength);
            CreatePiece(root, sidePrefab, "North_" + i, new Vector3(position, 0f, lastCenter), 180f);
            CreatePiece(root, sidePrefab, "East_" + i, new Vector3(lastCenter, 0f, position), 270f);
            CreatePiece(root, sidePrefab, "South_" + i, new Vector3(position, 0f, firstCenter), 0f);
            CreatePiece(root, sidePrefab, "West_" + i, new Vector3(firstCenter, 0f, position), 90f);
        }
    }

    private void CreatePiece(Transform root, GameObject prefab, string suffix, Vector3 localPosition, float yRotation)
    {
        GameObject piece = InstantiatePrefab(prefab, root);
        piece.name = GeneratedPieceName + "_" + suffix;
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        piece.transform.localScale = Vector3.one;
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

    private GameObject InstantiatePrefab(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
#endif

        return Instantiate(prefab, parent);
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
