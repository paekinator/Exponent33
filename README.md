# UNASSIGNED

**UNASSIGNED** is a first-person liminal office horror demo built in Unity.

You are trapped in the forgotten backrooms of your workplace while your company still considers you **on shift**. Your only proof of escape is your phone, but that same phone is also how the company tracks you. Survive until **6:00 AM**, keep your phone alive, maintain enough company coverage to clock out, and avoid **The Supervisor** before he drags you back to work.

> **Main objective:** Remain Unassigned until 6:00 AM.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Storyline](#storyline)
- [Core Concept](#core-concept)
- [Gameplay Loop](#gameplay-loop)
- [Current Implemented Features](#current-implemented-features)
- [Planned Features](#planned-features)
- [Controls](#controls)
- [Core Systems](#core-systems)
- [Map and Level Design](#map-and-level-design)
- [Phone System](#phone-system)
- [Coverage System](#coverage-system)
- [Charging System](#charging-system)
- [Interaction System](#interaction-system)
- [Hiding System](#hiding-system)
- [Win and Lose Conditions](#win-and-lose-conditions)
- [Project Structure](#project-structure)
- [Development Roadmap](#development-roadmap)
- [Team Workflow](#team-workflow)
- [Credits and Assets](#credits-and-assets)

---

## Project Overview

**UNASSIGNED** is a short vertical-slice horror demo focused on simple but tense gameplay.

The player explores an office Backrooms environment while managing:

- **Phone Battery**
- **Company Coverage**
- **Physical Condition**
- **Hiding and survival**
- **Final clock-out verification**

The demo is designed to be completed in approximately **5–7 minutes**.

This project is not intended to be a full game yet. The current goal is to prove that the core loop is fun, understandable, and scary.

---

## Storyline

You were supposed to be working an overnight shift.

At some point, you left your workstation.

The company system noticed.

Your employee status changed to:

```text
UNASSIGNED
```

This should mean you are free from your assigned duties.

But the company does not see it that way.

Your phone is still inside company bounds. The forgotten backrooms beneath the office are still technically part of the workplace. The tracking system still says you are on shift.

Now the building has started treating you like missing company property.

Your boss, known only as **The Supervisor**, has been dispatched to retrieve you and return you to your workstation.

The only way out is to survive until **6:00 AM**, when your shift officially expires. At that moment, the company system will run a final attendance audit. If your phone still has power and enough coverage to verify your location, you can clock out.

If your phone dies, your signal is lost.

If coverage is too low, the system rejects your clock-out.

If The Supervisor catches you, you may be returned to work.

Your phone is your proof of freedom.

Your phone is also your tracking device.

---

## Core Concept

> Hide in the office Backrooms until 6:00 AM. Keep your phone charged, maintain enough company coverage to clock out, and avoid The Supervisor before he drags you back to work.

The horror comes from the contradiction at the center of the game:

- You need the phone to escape.
- The phone drains battery over time.
- You need coverage to clock out.
- Strong coverage makes you easier to locate.
- Charging keeps your phone alive.
- Charging forces you to put the phone down and wait.
- Hiding keeps you safe.
- Hiding too long wastes time and resources.

---

## Gameplay Loop

The player repeatedly does the following:

1. Explore the Backrooms.
2. Check the phone for timer, battery, coverage, condition, and objectives.
3. Move through the map to find chargers, water, snacks, and safe areas.
4. Avoid dangerous areas with poor coverage.
5. Hide when threatened.
6. Maintain enough resources for the final 6:00 AM audit.
7. Survive until clock-out.

---

## Current Implemented Features

The current project already includes the foundation of the playable demo:

### Player and Movement

- First-person controller
- Mouse look
- Walking and sprinting
- Crouching support depending on controller settings
- Collision-tested basic movement

### Interaction System

- Raycast-based interaction
- Interaction prompt UI
- `IInteractable` interface
- Press `E` to interact with objects
- Working interactable objects

### Door System

- Door pivot setup
- Door rotates from hinge instead of center
- Press `E` to open and close

### Hiding System

- Press `E` to enter hiding spots
- Player movement disabled while hiding
- Player can look around while hidden
- Press `E` again to exit hiding

### Phone System

- Press `Tab` to pull phone out
- Phone UI appears on screen
- 3D phone model rises into view
- Press `Tab` again to put the phone away
- Phone battery drains over time
- Battery drains faster while phone is open

### Coverage System

- Invisible coverage zones
- Strong coverage zones increase coverage
- Weak coverage zones slowly drain coverage
- Lost coverage zones drain coverage quickly
- Phone UI displays coverage state and percentage

### Charging System

- Phone must be held out before charging
- Press `E` near a charging station to place the phone down
- Hand phone disappears while charging
- Charging-table phone prefab appears
- Battery charges slowly over time
- Charging progress is shown when looking at the charging station
- Press `E` again to take the phone back
- Charging stops when phone is taken back

### Environment

- Backrooms-style asset pack imported
- Modular map construction in progress
- Looping Backrooms layout planned
- Doors, ceiling lights, corridors, and rooms being integrated

---

## Planned Features

These are the major systems still planned for the demo:

### The Supervisor

The Supervisor will be the main enemy.

Planned behavior:

- Patrols the Backrooms
- Detects the player through vision and distance
- Investigates noises
- Chases the player when spotted
- Catches the player if close enough
- Returns the player to a previous area or triggers a penalty
- Becomes more aggressive during the final audit

### Noise Events

Planned noise events:

- Printer starts reporting the player's location
- Phone rings at dangerous moments
- Office equipment activates unexpectedly
- Strong coverage ping may attract The Supervisor

### Final Audit

At 6:00 AM, the company system checks:

- Phone battery is above 0%
- Coverage is high enough
- Player has not failed key survival conditions

If the player passes:

```text
Clock-out approved.
Employee status: UNASSIGNED.
```

If the player fails:

```text
Clock-out failed.
Manual retrieval authorized.
```

---

## Controls

| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Sprint | Left Shift |
| Interact | E |
| Open / Close Phone | Tab |
| Crouch | C or Ctrl, depending on controller settings |
| Pause | Esc |

---

## Core Systems

### Player Resources

The player manages three main values:

| Resource | Meaning |
|---|---|
| Battery | Phone power required for clock-out |
| Coverage | Company signal required for verification |
| Condition | Physical state of the player |

The player does not instantly lose if one resource becomes low. Instead, the systems create pressure and force the player to make decisions.

---

## Map and Level Design

The map is designed to feel like the Backrooms:

- Yellow walls
- Repeating corridors
- Fluorescent ceiling lights
- Similar-looking turns
- Fake paths
- Dead ends
- Office props in unnatural places
- Looping layout

The actual playable map should remain small and controlled.

The intended layout is:

```text
                  [Printer Room]
                       |
[Start Storage] -- [Main Hall] -- [Break Room]
                       |             |
                [Server Room] -- [Return Hall]
                       |
                [Final Audit Zone]
```

The player should feel like the environment is endless, but the actual demo should guide them through a clear survival route.

---

## Phone System

The phone is the central mechanic.

The phone shows:

- Current employee status
- Shift timer
- Battery
- Coverage
- Condition
- Current objective
- Warnings

Example phone screen:

```text
CompanyTrack™

Status: UNASSIGNED
Location: COMPANY BOUNDS
Shift Ends: 04:32
Battery: 46%
Coverage: WEAK
Condition: TIRED
Objective: Remain Unassigned until 6:00 AM
```

The phone can be opened with `Tab`.

When opened:

- The phone rises into view.
- The phone UI appears.
- Battery drains faster.

When closed:

- The phone lowers.
- The phone UI disappears.
- Battery drains slower.

---

## Coverage System

Coverage represents the company's tracking signal.

Coverage can be:

| Coverage State | Gameplay Meaning |
|---|---|
| Strong | Good for final clock-out, but dangerous later |
| Weak | Normal exploration state |
| Lost | Dangerous; clock-out may fail |

Coverage zones are invisible trigger areas placed around the map.

Examples:

| Area | Coverage |
|---|---|
| Main hallway | Weak |
| Server room | Strong |
| Charger area | Strong |
| Deep Backrooms dead end | Lost |
| Final audit zone | Strong |

The player must manage coverage carefully. Too little coverage means they may fail clock-out verification. Later, high coverage may also make The Supervisor more likely to locate the player.

---

## Charging System

Charging is intentionally not instant.

To charge the phone:

1. Press `Tab` to hold the phone.
2. Approach a charging station.
3. Press `E` to place the phone on the charger.
4. The hand phone disappears.
5. A phone-on-charger prefab appears on the table.
6. Battery slowly increases over time.
7. Look at the charger to see the current charging percentage.
8. Press `E` to take the phone back.
9. Charging stops.

This creates tension because the player must decide how long to leave the phone charging.

The phone is safer when charged, but charging keeps the player near a predictable location.

---

## Interaction System

The game uses a shared interaction format:

```text
Look at object → prompt appears → press E
```

Examples:

```text
E: Open door
E: Hide
E: Place phone on charger
Charging... 58%
E: Take phone
E: Drink water
E: Stop printer
```

The system is built around an `IInteractable` interface, allowing different objects to share the same interaction logic.

---

## Hiding System

Hiding spots are used to avoid The Supervisor.

Planned hiding spots:

- Lockers
- Under desks
- Maintenance closets
- Cubicle gaps
- Behind filing cabinets

Current hiding behavior:

- Press `E` to hide.
- Player movement is disabled.
- Player can still look around.
- Press `E` to exit hiding.

Future hiding behavior may include:

- Condition drain while hiding
- Boss checking nearby hiding spots
- Different safety levels for different hiding spots

---

## Win and Lose Conditions

### Win Condition

At 6:00 AM, the final audit passes if:

```text
Battery > 0
Coverage >= required threshold
Player has not failed survival conditions
```

Good ending:

```text
06:00 AM
Shift expired.
Clock-out verification received.
Employee status: UNASSIGNED.
```

### Lose Conditions

The player can lose if:

- The Supervisor catches them too many times
- Phone battery is dead at final audit
- Coverage is too low at final audit
- Condition collapses
- Final audit fails

Bad ending examples:

```text
Clock-out verification failed.
Employee signal lost.
Manual retrieval authorized.
```

```text
Location verification invalid.
Employee remains within company bounds.
Supervisor dispatched.
```

```text
Returned to workstation.
Shift restarted.
```

---

## Project Structure

Recommended Unity folder structure:

```text
Assets/
  _Project/
    Animations/
    Art/
      Characters/
      Environment/
      Props/
    Audio/
      Ambience/
      SFX/
      Voice/
    Materials/
    Prefabs/
      Interactables/
      Player/
      Supervisor/
      UI/
    Scenes/
      MainMenu.unity
      Game.unity
    Scripts/
      Core/
      Player/
      Interaction/
      Phone/
      AI/
      UI/
      Environment/
      Audio/
    ScriptableObjects/
    UI/
```

---

## Development Roadmap

### Phase 1 — Core Foundation

Status: In progress / mostly implemented.

- First-person movement
- Basic map
- Interaction system
- Door interaction
- Hiding interaction
- Phone open/close
- Battery drain
- Coverage zones
- Slow phone charging

### Phase 2 — Playable Survival Loop

Next goals:

- Condition system polish
- Water cooler and vending machine interactions
- Final audit timer
- Good and bad ending logic
- Strong/weak/lost coverage balancing
- Clear objective text

### Phase 3 — Enemy AI

Planned:

- Supervisor patrol route
- Vision detection
- Chase state
- Catch sequence
- Player reset or penalty
- Boss investigation behavior
- Boss reaction to charging/coverage pings

### Phase 4 — Scripted Horror Events

Planned:

- Printer location reveal event
- Phone warning messages
- Low battery alerts
- Final attendance audit sequence
- Emergency lights or red audit lighting
- Boss voice lines

### Phase 5 — Polish

Planned:

- Lighting pass
- Ambient sound
- Footstep sounds
- UI polish
- Phone animation polish
- Backrooms dressing
- Bug fixing
- Main menu
- Ending screen
- Final build

---

## Team Workflow

Recommended workflow:

1. Work on separate branches.
2. Do not edit the same Unity scene at the same time unless agreed.
3. Use prefabs for interactable objects.
4. Test after every merge.
5. Prioritize playable systems before visual polish.
6. Keep a backup testing scene.
7. Push working code only.
8. If a feature breaks the build, disable it instead of blocking the team.

Suggested branches:

```text
main
dev
player-interaction
phone-system
level-design
boss-ai
ui-polish
audio-narrative
```

---

## Development Notes

Important design rules:

- The demo should be short.
- The map should feel larger than it really is.
- The phone is both useful and dangerous.
- Charging should create tension.
- Coverage should matter.
- Hiding should help, but should not make the player permanently safe.
- The Supervisor should be simple but reliable.
- A polished 5-minute loop is better than a broken large game.

---

## Credits and Assets

This project uses Unity and third-party assets for prototyping.

Current asset categories include:

- Modular Backrooms environment assets
- First-person controller asset
- Phone model / charging prefab
- Doors and ceiling lights
- Office props and interactables

Asset licenses should be checked before commercial release.

---

## Pitch Text

**UNASSIGNED** is a first-person office liminal horror game where your company tracks your shift status through your phone. You hide in the forgotten Backrooms of your workplace until your shift expires, while managing phone battery, physical condition, and company coverage. Too little coverage and you cannot clock out; too much coverage and The Supervisor can find you. Survive until 6:00 AM and remain Unassigned.
