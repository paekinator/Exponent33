using UnityEngine;

/// <summary>
/// World pickup: press E next to a milk carton to add it to the player's
/// inventory (MilkItem). The carton itself disappears on pickup.
/// </summary>
public class MilkPickupInteractable : MonoBehaviour, IInteractable
{
    public string prompt = "E: Pick up milk";

    public string GetPrompt() => prompt;

    public void Interact(PlayerInteractor interactor)
    {
        MilkItem milkItem = interactor.GetComponent<MilkItem>();
        if (milkItem != null)
        {
            milkItem.AddMilk();
        }

        Destroy(gameObject);
    }
}
