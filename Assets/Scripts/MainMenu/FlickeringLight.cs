using UnityEngine;

// inspo https://www.youtube.com/watch?v=4Q4BFykWOPY


public class FlickeringLight : MonoBehaviour
{
    public Light lightToFlicker;
    [SerializeField, Range(0f, 3f)] private float minIntensity = 0.5f;
    [SerializeField, Range(0f, 3f)] private float maxIntensity = 1.2f;
    [SerializeField, Min(0f)] private float timeBetweenIntensity = 0.1f;


    private float currentTimer;

    private void Awake() {
        if (lightToFlicker == null) {
            lightToFlicker = GetComponent<Light>();
        }
    }

    private void Update() {
        currentTimer += Time.deltaTime;
        if (!(currentTimer >= timeBetweenIntensity)) {
            lightToFlicker.intensity = Random.Range(minIntensity, maxIntensity);
            currentTimer = 0;
        }
    }

}
