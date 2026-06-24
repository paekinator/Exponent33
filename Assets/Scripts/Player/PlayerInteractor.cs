using UnityEngine;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public Camera playerCamera;
    public float interactDistance = 3f;

    [Header("UI")]
    public TextMeshProUGUI promptText;

    private IInteractable currentInteractable;
    private PlayerHiding playerHiding;

    void Awake()
    {
        playerHiding = GetComponent<PlayerHiding>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        if (playerHiding != null && playerHiding.isHidden)
        {
            if (promptText != null)
            {
                promptText.text = "E: Exit hiding";
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                playerHiding.ExitHideSpot();
            }

            return;
        }

        CheckForInteractable();

        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact(this);
        }
    }

    void CheckForInteractable()
    {
        currentInteractable = null;

        if (promptText != null)
        {
            promptText.text = "";
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerInteractor has no camera assigned.");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;

                if (promptText != null)
                {
                    promptText.text = interactable.GetPrompt();
                }
            }
        }
    }
}
