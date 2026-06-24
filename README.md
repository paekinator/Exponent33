# Last Shi(f)t

**Last Shi(f)t** is a first-person survival horror game about hiding from work, accidentally finding the wrong room, and trying to survive until your shift ends.

You are ten minutes from closing. The shop is almost empty, the floor is mostly clean, and you are ready to leave. Then you hear that a large group of customers, maybe thirty people, are about to come in and order food. You do what any exhausted employee might briefly, terribly consider: you try to disappear until they leave.

You duck into a vent to hide, hoping everyone assumes the store is empty. Instead, the vent leads into a locked secret passage below the shop. You thought it might be your boss's private office, the one he never lets anyone enter. It is worse. It opens into a hot, hidden Backrooms-like space under the store.

Your company phone says you are still on shift. It tracks your location, proves you are inside store bounds, and must stay charged until the shift ends. If it dies, it sends a distress signal to your boss.

All you need to do is survive long enough for the customers to leave.

Twenty seconds in, you hear a voice nearby.

It is your boss, on the phone.

You cannot go back upstairs, because the customers might still be there. You cannot let your boss know you found the hidden space. You cannot let your phone die. You cannot pass out from thirst.

You need to last to the end of the shift.

---

## Core Pitch

> Hide in the hidden Backrooms beneath a closing store, keep your water and company phone alive, avoid your boss, and survive until your shift ends.

## Genre

- First-person survival horror
- Liminal workplace horror
- Resource management
- Stealth and sound-based avoidance
- Short replayable escape scenario

## Target Experience

The game should feel tense, absurd, and stressful in a very specific way: the player is not a chosen hero, investigator, or monster hunter. They are an underpaid employee who made one bad decision ten minutes before closing.

The horror comes from:

- being trapped somewhere you were never meant to see
- having simple needs become urgent problems
- being hunted by someone who technically should not know you are there
- trying to avoid consequences that are both supernatural and embarrassingly workplace-related

---

## Current High-Level Structure

### 1. Tutorial Area

The game begins in a safer tutorial space where the player learns the core interactions:

- walking and looking
- sprinting
- jumping and crouching
- interacting with objects
- drinking water
- charging the phone
- using the phone hotbar slot
- understanding sound and boss awareness

The tutorial should teach mechanics naturally through a closing-shift environment before the main incident begins.

### 2. Closing Shift Incident

The player is ten minutes from finishing their shift when they hear a large group of customers is about to enter and order food.

The player panics, tries to hide, and enters a vent.

The vent leads into a secret passage and then into the hidden Backrooms area below the store.

### 3. Backrooms Survival

The player discovers the area is extremely hot. Water drains quickly. Their company phone also drains unusually fast.

The player must stay alive, keep the phone charged, and avoid the boss until time runs out.

### 4. Escape and Win

After the survival timer ends, the boss leaves the Backrooms and returns upstairs. The customers have left. The player sneaks back into the shop, pretends they were cleaning and packing up, and crisis is avoided.

The boss assumes the noises downstairs were rats or something else in the hidden space.

---

## Main Objective

> Survive in the hidden Backrooms until your shift is over.

The intended full narrative timer is **10 minutes**, but the current gameplay prototype may use a shorter target such as **6 minutes** for pacing and testing.

## Win Condition

The player wins if they survive until the timer ends while:

- water is above `0%`
- phone charge is above `0%`
- the boss has not caught them

On success:

> The boss leaves. The customers are gone. You return upstairs and pretend to finish closing.

## Lose Conditions

The player loses if:

- water reaches `0%`
- phone charge reaches `0%`
- the boss catches the player
- the player is exposed at the wrong time or cannot return safely

---

## Core Gameplay Loop

1. Explore the Backrooms.
2. Watch water and phone charge.
3. Drink from dispensers or consume stored water bottles.
4. Charge at charger areas or use battery packs.
5. Avoid making too much sound.
6. Hide or reposition when the boss approaches.
7. Survive until the shift timer ends.

The player is constantly choosing between risk and maintenance:

- Sprinting saves time but makes sound and drains water faster.
- Jumping may be useful but creates noise.
- Charging the phone may force the player into exposed areas.
- Searching for water bottles or battery packs may lead toward the boss.
- Staying still may be safe briefly, but water and phone charge keep draining.

---

## Player Resources

### Water / Thirst

The Backrooms area is extremely hot. The player needs water constantly.

Prototype rules:

- Water starts at `100%`
- Water drains by `1%` per second
- Sprinting increases water drain
- Drinking from a dispenser restores water while interacting
- Water bottles can be collected and stored for later use
- A water bottle should restore around `50%`
- Water reaching `0%` causes failure

### Phone Charge

The phone is a company phone. It proves the player is still on shift and inside store bounds.

Prototype rules:

- Phone charge starts at `100%`
- Phone charge drains by `1%` per second
- Charging stations restore phone charge while interacting
- Battery packs can be collected and used later
- Phone reaching `0%` sends a distress signal to the boss
- Phone reaching `0%` should be treated as a major failure state

The phone is both useful and dangerous. It is proof that the player is still working, but it can also betray them.

---

## Inventory Direction

The inventory should be simple and readable during panic.

Recommended design:

- A small bottom-center hotbar, similar to Roblox-style quick slots
- Slot `1`: Phone
- Slot `2`: Water bottle stack
- Slot `3`: Battery pack stack
- Additional slots can be added later if needed

Items should be usable quickly without opening a large menu.

### Example Items

#### Phone

- Toggled with slot `1`
- Shows phone UI and later messages/objectives
- Can be held or stored

#### Water Bottle

- Picked up from the map
- Stored in inventory
- Used when water is low
- Restores around `50%` water

#### Battery Pack

- Picked up from the map
- Stored in inventory
- Used when phone charge is low
- Restores phone charge instantly

---

## Boss AI Concept

The boss is not just a patrol enemy. The boss is an active pressure system.

The boss technically always has some awareness of the player's area. It should often be nearby, making the player feel hunted even when not directly seen.

### Awareness Rules

The boss reacts to:

- sprinting too much
- jumping
- loud interactions
- phone distress signals
- direct line-of-sight
- suspicious movement or repeated noise

### Behavior States

#### Searching

The boss moves through nearby areas and investigates possible player locations.

#### Investigating Sound

If the player makes a significant noise, the boss walks toward that location.

#### Spotted

If the boss sees the player, it runs toward the player's last seen location.

#### Escalating

As the timer progresses, the boss becomes faster, more sensitive, and more dangerous.

The boss should become more aggressive over time so the final minutes feel much worse than the first minute.

---

## Sound and Stealth

Sound should matter.

Suggested noise levels:

| Action | Noise |
|---|---|
| Walking | Low |
| Crouching | Very low |
| Sprinting | Medium/high |
| Jumping | High |
| Landing | Medium/high |
| Drinking | Low |
| Charging phone | Low/medium |
| Dropping or moving objects | Medium/high |
| Phone distress | Very high |

The boss should not instantly know everything from every sound, but sound should pull it toward the player and increase pressure.

---

## Controls

Suggested controls:

| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Sprint | Shift |
| Jump | Space |
| Crouch | C |
| Interact | E |
| Toggle phone | 1 |
| Use water bottle | 2 |
| Use battery pack | 3 |
| Pause | Esc |

---

## Current Prototype Features

The current Unity prototype includes or is moving toward:

- First-person movement
- Jumping
- Crouching
- Sprinting
- Water drain
- Phone charge drain
- Water dispenser interaction
- Phone charger interaction
- Phone hotbar slot
- Runtime HUD for water and phone charge
- Backrooms level blockout
- Boss AI experiments
- Sound/noise direction for boss awareness

---

## Art and Level Direction

The Backrooms area should feel like a hidden maintenance/business liminal space below the shop:

- office ceiling panels
- harsh or uneven fluorescent lighting
- yellowed walls
- carpet/floor tiles
- vents and service corridors
- locked doors
- pillars and maze-like wall clusters
- occasional water/charging stations
- strange evidence of the boss using the area

The level should be readable enough for gameplay, but confusing enough to feel hostile.

---

## Tone

The tone should balance horror with workplace absurdity.

The player is not saving the world. They are avoiding customers, hiding from their boss, trying not to faint, and hoping their company phone does not betray them before the shift ends.

That contrast is the identity of **Last Shi(f)t**.

---

## Development Notes

Priority systems:

1. Stable first-person movement
2. Water and phone survival loop
3. Inventory hotbar
4. Boss sound detection
5. Boss search/chase behavior
6. Tutorial flow
7. Win/loss state
8. Final pacing, lighting, and performance

Performance priorities:

- Bake or fake lighting where possible
- Avoid excessive real-time lights
- Mark static geometry as static
- Use occlusion culling for the maze
- Use simple colliders for walls and props
- Profile before guessing
