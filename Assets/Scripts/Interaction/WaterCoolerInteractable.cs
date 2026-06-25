using UnityEngine;

/// <summary>
/// Water dispenser. HOLD E while looking at it to refill the player's water at
/// PlayerStats.refillPerSecond (10%/s by default).
/// </summary>
public class WaterCoolerInteractable : MonoBehaviour, IInteractable, IHoldInteractable
{
    public string GetPrompt()
    {
        return "Hold E: Drink water";
    }

    // Single tap does nothing special — refilling happens on HOLD.
    public void Interact(PlayerInteractor interactor) { }

    public void HoldTick(PlayerInteractor interactor, float deltaTime)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();
        if (stats != null) stats.AddWater(stats.refillPerSecond * deltaTime);

        PlayerDrinkAudio drinkAudio = interactor.GetComponent<PlayerDrinkAudio>();
        if (drinkAudio != null) drinkAudio.HoldDrinkTick();
    }
}
