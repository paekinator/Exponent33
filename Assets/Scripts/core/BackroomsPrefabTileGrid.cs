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
    public bool rebuildOnEnable = true;

    private const string GeneratedRootName = "Generated_Backrooms_Tile_Grid";
    private const string GeneratedTileName = "Generated_Backrooms_Fill_Tile";

    private void OnEnable()
    {
        if (rebuildOnEnable && !Application.isPlaying)
        {
            RebuildGrid();
        }
    }

    [ContextMenu("Rebuild Grid")]
    public void RebuildGrid()
    {
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
