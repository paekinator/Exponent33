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
    public bool rebuildOnEnable = true;

    private const string GeneratedRootName = "Generated_Backrooms_Wall_Grid";
    private const string GeneratedPieceName = "Generated_Backrooms_Edge_Tile";

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
