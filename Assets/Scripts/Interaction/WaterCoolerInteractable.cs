using UnityEngine;

public class WaterCoolerInteractable : MonoBehaviour, IInteractable
{
    public float waterGain = 10f;

    public string GetPrompt()
    {
        return "E: Drink water";
    }

    public void Interact(PlayerInteractor interactor)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.AddWater(waterGain);
        }

        Debug.Log("Player drank water.");
    }
}
