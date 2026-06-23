using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public string GetPrompt()
    {
        return "E: Test interaction";
    }

    public void Interact(PlayerInteractor interactor)
    {
        Debug.Log("Interaction worked.");
    }
}