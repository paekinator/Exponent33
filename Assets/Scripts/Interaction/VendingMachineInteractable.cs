using UnityEngine;

public class VendingMachineInteractable : MonoBehaviour, IInteractable
{
    public float conditionGain = 35f;

    public string GetPrompt()
    {
        return "E: Get snack";
    }

    public void Interact(PlayerInteractor interactor)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.AddCondition(conditionGain);
        }

        Debug.Log("Snack eaten.");
    }
}