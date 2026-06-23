using UnityEngine;

public class HideSpotInteractable : MonoBehaviour, IInteractable
{
    public Transform hideCameraPoint;

    public string GetPrompt()
    {
        return "E: Hide";
    }

    public void Interact(PlayerInteractor interactor)
    {
        PlayerHiding hiding = interactor.GetComponent<PlayerHiding>();

        if (hiding == null)
        {
            Debug.LogWarning("Player does not have PlayerHiding script.");
            return;
        }

        if (hideCameraPoint == null)
        {
            Debug.LogWarning("HideSpotInteractable has no hideCameraPoint assigned.");
            return;
        }

        hiding.EnterHideSpot(hideCameraPoint);
    }
}