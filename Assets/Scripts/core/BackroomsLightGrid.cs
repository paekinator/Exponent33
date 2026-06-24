using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BackroomsLightGrid : MonoBehaviour
{
    public float mapSize = 200f;
    public float spacing = 20f;
    public float height = 3.9f;
    public float panelHeight = 3.9f;
    public Vector2 panelSize = new Vector2(5f, 1.8f);
    public float ceilingTileHeight = 4f;
    public float gridLineWidth = 0.15f;
    public float intensity = 1.8f;
    public float range = 13f;
    public float spotAngle = 110f;
    public Vector2 intensityVariation = new Vector2(1f, 1f);
    public Color lightColor = new Color(0.82f, 0.88f, 0.62f, 1f); // dim yellow-green fluorescent
    public GameObject ceilingReferencePrefab;
    public bool autoPlaceAtCeiling = true;
    public float ceilingDrop = 0.08f;
    public bool useFourLightsPerTile = true;
    public float tileLightSpacing = 4f;
    public float lightOffsetFromTileCenter = 1f;
    public float randomOffset = 0f;
    public float brokenTileChance = 0f;
    public float brokenLightChance = 0f;
    public float brokenIntensityMultiplier = 0.08f;
    public bool generateVisibleLightFixtures = false;
    public Vector3 visibleLightSize = new Vector3(1.05f, 0.04f, 1.05f);
    public bool createPointLightForEveryFixture = false;
    public int actualLightEveryNthTile = 1;
    public Texture lightCookie;
    public bool generateCeilingTiles = false;
    public bool generateGridLines = false;
    public bool generateLightPanels = false;
    public Material ceilingTileMaterial;
    public Material gridLineMaterial;
    public Material lightPanelMaterial;
    public bool rebuildOnEnable = false;

    // MAP LOCKED: the ceiling-light grid is frozen as currently authored in the
    // scene. While true, no automatic or manual light rebuild runs, so existing
    // lights are never cleared or regenerated. Set to false ONLY if you
    // deliberately want to regenerate the light grid again.
    private const bool MapLocked = true;

    private const string LightNamePrefix = "Generated_Ceiling_Light_";
    private const string LightFixtureNamePrefix = "Generated_Ceiling_LightFixture_";
    private const string OldOmniFillLightNamePrefix = "Generated_Omni_Fill_Light_";
    private const string PanelNamePrefix = "Generated_Ceiling_LightPanel_";
    private const string TileNamePrefix = "Generated_Ceiling_Tile_";
    private const string GridLineNamePrefix = "Generated_Ceiling_GridLine_";

    void OnEnable()
    {
        if (MapLocked)
        {
            return;
        }

        if (rebuildOnEnable)
        {
            RebuildLights();
        }
    }

    [ContextMenu("Rebuild Lights")]
    public void RebuildLights()
    {
        if (MapLocked)
        {
            Debug.LogWarning("BackroomsLightGrid: rebuild is disabled (MapLocked = true). The lighting is frozen. Set MapLocked = false in BackroomsLightGrid.cs to regenerate.", this);
            return;
        }

        if (spacing <= 0f)
        {
            return;
        }

        ClearGeneratedLights();

        float lightSpacing = useFourLightsPerTile ? tileLightSpacing : spacing;
        if (lightSpacing <= 0f)
        {
            return;
        }

        int countPerAxis = Mathf.Max(1, Mathf.FloorToInt(mapSize / lightSpacing));
        float first = -((countPerAxis - 1) * lightSpacing) * 0.5f;
        float fixtureHeight = GetFixtureHeight();
        float pointLightHeight = fixtureHeight - 0.05f;

        if (generateCeilingTiles)
        {
            for (int x = 0; x < countPerAxis; x++)
            {
                for (int z = 0; z < countPerAxis; z++)
                {
                    GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tileObject.name = TileNamePrefix + x + "_" + z;
                    tileObject.transform.SetParent(transform, false);
                    tileObject.transform.localPosition = new Vector3(first + x * lightSpacing, fixtureHeight, first + z * lightSpacing);
                    tileObject.transform.localScale = new Vector3(lightSpacing - gridLineWidth, 0.04f, lightSpacing - gridLineWidth);

                    Collider tileCollider = tileObject.GetComponent<Collider>();
                    if (tileCollider != null)
                    {
                        DestroyGeneratedObject(tileCollider);
                    }

                    Renderer tileRenderer = tileObject.GetComponent<Renderer>();
                    if (tileRenderer != null && ceilingTileMaterial != null)
                    {
                        tileRenderer.sharedMaterial = ceilingTileMaterial;
                    }
                }
            }
        }

        if (generateGridLines)
        {
            float gridStart = -mapSize * 0.5f;
            for (int i = 0; i <= countPerAxis; i++)
            {
                float position = gridStart + i * lightSpacing;
                CreateGridLine(GridLineNamePrefix + "X_" + i, new Vector3(0f, fixtureHeight - 0.03f, position), new Vector3(mapSize, 0.04f, gridLineWidth));
                CreateGridLine(GridLineNamePrefix + "Z_" + i, new Vector3(position, fixtureHeight - 0.03f, 0f), new Vector3(gridLineWidth, 0.04f, mapSize));
            }
        }

        for (int x = 0; x < countPerAxis; x++)
        {
            for (int z = 0; z < countPerAxis; z++)
            {
                Vector3 tileCenter = new Vector3(first + x * lightSpacing, fixtureHeight, first + z * lightSpacing);
                if (useFourLightsPerTile)
                {
                    CreateTileLights(x, z, tileCenter, pointLightHeight, ShouldCreateActualLight(x, z));
                }
                else
                {
                    CreateLight(LightNamePrefix + x + "_" + z, tileCenter, intensity * GetIntensityMultiplier(x, z));
                }

                if (!generateLightPanels)
                {
                    continue;
                }

                GameObject panelObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panelObject.name = PanelNamePrefix + x + "_" + z;
                panelObject.transform.SetParent(transform, false);
                panelObject.transform.localPosition = new Vector3(first + x * lightSpacing, fixtureHeight, first + z * lightSpacing);
                panelObject.transform.localScale = new Vector3(panelSize.x, 0.04f, panelSize.y);

                Collider panelCollider = panelObject.GetComponent<Collider>();
                if (panelCollider != null)
                {
                    DestroyGeneratedObject(panelCollider);
                }

                Renderer panelRenderer = panelObject.GetComponent<Renderer>();
                if (panelRenderer != null && lightPanelMaterial != null)
                {
                    panelRenderer.sharedMaterial = lightPanelMaterial;
                }
            }
        }

    }

    private float GetIntensityMultiplier(int x, int z)
    {
        float min = Mathf.Min(intensityVariation.x, intensityVariation.y);
        float max = Mathf.Max(intensityVariation.x, intensityVariation.y);
        float noise = Mathf.Repeat(Mathf.Sin((x * 12.9898f) + (z * 78.233f)) * 43758.5453f, 1f);
        return Mathf.Lerp(min, max, noise);
    }

    private float GetFixtureHeight()
    {
        if (!autoPlaceAtCeiling)
        {
            return height;
        }

        if (ceilingReferencePrefab != null && TryGetPrefabBounds(ceilingReferencePrefab, out Bounds bounds))
        {
            return bounds.max.y - ceilingDrop;
        }

        return ceilingTileHeight - ceilingDrop;
    }

    private bool TryGetPrefabBounds(GameObject prefab, out Bounds bounds)
    {
        GameObject sample = InstantiatePrefabSample(prefab);
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

    private GameObject InstantiatePrefabSample(GameObject prefab)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
#endif

        return Instantiate(prefab);
    }

    private bool ShouldCreateActualLight(int x, int z)
    {
        int every = Mathf.Max(1, actualLightEveryNthTile);
        return (x % every) == 0 && (z % every) == 0;
    }

    private void CreateTileLights(int x, int z, Vector3 tileCenter, float pointLightHeight, bool shouldCreateActualLight)
    {
        if (Get01(x, z, 99) < brokenTileChance)
        {
            return;
        }

        Vector2[] offsets =
        {
            new Vector2(-lightOffsetFromTileCenter, -lightOffsetFromTileCenter),
            new Vector2(-lightOffsetFromTileCenter, lightOffsetFromTileCenter),
            new Vector2(lightOffsetFromTileCenter, -lightOffsetFromTileCenter),
            new Vector2(lightOffsetFromTileCenter, lightOffsetFromTileCenter)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            float offsetX = offsets[i].x + Mathf.Lerp(-randomOffset, randomOffset, Get01(x, z, i * 2));
            float offsetZ = offsets[i].y + Mathf.Lerp(-randomOffset, randomOffset, Get01(x, z, (i * 2) + 1));
            float lightIntensity = intensity * GetIntensityMultiplier(x + i, z - i);

            if (Get01(x, z, 20 + i) < brokenLightChance)
            {
                lightIntensity *= brokenIntensityMultiplier;
            }

            Vector3 lightPosition = tileCenter + new Vector3(offsetX, 0f, offsetZ);
            CreateFixture(LightFixtureNamePrefix + x + "_" + z + "_" + i, lightPosition);

            if (createPointLightForEveryFixture)
            {
                CreatePointLight(LightNamePrefix + x + "_" + z + "_" + i, lightPosition, pointLightHeight, lightIntensity);
            }
        }

        if (!createPointLightForEveryFixture && shouldCreateActualLight)
        {
            CreatePointLight(LightNamePrefix + x + "_" + z, tileCenter, pointLightHeight, intensity);
        }
    }

    private void CreateLight(string objectName, Vector3 localPosition, float lightIntensity)
    {
        CreateFixture(LightFixtureNamePrefix + objectName.Replace(LightNamePrefix, string.Empty), localPosition);
        CreatePointLight(objectName, localPosition, localPosition.y - 0.05f, lightIntensity);
    }

    private void CreateFixture(string fixtureName, Vector3 localPosition)
    {
        if (!generateVisibleLightFixtures)
        {
            return;
        }

        GameObject fixtureObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fixtureObject.name = fixtureName;
        fixtureObject.transform.SetParent(transform, false);
        fixtureObject.transform.localPosition = localPosition;
        fixtureObject.transform.localScale = visibleLightSize;

        Collider fixtureCollider = fixtureObject.GetComponent<Collider>();
        if (fixtureCollider != null)
        {
            DestroyGeneratedObject(fixtureCollider);
        }

        Renderer fixtureRenderer = fixtureObject.GetComponent<Renderer>();
        if (fixtureRenderer != null && lightPanelMaterial != null)
        {
            fixtureRenderer.sharedMaterial = lightPanelMaterial;
        }
    }

    private void CreatePointLight(string objectName, Vector3 localPosition, float pointLightHeight, float lightIntensity)
    {
        GameObject lightObject = new GameObject(objectName);
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(localPosition.x, pointLightHeight, localPosition.z);

        lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Light spotLight = lightObject.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = lightColor;
        spotLight.intensity = lightIntensity;
        spotLight.range = range;
        spotLight.spotAngle = spotAngle;
        spotLight.cookie = lightCookie;
        spotLight.shadows = LightShadows.None;
    }

    private float Get01(int x, int z, int salt)
    {
        return Mathf.Repeat(Mathf.Sin((x * 12.9898f) + (z * 78.233f) + (salt * 37.719f)) * 43758.5453f, 1f);
    }

    private void CreateGridLine(string objectName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject lineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lineObject.name = objectName;
        lineObject.transform.SetParent(transform, false);
        lineObject.transform.localPosition = localPosition;
        lineObject.transform.localScale = localScale;

        Collider lineCollider = lineObject.GetComponent<Collider>();
        if (lineCollider != null)
        {
            DestroyGeneratedObject(lineCollider);
        }

        Renderer lineRenderer = lineObject.GetComponent<Renderer>();
        if (lineRenderer != null && gridLineMaterial != null)
        {
            lineRenderer.sharedMaterial = gridLineMaterial;
        }
    }

    private void ClearGeneratedLights()
    {
        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject == null || sceneObject.transform.parent == transform || sceneObject.scene != gameObject.scene)
            {
                continue;
            }

            if (IsGeneratedObjectName(sceneObject.name))
            {
                DestroyGeneratedObject(sceneObject);
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (!IsGeneratedObjectName(child.name))
            {
                continue;
            }

            DestroyGeneratedObject(child.gameObject);
        }
    }

    private bool IsGeneratedObjectName(string objectName)
    {
        return objectName.StartsWith(LightNamePrefix)
            || objectName.StartsWith(LightFixtureNamePrefix)
            || objectName.StartsWith(OldOmniFillLightNamePrefix)
            || objectName.StartsWith(PanelNamePrefix)
            || objectName.StartsWith(TileNamePrefix)
            || objectName.StartsWith(GridLineNamePrefix);
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
