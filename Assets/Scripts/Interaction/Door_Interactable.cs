using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public float openAngle = 90f;
    public float openSpeed = 5f;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * openSpeed
        );
    }

    public string GetPrompt()
    {
        return isOpen ? "E: Close door" : "E: Open door";
    }

    public void Interact(PlayerInteractor interactor)
    {
        isOpen = !isOpen;
    }
}