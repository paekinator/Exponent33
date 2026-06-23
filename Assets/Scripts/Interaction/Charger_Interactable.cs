using UnityEngine;

public class ChargerInteractable : MonoBehaviour, IInteractable
{
    public float batteryGain = 25f;
    public float coverageGain = 10f;

    public string GetPrompt()
    {
        return "E: Charge phone";
    }

    public void Interact(PlayerInteractor interactor)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.AddBattery(batteryGain);
            stats.AddCoverage(coverageGain);
        }

        Debug.Log("Phone charged. Company coverage ping increased.");
    }
}