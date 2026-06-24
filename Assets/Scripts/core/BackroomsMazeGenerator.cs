using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BackroomsMazeGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject[] doorWallPrefabs;
    public GameObject[] detailWallPrefabs;
    public GameObject[] pillarPrefabs;

    [Header("Grid")]
    public float mapSize = 200f;
    public float tileSize = 4f;
    public int borderTilePadding = 3;

    [Header("Generation")]
    public bool rebuildOnEnable = false;
    public bool capWallEndsWithPillars = true;

    private const string GeneratedRootName = "Generated_Backrooms_Maze";
    private const string GeneratedPieceName = "Generated_Backrooms_MazePiece";

    private readonly HashSet<string> occupiedWalls = new HashSet<string>();
    private readonly HashSet<string> occupiedPillars = new HashSet<string>();

    private enum WallKind
    {
        Standard,
        Door,
        Detail
    }

    private void OnEnable()
    {
        if (rebuildOnEnable && !Application.isPlaying)
        {
            RebuildMaze();
        }
    }

    [ContextMenu("Rebuild Fixed Level")]
    public void RebuildMaze()
    {
        if (wallPrefab == null || mapSize <= 0f || tileSize <= 0f)
        {
            return;
        }

        ClearGeneratedMaze();
        occupiedWalls.Clear();
        occupiedPillars.Clear();

        Transform root = CreateGeneratedRoot();
        int cellCount = Mathf.RoundToInt(mapSize / tileSize);
        int min = borderTilePadding;
        int max = cellCount - borderTilePadding;

        BuildWestMazeBiome(root, min + 2, min + 2);
        BuildSouthPillarBiome(root, min + 28, min + 4);
        BuildCentralPillarForest(root, min + 15, min + 18);
        BuildLongCorridorBiome(root, min + 4, min + 30);
        BuildDoorRoomBiome(root, min + 30, min + 27);
        BuildBrokenWallBiome(root, min + 8, min + 39);
        BuildSparseTransitionPieces(root, min, max);
    }

    private void BuildWestMazeBiome(Transform root, int startX, int startZ)
    {
        AddRoomBox(root, startX, startZ, 8, 7, 2);
        AddRoomBox(root, startX + 9, startZ, 9, 8, 1);
        AddRoomBox(root, startX + 2, startZ + 9, 8, 9, 3);
        AddRoomBox(root, startX + 12, startZ + 10, 7, 8, 0);

        AddWallRun(root, true, startX + 4, startZ + 1, 17, new[] { 4, 11 }, WallKind.Standard);
        AddWallRun(root, true, startX + 8, startZ + 3, 15, new[] { 6, 12 }, WallKind.Standard);
        AddWallRun(root, true, startX + 13, startZ + 1, 18, new[] { 3, 9, 15 }, WallKind.Detail);
        AddWallRun(root, false, startX + 1, startZ + 5, 18, new[] { 2, 8, 14 }, WallKind.Standard);
        AddWallRun(root, false, startX + 3, startZ + 12, 17, new[] { 5, 11 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 17, 20, new[] { 3, 16 }, WallKind.Detail);

        AddDoorWall(root, false, startX + 8, startZ + 7);
        AddDoorWall(root, true, startX + 11, startZ + 12);
        AddDoorWall(root, false, startX + 14, startZ + 9);
    }

    private void BuildSouthPillarBiome(Transform root, int startX, int startZ)
    {
        AddRoomBox(root, startX - 1, startZ - 1, 16, 18, 4);

        for (int x = startX; x <= startX + 14; x += 2)
        {
            for (int z = startZ; z <= startZ + 16; z += 2)
            {
                if ((x + (z * 2)) % 10 == 0)
                {
                    continue;
                }

                CreatePillar(root, x, z);
            }
        }

        AddWallRun(root, false, startX + 1, startZ + 5, 13, new[] { 4, 9 }, WallKind.Standard);
        AddWallRun(root, false, startX + 1, startZ + 11, 13, new[] { 2, 10 }, WallKind.Detail);
        AddWallRun(root, true, startX + 5, startZ + 2, 14, new[] { 5, 8 }, WallKind.Standard);
        AddWallRun(root, true, startX + 11, startZ + 1, 15, new[] { 7, 12 }, WallKind.Standard);
    }

    private void BuildCentralPillarForest(Transform root, int startX, int startZ)
    {
        for (int x = startX; x <= startX + 13; x += 2)
        {
            for (int z = startZ; z <= startZ + 11; z += 2)
            {
                if ((x + z) % 6 == 0)
                {
                    continue;
                }

                CreatePillar(root, x, z);
            }
        }

        AddWallRun(root, true, startX - 1, startZ - 1, 13, new[] { 3, 9 }, WallKind.Standard);
        AddWallRun(root, false, startX - 1, startZ - 1, 15, new[] { 6, 12 }, WallKind.Standard);
        AddWallRun(root, true, startX + 14, startZ, 11, new[] { 5 }, WallKind.Detail);
        AddWallRun(root, false, startX, startZ + 12, 13, new[] { 4, 10 }, WallKind.Standard);
    }

    private void BuildLongCorridorBiome(Transform root, int startX, int startZ)
    {
        AddWallRun(root, false, startX, startZ, 39, new[] { 8, 18, 29 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 5, 39, new[] { 5, 15, 25, 35 }, WallKind.Standard);
        AddWallRun(root, false, startX + 2, startZ + 10, 35, new[] { 12, 24 }, WallKind.Detail);

        for (int x = startX + 4; x <= startX + 34; x += 6)
        {
            AddWallRun(root, true, x, startZ + 1, 4, new[] { 2 }, WallKind.Standard);
            AddDoorWall(root, true, x + 2, startZ + 5);
        }

        AddDoorWall(root, false, startX + 10, startZ);
        AddDoorWall(root, false, startX + 21, startZ + 5);
        AddDoorWall(root, false, startX + 31, startZ + 10);
    }

    private void BuildDoorRoomBiome(Transform root, int startX, int startZ)
    {
        for (int x = startX; x <= startX + 12; x += 4)
        {
            for (int z = startZ; z <= startZ + 12; z += 4)
            {
                AddRoomBox(root, x, z, 4, 4, (x + z) % 4);
            }
        }

        AddWallRun(root, true, startX + 4, startZ, 16, new[] { 2, 7, 13 }, WallKind.Detail);
        AddWallRun(root, true, startX + 8, startZ, 16, new[] { 4, 10 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 4, 16, new[] { 3, 9, 14 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 8, 16, new[] { 6, 12 }, WallKind.Detail);

        AddDoorWall(root, false, startX + 2, startZ + 4);
        AddDoorWall(root, true, startX + 4, startZ + 6);
        AddDoorWall(root, false, startX + 10, startZ + 8);
        AddDoorWall(root, true, startX + 12, startZ + 10);
    }

    private void BuildBrokenWallBiome(Transform root, int startX, int startZ)
    {
        AddWallRun(root, false, startX, startZ, 27, new[] { 2, 6, 11, 17, 23 }, WallKind.Standard);
        AddWallRun(root, false, startX + 2, startZ + 4, 25, new[] { 4, 8, 15, 21 }, WallKind.Detail);
        AddWallRun(root, false, startX, startZ + 8, 30, new[] { 5, 9, 14, 20, 27 }, WallKind.Standard);

        AddWallRun(root, true, startX + 3, startZ, 8, new[] { 3, 6 }, WallKind.Standard);
        AddWallRun(root, true, startX + 12, startZ + 1, 7, new[] { 2, 5 }, WallKind.Standard);
        AddWallRun(root, true, startX + 21, startZ, 8, new[] { 4 }, WallKind.Detail);

        for (int x = startX + 5; x <= startX + 27; x += 7)
        {
            CreatePillar(root, x, startZ + 2);
            CreatePillar(root, x + 1, startZ + 6);
        }
    }

    private void BuildSparseTransitionPieces(Transform root, int min, int max)
    {
        AddWallRun(root, true, min + 24, min + 4, 9, new[] { 2, 6 }, WallKind.Standard);
        AddWallRun(root, false, min + 19, min + 15, 11, new[] { 4 }, WallKind.Detail);
        AddWallRun(root, true, min + 26, min + 33, 10, new[] { 3, 8 }, WallKind.Standard);
        AddWallRun(root, false, min + 6, max - 6, 14, new[] { 5, 10 }, WallKind.Standard);

        CreatePillar(root, min + 24, min + 4);
        CreatePillar(root, min + 24, min + 13);
        CreatePillar(root, min + 38, min + 22);
        CreatePillar(root, max - 8, max - 9);
        CreatePillar(root, min + 8, max - 5);
    }

    private void AddRoomBox(Transform root, int startX, int startZ, int width, int height, int doorwaySide)
    {
        int doorX = Mathf.Max(1, width / 2);
        int doorZ = Mathf.Max(1, height / 2);

        AddWallRun(root, false, startX, startZ, width, doorwaySide == 0 ? new[] { doorX } : null, WallKind.Standard);
        AddWallRun(root, true, startX + width, startZ, height, doorwaySide == 1 ? new[] { doorZ } : null, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + height, width, doorwaySide == 2 ? new[] { doorX } : null, WallKind.Standard);
        AddWallRun(root, true, startX, startZ, height, doorwaySide == 3 ? new[] { doorZ } : null, WallKind.Standard);

        if (doorwaySide == 0)
        {
            AddDoorWall(root, false, startX + doorX, startZ);
        }
        else if (doorwaySide == 1)
        {
            AddDoorWall(root, true, startX + width, startZ + doorZ);
        }
        else if (doorwaySide == 2)
        {
            AddDoorWall(root, false, startX + doorX, startZ + height);
        }
        else if (doorwaySide == 3)
        {
            AddDoorWall(root, true, startX, startZ + doorZ);
        }
    }

    private void AddWallRun(Transform root, bool vertical, int gridX, int gridZ, int length, int[] gaps, WallKind kind)
    {
        for (int i = 0; i < length; i++)
        {
            if (HasGap(gaps, i))
            {
                continue;
            }

            int x = vertical ? gridX : gridX + i;
            int z = vertical ? gridZ + i : gridZ;

            if (!IsInsideBuildArea(x, z))
            {
                continue;
            }

            WallKind resolvedKind = kind;
            if (kind == WallKind.Standard && ((x * 7) + (z * 3)) % 17 == 0)
            {
                resolvedKind = WallKind.Detail;
            }

            CreateWall(root, vertical, x, z, resolvedKind);
        }
    }

    private void AddDoorWall(Transform root, bool vertical, int gridX, int gridZ)
    {
        if (IsInsideBuildArea(gridX, gridZ))
        {
            CreateWall(root, vertical, gridX, gridZ, WallKind.Door);
        }
    }

    private void CreateWall(Transform root, bool vertical, int gridX, int gridZ, WallKind kind)
    {
        string key = (vertical ? "V" : "H") + "_" + gridX + "_" + gridZ;
        if (!occupiedWalls.Add(key))
        {
            return;
        }

        GameObject prefab = PickWallPrefab(kind, gridX, gridZ);
        Vector3 position = vertical
            ? new Vector3(GridLine(gridX), 0f, CellCenter(gridZ))
            : new Vector3(CellCenter(gridX), 0f, GridLine(gridZ));
        float rotation = vertical ? 90f : 0f;
        CreatePiece(root, prefab, "Wall_" + key, position, rotation);

        if (capWallEndsWithPillars)
        {
            if (vertical)
            {
                CreatePillar(root, gridX, gridZ);
                CreatePillar(root, gridX, gridZ + 1);
            }
            else
            {
                CreatePillar(root, gridX, gridZ);
                CreatePillar(root, gridX + 1, gridZ);
            }
        }
    }

    private void CreatePillar(Transform root, int gridX, int gridZ)
    {
        if (pillarPrefabs == null || pillarPrefabs.Length == 0 || !IsInsideBuildArea(gridX, gridZ))
        {
            return;
        }

        string key = gridX + "_" + gridZ;
        if (!occupiedPillars.Add(key))
        {
            return;
        }

        GameObject prefab = pillarPrefabs[PositiveModulo(gridX + gridZ, pillarPrefabs.Length)];
        Vector3 position = new Vector3(GridLine(gridX), 0f, GridLine(gridZ));
        float rotation = PositiveModulo((gridX * 2) + gridZ, 4) * 90f;
        CreatePiece(root, prefab, "Pillar_" + key, position, rotation);
    }

    private GameObject PickWallPrefab(WallKind kind, int gridX, int gridZ)
    {
        if (kind == WallKind.Door && doorWallPrefabs != null && doorWallPrefabs.Length > 0)
        {
            return doorWallPrefabs[PositiveModulo((gridX * 3) + gridZ, doorWallPrefabs.Length)];
        }

        if (kind == WallKind.Detail && detailWallPrefabs != null && detailWallPrefabs.Length > 0)
        {
            return detailWallPrefabs[PositiveModulo(gridX + (gridZ * 5), detailWallPrefabs.Length)];
        }

        return wallPrefab;
    }

    private void CreatePiece(Transform root, GameObject prefab, string suffix, Vector3 localPosition, float yRotation)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject piece = InstantiatePrefab(prefab, root);
        piece.name = GeneratedPieceName + "_" + suffix;
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        piece.transform.localScale = Vector3.one;
    }

    private bool IsInsideBuildArea(int gridX, int gridZ)
    {
        int edge = Mathf.RoundToInt(mapSize / tileSize) - borderTilePadding;
        return gridX > borderTilePadding && gridZ > borderTilePadding && gridX < edge && gridZ < edge;
    }

    private static bool HasGap(int[] gaps, int index)
    {
        if (gaps == null)
        {
            return false;
        }

        for (int i = 0; i < gaps.Length; i++)
        {
            if (gaps[i] == index)
            {
                return true;
            }
        }

        return false;
    }

    private static int PositiveModulo(int value, int length)
    {
        return ((value % length) + length) % length;
    }

    private float GridLine(int index)
    {
        return (-mapSize * 0.5f) + (index * tileSize);
    }

    private float CellCenter(int index)
    {
        return GridLine(index) + (tileSize * 0.5f);
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

    private void ClearGeneratedMaze()
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
