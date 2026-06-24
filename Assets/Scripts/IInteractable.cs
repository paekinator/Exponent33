public interface IInteractable
{
    string GetPrompt();
    void Interact(PlayerInteractor interactor);
}

/// <summary>
/// Implement alongside IInteractable for things you HOLD E on (water dispenser,
/// phone charger). PlayerInteractor calls HoldTick every frame E is held while
/// you're looking at the object.
/// </summary>
public interface IHoldInteractable
{
    void HoldTick(PlayerInteractor interactor, float deltaTime);
}
