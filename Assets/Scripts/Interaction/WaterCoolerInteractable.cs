using UnityEngine;

public class WaterCoolerInteractable : MonoBehaviour, IInteractable
{
    public float conditionGain = 25f;

    public string GetPrompt()
    {
        return "E: Drink water";
    }

    public void Interact(PlayerInteractor interactor)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.AddCondition(conditionGain);
        }

        Debug.Log("Player drank water.");
    }
}