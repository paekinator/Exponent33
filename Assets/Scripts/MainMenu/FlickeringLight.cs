using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    public Light lightToFlicker;
    [SerializeField, Min(0f)] private float minIntensity = 0.5f;
    [SerializeField, Min(0f)] private float maxIntensity = 1.2f;
    [SerializeField, Min(0.01f)] private float timeBetweenIntensity = 0.07f;
    [SerializeField, Min(0f)] private float minStableTime = 1.4f;
    [SerializeField, Min(0f)] private float maxStableTime = 4.5f;
    [SerializeField, Min(0.01f)] private float minFlickerTime = 0.18f;
    [SerializeField, Min(0.01f)] private float maxFlickerTime = 0.65f;
    [SerializeField, Range(0f, 1f)] private float offChance = 0.18f;

    private float stableIntensity;
    private float nextBurstTime;
    private float burstEndTime;
    private float nextIntensityTime;
    private bool isFlickering;

    private void Awake() {
        if (lightToFlicker == null) {
            lightToFlicker = GetComponent<Light>();
        }

        if (lightToFlicker == null) {
            enabled = false;
            return;
        }

        stableIntensity = lightToFlicker.intensity;
        if (maxIntensity <= 0f) {
            maxIntensity = stableIntensity;
        }

        ScheduleNextBurst();
    }

    private void Update() {
        if (!isFlickering && Time.time >= nextBurstTime) {
            isFlickering = true;
            burstEndTime = Time.time + Random.Range(minFlickerTime, maxFlickerTime);
            nextIntensityTime = 0f;
        }

        if (!isFlickering) {
            lightToFlicker.intensity = Mathf.Lerp(lightToFlicker.intensity, stableIntensity, Time.deltaTime * 8f);
            return;
        }

        if (Time.time >= burstEndTime) {
            isFlickering = false;
            ScheduleNextBurst();
            return;
        }

        if (Time.time >= nextIntensityTime) {
            lightToFlicker.intensity = Random.value < offChance ? 0f : Random.Range(minIntensity, maxIntensity);
            nextIntensityTime = Time.time + timeBetweenIntensity;
        }
    }

    private void ScheduleNextBurst() {
        float stableMin = Mathf.Min(minStableTime, maxStableTime);
        float stableMax = Mathf.Max(minStableTime, maxStableTime);
        nextBurstTime = Time.time + Random.Range(stableMin, stableMax);
    }
}
