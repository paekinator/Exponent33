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

        // Hold-to-use objects (water dispenser, phone charger): tick while E held.
        if (currentInteractable is IHoldInteractable holdable && Input.GetKey(KeyCode.E))
        {
            holdable.HoldTick(this, Time.deltaTime);
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

                return;
            }
        }

        IInteractable nearby = FindNearbyInteractable();
        if (nearby != null)
        {
            currentInteractable = nearby;
            if (promptText != null)
            {
                promptText.text = nearby.GetPrompt();
            }
        }
    }

    IInteractable FindNearbyInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactDistance);
        IInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform))
            {
                continue;
            }

            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(hit.transform.position - transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }
}
