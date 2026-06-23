using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Phone")]
    [Range(0, 100)] public float battery = 60f;
    [Range(0, 100)] public float coverage = 50f;

    [Header("Body")]
    [Range(0, 100)] public float condition = 100f;

    void Update()
    {
        battery -= 0.5f * Time.deltaTime;
        condition -= 0.2f * Time.deltaTime;

        battery = Mathf.Clamp(battery, 0f, 100f);
        coverage = Mathf.Clamp(coverage, 0f, 100f);
        condition = Mathf.Clamp(condition, 0f, 100f);
    }

    public void AddBattery(float amount)
    {
        battery = Mathf.Clamp(battery + amount, 0f, 100f);
        Debug.Log("Battery: " + battery);
    }

    public void AddCoverage(float amount)
    {
        coverage = Mathf.Clamp(coverage + amount, 0f, 100f);
        Debug.Log("Coverage: " + coverage);
    }

    public void AddCondition(float amount)
    {
        condition = Mathf.Clamp(condition + amount, 0f, 100f);
        Debug.Log("Condition: " + condition);
    }
}