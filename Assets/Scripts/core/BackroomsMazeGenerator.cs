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
        Door
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
        BuildBackroomsCorridorFill(root, min, max);
        BuildEdgeToEdgeHallwayFill(root, min, max);
        BuildStandalonePillarBiomes(root, min, max);
    }

    private void BuildWestMazeBiome(Transform root, int startX, int startZ)
    {
        AddRoomBox(root, startX, startZ, 8, 7, 2);
        AddRoomBox(root, startX + 9, startZ, 9, 8, 1);
        AddRoomBox(root, startX + 2, startZ + 9, 8, 9, 3);
        AddRoomBox(root, startX + 12, startZ + 10, 7, 8, 0);

        AddWallRun(root, true, startX + 4, startZ + 1, 17, new[] { 4, 11 }, WallKind.Standard);
        AddWallRun(root, true, startX + 8, startZ + 3, 15, new[] { 6, 12 }, WallKind.Standard);
        AddWallRun(root, true, startX + 13, startZ + 1, 18, new[] { 3, 9, 15 }, WallKind.Standard);
        AddWallRun(root, false, startX + 1, startZ + 5, 18, new[] { 2, 8, 14 }, WallKind.Standard);
        AddWallRun(root, false, startX + 3, startZ + 12, 17, new[] { 5, 11 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 17, 20, new[] { 3, 16 }, WallKind.Standard);

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
        AddWallRun(root, false, startX + 1, startZ + 11, 13, new[] { 2, 10 }, WallKind.Standard);
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
        AddWallRun(root, true, startX + 14, startZ, 11, new[] { 5 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 12, 13, new[] { 4, 10 }, WallKind.Standard);
    }

    private void BuildLongCorridorBiome(Transform root, int startX, int startZ)
    {
        AddWallRun(root, false, startX, startZ, 39, new[] { 8, 18, 29 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 5, 39, new[] { 5, 15, 25, 35 }, WallKind.Standard);
        AddWallRun(root, false, startX + 2, startZ + 10, 35, new[] { 12, 24 }, WallKind.Standard);

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

        AddWallRun(root, true, startX + 4, startZ, 16, new[] { 2, 7, 13 }, WallKind.Standard);
        AddWallRun(root, true, startX + 8, startZ, 16, new[] { 4, 10 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 4, 16, new[] { 3, 9, 14 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 8, 16, new[] { 6, 12 }, WallKind.Standard);

        AddDoorWall(root, false, startX + 2, startZ + 4);
        AddDoorWall(root, true, startX + 4, startZ + 6);
        AddDoorWall(root, false, startX + 10, startZ + 8);
        AddDoorWall(root, true, startX + 12, startZ + 10);
    }

    private void BuildBrokenWallBiome(Transform root, int startX, int startZ)
    {
        AddWallRun(root, false, startX, startZ, 27, new[] { 2, 6, 11, 17, 23 }, WallKind.Standard);
        AddWallRun(root, false, startX + 2, startZ + 4, 25, new[] { 4, 8, 15, 21 }, WallKind.Standard);
        AddWallRun(root, false, startX, startZ + 8, 30, new[] { 5, 9, 14, 20, 27 }, WallKind.Standard);

        AddWallRun(root, true, startX + 3, startZ, 8, new[] { 3, 6 }, WallKind.Standard);
        AddWallRun(root, true, startX + 12, startZ + 1, 7, new[] { 2, 5 }, WallKind.Standard);
        AddWallRun(root, true, startX + 21, startZ, 8, new[] { 4 }, WallKind.Standard);

        for (int x = startX + 5; x <= startX + 27; x += 7)
        {
            CreatePillar(root, x, startZ + 2);
            CreatePillar(root, x + 1, startZ + 6);
        }
    }

    private void BuildSparseTransitionPieces(Transform root, int min, int max)
    {
        AddWallRun(root, true, min + 24, min + 4, 9, new[] { 2, 6 }, WallKind.Standard);
        AddWallRun(root, false, min + 19, min + 15, 11, new[] { 4 }, WallKind.Standard);
        AddWallRun(root, true, min + 26, min + 33, 10, new[] { 3, 8 }, WallKind.Standard);
        AddWallRun(root, false, min + 6, max - 6, 14, new[] { 5, 10 }, WallKind.Standard);

        CreatePillar(root, min + 24, min + 4);
        CreatePillar(root, min + 24, min + 13);
        CreatePillar(root, min + 38, min + 22);
        CreatePillar(root, max - 8, max - 9);
        CreatePillar(root, min + 8, max - 5);
    }

    private void BuildBackroomsCorridorFill(Transform root, int min, int max)
    {
        for (int z = min + 6; z <= max - 8; z += 7)
        {
            AddWallRun(root, false, min + 4, z, 12, new[] { 4, 9 }, WallKind.Standard);
            AddWallRun(root, false, min + 20, z + 2, 10, new[] { 5 }, WallKind.Standard);
            AddWallRun(root, false, max - 18, z, 12, new[] { 3, 8 }, WallKind.Standard);
        }

        for (int x = min + 10; x <= max - 10; x += 8)
        {
            AddWallRun(root, true, x, min + 5, 9, new[] { 4 }, WallKind.Standard);
            AddWallRun(root, true, x + 3, min + 20, 10, new[] { 2, 7 }, WallKind.Standard);
            AddWallRun(root, true, x, max - 18, 11, new[] { 5 }, WallKind.Standard);
        }

        AddRoomBox(root, min + 4, min + 18, 7, 6, 1);
        AddRoomBox(root, min + 33, min + 6, 8, 7, 3);
        AddRoomBox(root, min + 36, min + 22, 7, 8, 0);
        AddRoomBox(root, min + 18, max - 12, 9, 7, 2);
    }

    private void BuildStandalonePillarBiomes(Transform root, int min, int max)
    {
        BuildPillarGrid(root, min + 5, min + 22, 7, 9, 2);
        BuildPillarGrid(root, min + 34, min + 18, 9, 7, 2);
        BuildPillarGrid(root, min + 21, min + 6, 5, 13, 2);

        BuildStaggeredPillarField(root, min + 4, min + 34, 16, 9);
        BuildStaggeredPillarField(root, min + 28, min + 39, 12, 6);

        BuildPillarIsland(root, min + 12, min + 27);
        BuildPillarIsland(root, min + 41, min + 13);
        BuildPillarIsland(root, max - 11, max - 7);
    }

    private void BuildEdgeToEdgeHallwayFill(Transform root, int min, int max)
    {
        int length = max - min - 1;

        for (int z = min + 4; z <= max - 4; z += 5)
        {
            int[] gaps = z % 10 == 0 ? new[] { 9, 21, 34 } : new[] { 14, 28 };
            AddWallRun(root, false, min + 1, z, length, gaps, WallKind.Standard);
        }

        for (int x = min + 6; x <= max - 6; x += 7)
        {
            int[] gaps = x % 14 == 0 ? new[] { 8, 19, 31 } : new[] { 12, 25 };
            AddWallRun(root, true, x, min + 1, length, gaps, WallKind.Standard);
        }
    }

    private void BuildPillarGrid(Transform root, int startX, int startZ, int width, int height, int spacing)
    {
        for (int x = startX; x <= startX + width; x += spacing)
        {
            for (int z = startZ; z <= startZ + height; z += spacing)
            {
                CreatePillar(root, x, z);
            }
        }
    }

    private void BuildStaggeredPillarField(Transform root, int startX, int startZ, int width, int height)
    {
        for (int x = startX; x <= startX + width; x += 2)
        {
            int offset = ((x - startX) / 2) % 2;
            for (int z = startZ + offset; z <= startZ + height; z += 3)
            {
                CreatePillar(root, x, z);
            }
        }
    }

    private void BuildPillarIsland(Transform root, int centerX, int centerZ)
    {
        CreatePillar(root, centerX, centerZ);
        CreatePillar(root, centerX - 1, centerZ);
        CreatePillar(root, centerX + 1, centerZ);
        CreatePillar(root, centerX, centerZ - 1);
        CreatePillar(root, centerX, centerZ + 1);
        CreatePillar(root, centerX - 2, centerZ - 2);
        CreatePillar(root, centerX + 2, centerZ - 2);
        CreatePillar(root, centerX - 2, centerZ + 2);
        CreatePillar(root, centerX + 2, centerZ + 2);
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
        int segmentStartX = 0;
        int segmentStartZ = 0;
        int segmentEndX = 0;
        int segmentEndZ = 0;
        bool hasActiveSegment = false;

        for (int i = 0; i < length; i++)
        {
            int x = vertical ? gridX : gridX + i;
            int z = vertical ? gridZ + i : gridZ;

            if (HasGap(gaps, i) || !IsInsideBuildArea(x, z))
            {
                CapWallSegment(root, vertical, hasActiveSegment, segmentStartX, segmentStartZ, segmentEndX, segmentEndZ);
                hasActiveSegment = false;
                continue;
            }

            if (!hasActiveSegment)
            {
                segmentStartX = x;
                segmentStartZ = z;
                hasActiveSegment = true;
            }

            segmentEndX = x;
            segmentEndZ = z;

            CreateWall(root, vertical, x, z, kind);
        }

        CapWallSegment(root, vertical, hasActiveSegment, segmentStartX, segmentStartZ, segmentEndX, segmentEndZ);
    }

    private void AddDoorWall(Transform root, bool vertical, int gridX, int gridZ)
    {
        if (IsInsideBuildArea(gridX, gridZ))
        {
            CreateWall(root, vertical, gridX, gridZ, WallKind.Door);
            CapWallSegment(root, vertical, true, gridX, gridZ, gridX, gridZ);
        }
    }

    private void CapWallSegment(Transform root, bool vertical, bool hasSegment, int startX, int startZ, int endX, int endZ)
    {
        if (!capWallEndsWithPillars || !hasSegment)
        {
            return;
        }

        if (vertical)
        {
            CreatePillar(root, startX, startZ);
            CreatePillar(root, endX, endZ + 1);
        }
        else
        {
            CreatePillar(root, startX, startZ);
            CreatePillar(root, endX + 1, endZ);
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

        GameObject prefab = pillarPrefabs[0];
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
