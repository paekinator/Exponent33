# Unity Interaction Coding Notes

This is a learning note for understanding how the interaction code in this game works.

The big idea:

> The player detects an object. The object decides what happens.

That means the player code does not need to know if something is a water dispenser, charger, door, locker, bottle, or keypad. The player only asks:

> "Are you interactable?"

If yes, the player calls the object's interaction function.

---

## 1. C# vs Unity

Unity games are usually written in **C#**.

C# is the programming language:

```csharp
float water = 100f;

if (water <= 0f)
{
    Debug.Log("Out of water");
}
```

Unity is the game engine. Unity gives you game-specific tools:

```csharp
Input.GetKey(KeyCode.E)
Time.deltaTime
Physics.Raycast(...)
GetComponent<PlayerStats>()
transform.position
```

So when writing Unity code, you are using:

```text
C# language + Unity engine functions + your own game structure
```

---

## 2. Important Unity Functions

### Awake

Runs when the object is loaded.

Good for setting up references:

```csharp
void Awake()
{
    playerCamera = GetComponentInChildren<Camera>();
}
```

### Start

Runs before the first frame.

Good for spawning or initializing things after everything exists:

```csharp
void Start()
{
    Debug.Log("Game started");
}
```

### Update

Runs every frame.

Good for input:

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.E))
    {
        Debug.Log("Pressed E");
    }
}
```

### FixedUpdate

Runs on the physics tick.

Good for Rigidbody movement:

```csharp
void FixedUpdate()
{
    rb.AddForce(Vector3.forward);
}
```

---

## 3. Input

Unity can check keyboard input like this:

```csharp
Input.GetKeyDown(KeyCode.E)
```

This is true only on the frame the key is first pressed.

Use it for one-time actions:

```csharp
if (Input.GetKeyDown(KeyCode.E))
{
    OpenDoor();
}
```

For holding a key:

```csharp
Input.GetKey(KeyCode.E)
```

This stays true while the key is held down.

Use it for actions over time:

```csharp
if (Input.GetKey(KeyCode.E))
{
    DrinkWater(Time.deltaTime);
}
```

For key release:

```csharp
Input.GetKeyUp(KeyCode.E)
```

---

## 4. Components

Unity objects are made of components.

A player might have:

- Transform
- Rigidbody
- Collider
- FirstPersonController
- PlayerStats
- PlayerInteractor

You get another component like this:

```csharp
PlayerStats stats = GetComponent<PlayerStats>();
```

That means:

> Look on this same GameObject for a PlayerStats script.

You can also search children:

```csharp
Camera cam = GetComponentInChildren<Camera>();
```

Or search the scene:

```csharp
BossAI boss = Object.FindAnyObjectByType<BossAI>();
```

Scene searching is useful sometimes, but do not use it constantly every frame.

---

## 5. Raycasting

Raycasting means shooting an invisible line into the world.

For first-person interactions, we usually raycast from the camera forward.

```csharp
Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
{
    Debug.Log("Looking at: " + hit.collider.name);
}
```

This lets the game know what object the player is looking at.

In this game, raycasting is used for interactions:

```text
Camera shoots ray forward
Ray hits object
Check if object is interactable
If player presses E, interact
```

---

## 6. Interfaces

An interface is a contract.

It says:

> Any class that uses this interface must have these functions.

Example:

```csharp
public interface IInteractable
{
    string GetPrompt();
    void Interact(PlayerInteractor interactor);
}
```

This means every interactable object must have:

```csharp
GetPrompt()
Interact(...)
```

So the player can treat all interactables the same.

---

## 7. Why Interfaces Are Useful

Without interfaces, the player code might become messy:

```csharp
if (hit.collider.GetComponent<Door>() != null)
{
    hit.collider.GetComponent<Door>().Open();
}
else if (hit.collider.GetComponent<WaterCooler>() != null)
{
    hit.collider.GetComponent<WaterCooler>().Drink();
}
else if (hit.collider.GetComponent<Charger>() != null)
{
    hit.collider.GetComponent<Charger>().Charge();
}
```

That gets ugly fast.

With an interface:

```csharp
IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

if (interactable != null)
{
    interactable.Interact(this);
}
```

The player does not care what kind of object it is.

The object handles its own behavior.

---

## 8. Basic Interaction Flow

The architecture looks like this:

```text
PlayerInteractor
    |
    | raycast
    v
Interactable object
    |
    | Interact()
    v
Object-specific behavior
```

Example:

```text
Player looks at water dispenser
Player holds E
Water dispenser script adds water to PlayerStats
```

---

## 9. PlayerInteractor Example

This is the basic shape of an interaction scanner:

```csharp
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 3f;

    IInteractable currentInteractable;

    void Update()
    {
        CheckForInteractable();

        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact(this);
        }
    }

    void CheckForInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
            }
        }
    }
}
```

This script does not open doors, drink water, or charge phones.

It only detects interactable objects and calls them.

---

## 10. One-Time Interaction Example

For something like a door:

```csharp
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    bool isOpen;

    public string GetPrompt()
    {
        return isOpen ? "E: Close door" : "E: Open door";
    }

    public void Interact(PlayerInteractor interactor)
    {
        isOpen = !isOpen;
        Debug.Log(isOpen ? "Door opened" : "Door closed");
    }
}
```

Pressing `E` once toggles the door.

---

## 11. Hold Interaction

Some interactions happen while holding a key, not just pressing once.

Examples:

- drinking water
- charging phone
- holding a valve
- pulling a lever over time

For that, this game uses another interface:

```csharp
public interface IHoldInteractable
{
    void HoldTick(PlayerInteractor interactor, float deltaTime);
}
```

`deltaTime` is the time since the last frame.

This lets us write "10 per second" behavior:

```csharp
stats.AddWater(10f * deltaTime);
```

If the frame is short, it adds a small amount.

Over one full second, it adds about 10.

---

## 12. Water Dispenser Example

```csharp
using UnityEngine;

public class WaterCoolerInteractable : MonoBehaviour, IInteractable, IHoldInteractable
{
    public string GetPrompt()
    {
        return "Hold E: Drink water";
    }

    public void Interact(PlayerInteractor interactor)
    {
        // Single tap does nothing.
        // Drinking happens while E is held.
    }

    public void HoldTick(PlayerInteractor interactor, float deltaTime)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.AddWater(stats.refillPerSecond * deltaTime);
        }
    }
}
```

Important part:

```csharp
stats.AddWater(stats.refillPerSecond * deltaTime);
```

If `refillPerSecond` is `10`, then holding E restores about `10%` per second.

---

## 13. Phone Charger Example

```csharp
using UnityEngine;

public class ChargerInteractable : MonoBehaviour, IInteractable, IHoldInteractable
{
    public string GetPrompt()
    {
        return "Hold E: Charge phone";
    }

    public void Interact(PlayerInteractor interactor)
    {
        // Single tap does nothing.
    }

    public void HoldTick(PlayerInteractor interactor, float deltaTime)
    {
        PlayerStats stats = interactor.GetComponent<PlayerStats>();

        if (stats != null)
        {
            stats.AddPhone(stats.refillPerSecond * deltaTime);
        }
    }
}
```

The water dispenser and charger are almost the same.

The difference is:

```csharp
AddWater(...)
```

versus:

```csharp
AddPhone(...)
```

---

## 14. Player Stats

The player stats script owns the actual water and phone values.

Example:

```csharp
public class PlayerStats : MonoBehaviour
{
    public float maxWater = 100f;
    public float water = 100f;

    public float maxPhone = 100f;
    public float phone = 100f;

    public void AddWater(float amount)
    {
        water = Mathf.Clamp(water + amount, 0f, maxWater);
    }

    public void AddPhone(float amount)
    {
        phone = Mathf.Clamp(phone + amount, 0f, maxPhone);
    }
}
```

`Mathf.Clamp` keeps the value inside a range.

So water cannot go above 100 or below 0:

```csharp
water = Mathf.Clamp(water, 0f, 100f);
```

---

## 15. Different Objects, Same Button

The same `E` key can do different things because each object has its own script.

```text
Door -> opens door
Water dispenser -> adds water
Charger -> adds phone charge
Locker -> hides player
Bottle -> adds item to inventory
Vent -> moves player to new area
```

The player interactor does not need to know all these details.

Each object owns its own behavior.

---

## 16. How To Add A New Interactable

Example: a water bottle pickup.

Step 1: Create script:

```csharp
using UnityEngine;

public class WaterBottleInteractable : MonoBehaviour, IInteractable
{
    public string GetPrompt()
    {
        return "E: Pick up water bottle";
    }

    public void Interact(PlayerInteractor interactor)
    {
        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();

        if (inventory != null)
        {
            inventory.AddWaterBottle();
            Destroy(gameObject);
        }
    }
}
```

Step 2: Add script to the water bottle object in Unity.

Step 3: Make sure the object has a collider.

Step 4: Press Play, look at the object, press E.

---

## 17. Common Unity Interaction Mistakes

### No Collider

Raycasts hit colliders.

If the object has no collider, the player cannot interact with it.

### Script On Wrong Object

If the script is on the parent but the collider is on the child, use:

```csharp
GetComponentInParent<IInteractable>()
```

### Forgetting To Assign Camera

If the player interactor has no camera, raycasting will not work.

Fix:

```csharp
playerCamera = GetComponentInChildren<Camera>();
```

### Doing Too Much In PlayerInteractor

Bad:

```text
PlayerInteractor handles door, water, phone, bottle, locker, vent...
```

Good:

```text
PlayerInteractor only detects and calls Interact().
Each object handles itself.
```

---

## 18. What To Learn Next

Recommended order:

1. C# basics
2. Unity `MonoBehaviour`
3. `Update`, `Awake`, `Start`, `FixedUpdate`
4. Input with `Input.GetKey`
5. Raycasts
6. Colliders
7. Components and `GetComponent`
8. Interfaces
9. Inventory systems
10. ScriptableObjects
11. Events
12. State machines for AI

For this game, the most important pattern is:

```text
Detect -> Interact -> Object decides behavior
```

Once you understand that, you can build most gameplay objects in the project.

