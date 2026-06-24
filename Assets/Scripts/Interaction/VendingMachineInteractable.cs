using UnityEngine;

public class VendingMachineInteractable : MonoBehaviour, IInteractable
{
    public string GetPrompt()
    {
        return "E: Get snack";
    }

    public void Interact(PlayerInteractor interactor)
    {
        Debug.Log("Snack eaten.");
    }
}
