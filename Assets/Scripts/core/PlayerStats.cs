using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public System.Action<PlayerStats> OnStatsChanged;

    [Header("Phone")]
    [Range(0, 100)] public float battery = 60f;
    [Range(0, 100)] public float coverage = 50f;

    [Header("Body")]
    [Range(0, 100)] public float condition = 100f;
    [Range(0, 100)] public float water = 100f;

    [Header("UI")]
    public bool createDefaultWaterBar = true;

    [Header("Drain Rates")]
    public float batteryDrainPerSecond = 0.5f;
    public float conditionDrainPerSecond = 0.2f;
    public float waterDrainPerSecond = 1f;

    void Start()
    {
        if (water <= 0f)
        {
            water = 100f;
        }

        if (waterDrainPerSecond <= 0f)
        {
            waterDrainPerSecond = 1f;
        }

        if (createDefaultWaterBar && FindObjectOfType<PlayerStatsUI>() == null)
        {
            PlayerStatsUI.CreateDefaultFor(this);
        }
    }

    void Update()
    {
        battery -= batteryDrainPerSecond * Time.deltaTime;
        condition -= conditionDrainPerSecond * Time.deltaTime;
        water -= waterDrainPerSecond * Time.deltaTime;

        battery = Mathf.Clamp(battery, 0f, 100f);
        coverage = Mathf.Clamp(coverage, 0f, 100f);
        condition = Mathf.Clamp(condition, 0f, 100f);
        water = Mathf.Clamp(water, 0f, 100f);

        OnStatsChanged?.Invoke(this);
    }

    public void AddBattery(float amount)
    {
        battery = Mathf.Clamp(battery + amount, 0f, 100f);
        Debug.Log("Battery: " + battery);
        OnStatsChanged?.Invoke(this);
    }

    public void AddCoverage(float amount)
    {
        coverage = Mathf.Clamp(coverage + amount, 0f, 100f);
        Debug.Log("Coverage: " + coverage);
        OnStatsChanged?.Invoke(this);
    }

    public void AddCondition(float amount)
    {
        condition = Mathf.Clamp(condition + amount, 0f, 100f);
        Debug.Log("Condition: " + condition);
        OnStatsChanged?.Invoke(this);
    }

    public void AddWater(float amount)
    {
        water = Mathf.Clamp(water + amount, 0f, 100f);
        Debug.Log("Water: " + water);
        OnStatsChanged?.Invoke(this);
    }
}
