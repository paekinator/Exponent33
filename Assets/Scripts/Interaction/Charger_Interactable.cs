using UnityEngine;

/// <summary>
/// Phone charger. HOLD E while looking at it to recharge the player's phone at
/// PlayerStats.refillPerSecond (10%/s by default).
/// </summary>
public class ChargerInteractable : MonoBehaviour, IInteractable, IHoldInteractable
{
    public string GetPrompt()
    {
        return "Hold E: Charge phone";
    }

    // Single tap does nothing special — charging happens on HOLD.
    public void Interact(PlayerInteractor interactor) { }

    public void HoldTick(PlayerInteractor interactor, float deltaTime)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();
        if (stats != null) stats.AddPhone(stats.refillPerSecond * deltaTime);
    }
}
