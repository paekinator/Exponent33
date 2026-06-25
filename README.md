# BackShift

A first-person survival horror game about a phone that's tracking you, a body
that needs water, and a boss who is never quite far enough away.

---

## 1. Premise

You're an office worker who's found Paul's secret room — a hidden space
underneath the normal office, hot, disorienting, and clearly not meant to be
found. You're dehydrating fast. Your company phone is draining faster than it
should, and it's the kind of phone that **proves you're still on shift**: if
it dies, it sends a distress signal, your boss finds out exactly where you
are, and he comes looking.

It's 8:55. Shift ends in five minutes. You can't go back upstairs until the
night meeting pulls everyone to the other side of the office — so you wait it
out down here, keeping yourself hydrated, your phone charged, and yourself out
of sight, until the timer runs out and it's safe to sneak back.

Twenty seconds after you arrive, you hear him. Laughing. On the phone. Closer
than you'd like.

## 2. Genre

- First-person survival horror
- Resource management (two depleting meters, both punishing at zero)
- Stealth via sound and sight, not combat — there is no way to fight back
- Single-objective, timed escape (no exploration reward beyond surviving)

## 3. Core Gameplay Loop

1. Move through the Backrooms space, watching **water** and **phone charge**
   drain in the corner of the screen.
2. Refill at a **water dispenser** or **phone charger** (hold E) — or drink a
   carried **milk carton** for an instant water refill.
3. Stay aware of how much **noise** you're making — every action that makes
   noise can be heard by the boss from a distance proportional to how loud it
   was.
4. If the boss is closing in, duck into a **locker** to hide — noise drops to
   zero and you become invisible to it, but you're frozen in place until you
   come back out.
5. Survive **5 minutes** of game-clock (the clock pauses during dialogue, so
   it's 5 minutes of actual pressure, not wall-clock time).

The tension is from two independent failure states racing the survival timer:
run out of water and you faint; run out of phone charge and the boss is told
exactly where you are.

## 4. Win / Lose Conditions

| Outcome | Trigger |
|---|---|
| **Survived** (win) | `GameTimer` reaches `survivalSeconds` (300s / 5 minutes) |
| **Dehydrated** (lose) | `water` reaches 0 |
| **Caught** (lose) | The boss reaches `catchDistance` of the player |

Phone charge reaching 0 is not an instant loss — it triggers **distress**
(the boss beelines straight at the player's live position, no stealth options
left) rather than ending the run by itself. `GameEndManager` owns all three
checks and self-installs into the scene on load, so the win/loss logic works
without any manual wiring.

## 5. Player Systems

### Movement
Standard first-person controller (walk / sprint / crouch / jump / head-bob),
built on a Rigidbody rather than a CharacterController. Sprinting and walking
both drain water faster than standing still; crouching halves move speed but
makes zero noise.

### Survival Stats (`PlayerStats`)
- **Water**: 100 → 0, drains 1/s (1.5x while sprinting). Hitting 0 faints the
  player (movement disabled, cursor unlocked, end screen shown).
- **Phone charge**: 100 → 0, drains at 1% every 1.5s (1.5x while the torch is
  on). Hitting 0 sets `distressActive` on the boss.
- Both refill at 10%/s while holding E at their respective station.

### Noise (`PlayerNoiseMeter`)
Noise is **event-based, not accumulating** — each frame it snaps instantly to
whichever single source is loudest *right now*, then drains fast once nothing
is making noise:

| Source | Noise level |
|---|---|
| Running | 80 (sustained — only while actually sprinting) |
| Walking | 30 (sustained) |
| Landing from a fall | 100 (one-off spike) |
| Hitting a wall | 20 (one-off spike) |
| Crouching | 0 (contributes nothing either way) |
| *(nothing)* | decays toward 0 at 50/second |

The boss's hearing range scales directly with this value, so a sprinting
player is audible from much further than a walking one, and a one-off spike
(a fall, a wall bump) fades back to silence in about two seconds.

### Interaction (`PlayerInteractor` / `IInteractable`)
A simple raycast-based interaction system. Two flavors:
- **Tap E** — pick up a milk carton, enter/exit a hiding spot.
- **Hold E** (`IHoldInteractable`) — drink at a water dispenser, charge at a
  phone charger. Both play a looping sound for as long as E is held.

### Hiding (`PlayerHiding` + locker prefabs)
Tap E at a locker to duck inside: position and camera snap to the locker's
interior viewpoint, the Rigidbody goes kinematic (immune to gravity/momentum,
so you can't drift out of place), and the only input still live is a
clamped look-around. Noise is forced to 0, the boss can't spot you visually,
and **the phone and torch are both forced off and can't be turned back on**
until you exit (tap E again) — a lit screen would give the hiding spot away.

### Phone & Torch (`PhoneViewmodel`)
A held first-person phone viewmodel (toggle with `1`) with a torch (`T`) that
lights a real spotlight and drains phone charge 1.5x faster while on. Torch
usage quietly makes the player easier for the boss to spot (see below) —
nothing is shown on screen about this; it's a hidden risk, not a UI stat.

### Milk Cartons (`MilkItem`)
A stackable pickup placed around the map. Picking one up adds to a count
shown as sequential HUD slots (2, 3, 4…); drinking one (E) restores +50 water
and decrements the stack.

## 6. The Boss

The boss is the most complex system in the game — three animation states
(`Phone`, `Walk`, `Run`) chosen fresh every frame from distance, hearing, and
sight, plus a few persistent behaviors layered on top.

### Idle — Phone
Default state. The boss is always loosely aware of the player but stays on a
soft leash: within `leashDistance` (70m) it just paces near its current spot;
beyond that it paths back toward the player (via NavMesh — it never cuts
through walls) without beelining.

### Walk — Heard
Noise is event-based now, so hearing is too: the instant the boss is within
the player's current audible range, it **snapshots that position** and walks
there — and keeps walking there even after the noise itself has already
decayed back to 0, until it actually arrives (or a closer new noise
re-snapshots the target). Standing still after one noise spike means the boss
walks to where you *used to be* — you have to keep moving to keep being
tracked in real time.

### Run — Spotted
Line-of-sight raycast, blocked by walls and by hiding. The angle that counts
depends on whether the player is **detectable**:
- **Detectable** (making any noise right now, *or* phone torch on) — spotted
  from any angle the boss can see, front or back.
- **Silent and dark** — only spotted within the boss's forward vision cone
  (`forwardSightHalfAngle`, 60° either side of dead-ahead).

Being spotted screams once (rate-limited so re-spotting doesn't spam it) and
charges the last-seen position at full speed. If sight is lost, it keeps
running to that last-seen spot to investigate before giving up to Walk/Phone.

### Distress
Set externally by `PlayerStats` when the phone dies. Overrides everything
else and beelines the player's *exact live position* — no stealth, no
forward-cone exception, no scream (that's reserved for an actual visual
spotting). Clears automatically once the phone is charged again.

### Exhaustion
Running continuously (Run mode, including distress) for `tiredAfterRunningSeconds`
forces a `tiredDurationSeconds` recovery: locked into the Walk animation at a
fraction of normal walk speed, plus an audible panting loop. It still heads
for the same target, it just can't sprint there — giving the player a
breathing-room window before it's eligible to run again.

### Giving up on a hidden player
If the boss was mid-chase when the player ducked into a locker, it finishes
investigating the last-seen spot first (the "walks around searching" beat).
Once that resolves with nobody found and the boss is still close to the
hidden player, it deliberately walks away (still on the idle Phone animation,
not idle-wandering on the spot) until it's actually put real distance behind
it.

### Pathfinding safety
NavMesh-driven, with a few defensive layers: destinations are always
snapped onto the navmesh before being set; a stuck-detector forces a fresh
path if the agent stalls against geometry; an off-mesh recovery system warps
the boss back onto the navmesh if it ever ends up off it (debounced so a
single bad frame doesn't cause a visible teleport).

### Audio
Three crossfading movement loops (phone/walk/run), a rate-limited scream
one-shot, and a continuous 3D "suspense emitter" that plays only while the
boss is in Walk or Run — real positional audio, so it gets audibly louder the
closer the boss actually is, on top of the boss's own footstep/talking loops.

## 7. Difficulty

Selected on the main menu, applied once at the boss's `Awake()`. The game
itself never changes between difficulties — only how fast, how far, and how
forgiving the boss is.

| Parameter | Easy | Medium *(1x reference)* | Hard |
|---|---|---|---|
| Starting speed multiplier | 0.7x | 1.0x | 1.2x |
| Escalation rate (compounds every 60s) | +20%/min | +12%/min | +20%/min |
| Effective speed at 5 min | ~1.74x | ~1.76x | ~3.0x |
| Sight range | 14m (0.7x) | 20m | 30m (1.5x) |
| Hearing range | 0.7x | 1.0x | 1.5x |
| Tires out after running | 7s | 10s | 14s |
| Tired recovery duration | 7s | 5s | 3s |
| Speed while tired | 0.4x | 0.5x | 0.65x |

Hard's endgame (~3x base speed by the 5-minute mark) is intentionally close
to "can no longer be outrun" — forcing hiding over sprinting in the late
game.

## 8. World & Level Design

The Backrooms space is a 4m-grid maze, originally laid out by an in-editor
procedural generator (`BackroomsMazeGenerator`/`BackroomsPrefabTileGrid`/
`BackroomsPrefabWallGrid`) and then **frozen** (`MapLocked = true`) once the
layout was finished by hand — the generator code is still in the project for
reference but no longer runs.

Hand-placed on top of the generated maze:
- 20 milk cartons, scattered across all four quadrants.
- 20 lockers, snapped flush against real walls with their fronts facing into
  the room (derived from the actual wall thickness and the locker's own mesh
  bounds, not guessed).
- 20 large furniture/decoration props (beds, server racks, tables, shelves)
  for set dressing.
- A handful of water dispensers and a phone charger station.

## 9. UI / HUD

- Runtime-built survival bars for water and phone charge (no manual Editor
  setup required).
- Sequential milk-carton slots (2, 3, 4…) that appear/disappear as the stack
  changes.
- A noise-level indicator that only becomes visible after `revealAfterSeconds`
  (20s) — or instantly, via `ForceReveal()`, at the boss-laugh story beat —
  and always starts from exactly 0 the moment it appears.
- Full-screen end states for Caught, Dehydrated, and Survived.

## 10. Pacing & Dialogue

`IntroDialogueSequencer` runs a reusable click-through dialogue panel (E to
advance) for both the opening monologue and later story interrupts. While a
"chapter" is active, it fully freezes the game: stats, noise, interaction,
movement, *and* camera look (so the view doesn't keep spinning while you're
supposedly paused reading text) — everything resumes the instant the last
line is dismissed.

Twenty seconds after the intro ends, `BossLaughEvent` plays a one-off spatial
laugh from the boss's spawn point, wakes the boss up (`BossAI.Activate()`),
and forces a short two-line interrupt dialogue — the moment the player
"hears" the boss is the same moment it actually becomes active, so they can
never drift out of sync regardless of how long the player takes to click
through the intro.

`GameTimer` is a pausable stopwatch — it's what `GameEndManager` checks for
the win condition, and what `BossAI`'s difficulty escalation is keyed to. It
explicitly pauses during dialogue chapters so reading text never eats into
either.

## 11. Main Menu & Meta

The main menu collects a player name and a difficulty (Easy/Medium/Hard),
saving both via `GameSessionSettings` (backed by `PlayerPrefs`) before
loading the level. `BackShiftLeaderboardStore` records each run's name,
difficulty, result, and survived time locally — currently a minimal store for
testing the end-to-end flow, intended to back a real leaderboard UI later.

## 12. Controls

| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Sprint | Shift |
| Jump | Space |
| Crouch | C (hold) |
| Interact / Hold-to-use / Hide-Unhide | E |
| Toggle phone | 1 |
| Toggle torch | T |

## 13. Technical Reference

### `Assets/Scripts/Boss/`
| Script | Responsibility |
|---|---|
| `BossAI.cs` | The boss's full state machine, sight/hearing, exhaustion, difficulty scaling, pathing safety, audio |
| `BossEndScreen.cs` | Full-screen Caught/Dehydrated/Survived overlay, self-builds its UI |

### `Assets/Scripts/Player/`
| Script | Responsibility |
|---|---|
| `PlayerStats.cs` | Water/phone drain & refill, faint and distress triggers |
| `PlayerNoiseMeter.cs` | Event-based noise model feeding the boss's hearing |
| `PlayerHiding.cs` | Locker hide/unhide — position/camera lock, physics freeze |
| `PlayerInteractor.cs` | Raycast target detection, tap-E vs hold-E dispatch |
| `PhoneViewmodel.cs` | Held phone model, torch light, charge-aware auto-off |
| `MilkItem.cs` | Stackable milk-carton inventory and drinking |
| `PlayerHUD.cs` | Runtime-built survival bars and milk slots |
| `PlayerWalkAudio.cs` / `PlayerDrinkAudio.cs` / `PlayerChargeAudio.cs` / `PlayerLowWaterPanting.cs` | Movement/action audio loops |
| `IntroDialogueSequencer.cs` | Click-through dialogue panel + full gameplay freeze/unfreeze |
| `BossLaughEvent.cs` | 20-second story beat that wakes the boss and forces an interrupt |
| `GameTimer.cs` | Pausable survival stopwatch |
| `GameEndManager.cs` | Self-installing win/loss watcher |
| `GameSessionSettings.cs` / `PlayerProfile.cs` | Difficulty + player name, persisted via `PlayerPrefs` |
| `BackShiftLeaderboardStore.cs` | Local per-run result log |
| `GamePauseMenu.cs` | Pause menu |

### `Assets/Scripts/Interaction/`
`IInteractable` / `IHoldInteractable` define the two interaction shapes used
throughout. `WaterCoolerInteractable.cs`, `Charger_Interactable.cs`,
`MilkPickupInteractable.cs`, and `HideSpotInteractable.cs` are the
implementations actually placed in the level. `Door_Interactable.cs`,
`VendingMachineInteractable.cs`, and `Test_Interactable.cs` exist as unused
scaffolding for future content — not currently placed anywhere.

### `Assets/Scripts/MainMenu/`
`MainMenu.cs` (name entry + difficulty select), `MenuCameraLock.cs`,
`MenuBackdropParallax.cs`, `FlickeringLight.cs` — self-contained menu scene
dressing.

### `Assets/Scripts/Sound/`
`MusicManager.cs` / `MusicLibrary.cs` / `SoundManager.cs` / `SoundLibrary.cs`
/ `GameAudioSettings.cs` — general-purpose audio/SFX library plumbing,
separate from `GameMusicPlayer.cs` (the actual in-level background music
player, under `Player/`).

### `Assets/Scripts/core/`
`BackroomsMazeGenerator.cs`, `BackroomsPrefabTileGrid.cs`,
`BackroomsPrefabWallGrid.cs`, `BackroomsLightGrid.cs` — the editor-time
procedural level generator. Currently locked (`MapLocked = true`); the level
is hand-frozen, not regenerated at runtime.

## 14. Tone

The horror is workplace-shaped, not supernatural-hero-shaped: the player
isn't equipped to fight anything, just to manage two depleting numbers and
not be seen. The threat (a boss who'd fire you, not kill you, if he found out
what you were doing) is mundane right up until it isn't.
