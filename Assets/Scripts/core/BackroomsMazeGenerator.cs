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
    public int seed = 3317;
    public bool rebuildOnEnable = false;
    [Range(0f, 1f)] public float doorChance = 0.12f;
    [Range(0f, 1f)] public float detailWallChance = 0.08f;
    [Range(0f, 1f)] public float shortSegmentChance = 0.18f;
    [Range(0f, 1f)] public float pillarSkipChance = 0.28f;
    public bool capWallEndsWithPillars = true;

    private const string GeneratedRootName = "Generated_Backrooms_Maze";
    private const string GeneratedPieceName = "Generated_Backrooms_MazePiece";

    private readonly HashSet<string> occupiedWalls = new HashSet<string>();
    private readonly HashSet<string> occupiedPillars = new HashSet<string>();
    private System.Random random;

    private void OnEnable()
    {
        if (rebuildOnEnable && !Application.isPlaying)
        {
            RebuildMaze();
        }
    }

    [ContextMenu("Rebuild Maze")]
    public void RebuildMaze()
    {
        if (wallPrefab == null || mapSize <= 0f || tileSize <= 0f)
        {
            return;
        }

        ClearGeneratedMaze();
        occupiedWalls.Clear();
        occupiedPillars.Clear();
        random = new System.Random(seed);

        Transform root = CreateGeneratedRoot();
        int cellCount = Mathf.RoundToInt(mapSize / tileSize);
        int min = borderTilePadding;
        int max = cellCount - borderTilePadding;

        BuildStructuredMaze(root, min + 2, min + 2, 21, 24);
        BuildPillarField(root, min + 28, min + 7, 14, 16);
        BuildDensePillarSection(root, min + 7, min + 8, 13, 12);
        BuildDensePillarSection(root, min + 34, min + 35, 9, 9);
        BuildBrokenOfficeRuns(root, min + 7, min + 31, 30, 10);
        BuildDoorCluster(root, min + 30, min + 28, 13, 14);
        BuildPillarMazeSection(root, min + 17, min + 18, 12, 11);
        BuildLooseStrays(root, min, max, 75);
    }

    private void BuildStructuredMaze(Transform root, int startX, int startZ, int width, int height)
    {
        for (int x = startX; x <= startX + width; x += 3)
        {
            AddWallRun(root, true, x, startZ, height, RandomRange(2, 5));
        }

        for (int z = startZ + 2; z <= startZ + height; z += 4)
        {
            AddWallRun(root, false, startX, z, width, RandomRange(2, 5));
        }

        for (int i = 0; i < 26; i++)
        {
            bool vertical = random.NextDouble() > 0.45;
            int x = RandomRange(startX, startX + width);
            int z = RandomRange(startZ, startZ + height);
            int length = RandomRange(2, 8);
            AddWallRun(root, vertical, x, z, length, RandomRange(2, 5));
        }
    }

    private void BuildPillarField(Transform root, int startX, int startZ, int width, int height)
    {
        for (int x = startX; x <= startX + width; x += 2)
        {
            for (int z = startZ; z <= startZ + height; z += 2)
            {
                if (random.NextDouble() < pillarSkipChance)
                {
                    continue;
                }

                CreatePillar(root, x, z);
            }
        }

        AddWallRun(root, false, startX - 1, startZ - 1, width + 2, 4);
        AddWallRun(root, true, startX - 1, startZ - 1, height + 2, 4);
        AddWallRun(root, false, startX - 1, startZ + height + 1, width + 2, 4);
    }

    private void BuildDensePillarSection(Transform root, int startX, int startZ, int width, int height)
    {
        for (int x = startX; x <= startX + width; x += 2)
        {
            for (int z = startZ; z <= startZ + height; z += 2)
            {
                if (random.NextDouble() < 0.12f)
                {
                    continue;
                }

                CreatePillar(root, x, z);
            }
        }

        AddWallRun(root, false, startX - 1, startZ - 1, width + 2, 0);
        AddWallRun(root, true, startX - 1, startZ - 1, height + 2, 0);
        AddWallRun(root, false, startX - 1, startZ + height + 1, width + 2, 3);
        AddWallRun(root, true, startX + width + 1, startZ - 1, height + 2, 3);
    }

    private void BuildPillarMazeSection(Transform root, int startX, int startZ, int width, int height)
    {
        for (int x = startX; x <= startX + width; x += 2)
        {
            AddWallRun(root, true, x, startZ, height, RandomRange(3, 5));
        }

        for (int z = startZ; z <= startZ + height; z += 2)
        {
            AddWallRun(root, false, startX, z, width, RandomRange(3, 5));
        }

        for (int i = 0; i < 36; i++)
        {
            int x = RandomRange(startX, startX + width + 1);
            int z = RandomRange(startZ, startZ + height + 1);

            if ((x + z) % 2 == 0)
            {
                CreatePillar(root, x, z);
            }
        }
    }

    private void BuildBrokenOfficeRuns(Transform root, int startX, int startZ, int width, int height)
    {
        for (int z = startZ; z <= startZ + height; z += 3)
        {
            int cursor = startX;
            while (cursor < startX + width)
            {
                int length = RandomRange(2, 7);
                AddWallRun(root, false, cursor, z, length, RandomRange(2, 4));
                cursor += length + RandomRange(2, 5);
            }
        }

        for (int x = startX + 4; x <= startX + width; x += 6)
        {
            AddWallRun(root, true, x, startZ, height, RandomRange(3, 5));
        }
    }

    private void BuildDoorCluster(Transform root, int startX, int startZ, int width, int height)
    {
        for (int i = 0; i < 5; i++)
        {
            int z = startZ + (i * 3);
            AddWallRun(root, false, startX, z, width, 3);
        }

        for (int i = 0; i < 4; i++)
        {
            int x = startX + (i * 4);
            AddWallRun(root, true, x, startZ, height, 3);
        }
    }

    private void BuildLooseStrays(Transform root, int min, int max, int count)
    {
        for (int i = 0; i < count; i++)
        {
            bool vertical = random.NextDouble() > 0.5;
            int x = RandomRange(min + 3, max - 3);
            int z = RandomRange(min + 3, max - 3);
            int length = random.NextDouble() < shortSegmentChance ? RandomRange(1, 3) : RandomRange(3, 8);
            AddWallRun(root, vertical, x, z, length, RandomRange(2, 5));
        }
    }

    private void AddWallRun(Transform root, bool vertical, int gridX, int gridZ, int length, int gapEvery)
    {
        int doorwayIndex = RandomRange(0, Mathf.Max(1, length));

        for (int i = 0; i < length; i++)
        {
            if (gapEvery > 0 && i > 0 && (i % gapEvery) == 0)
            {
                continue;
            }

            int x = vertical ? gridX : gridX + i;
            int z = vertical ? gridZ + i : gridZ;

            if (x <= borderTilePadding || z <= borderTilePadding)
            {
                continue;
            }

            if (x >= Mathf.RoundToInt(mapSize / tileSize) - borderTilePadding || z >= Mathf.RoundToInt(mapSize / tileSize) - borderTilePadding)
            {
                continue;
            }

            bool forceDoor = i == doorwayIndex && length >= 4 && random.NextDouble() < 0.55;
            CreateWall(root, vertical, x, z, forceDoor);
        }
    }

    private void CreateWall(Transform root, bool vertical, int gridX, int gridZ, bool forceDoor)
    {
        string key = (vertical ? "V" : "H") + "_" + gridX + "_" + gridZ;
        if (!occupiedWalls.Add(key))
        {
            return;
        }

        GameObject prefab = PickWallPrefab(forceDoor);
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
        if (pillarPrefabs == null || pillarPrefabs.Length == 0)
        {
            return;
        }

        string key = gridX + "_" + gridZ;
        if (!occupiedPillars.Add(key))
        {
            return;
        }

        GameObject prefab = pillarPrefabs[RandomRange(0, pillarPrefabs.Length)];
        Vector3 position = new Vector3(GridLine(gridX), 0f, GridLine(gridZ));
        CreatePiece(root, prefab, "Pillar_" + key, position, RandomRange(0, 4) * 90f);
    }

    private GameObject PickWallPrefab(bool forceDoor)
    {
        if ((forceDoor || random.NextDouble() < doorChance) && doorWallPrefabs != null && doorWallPrefabs.Length > 0)
        {
            return doorWallPrefabs[RandomRange(0, doorWallPrefabs.Length)];
        }

        if (random.NextDouble() < detailWallChance && detailWallPrefabs != null && detailWallPrefabs.Length > 0)
        {
            return detailWallPrefabs[RandomRange(0, detailWallPrefabs.Length)];
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

    private float GridLine(int index)
    {
        return (-mapSize * 0.5f) + (index * tileSize);
    }

    private float CellCenter(int index)
    {
        return GridLine(index) + (tileSize * 0.5f);
    }

    private int RandomRange(int minInclusive, int maxExclusive)
    {
        return random.Next(minInclusive, Mathf.Max(minInclusive + 1, maxExclusive));
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
