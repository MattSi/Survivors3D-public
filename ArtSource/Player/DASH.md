# Short Dash

A basic ability available with every starting weapon. It's free and doesn't take one of the three weapon slots.

## Controls and rules

- Space on the keyboard, or the left shoulder button LB / L1 on a gamepad. Each press triggers one dash; holding the button doesn't dash repeatedly, and presses during cooldown aren't buffered.
- Dashes along the current movement direction. With no movement input, it uses the last actual world-space movement direction; if you haven't moved yet, it defaults to forward.
  In third person it follows the camera-relative movement direction; turning the camera during a dash doesn't change the direction already chosen.
- Distance 2.5 m, duration 0.15 s, cooldown 6 s. Speed items and diagonal input don't increase dash distance.
  The cooldown starts when the dash triggers and doesn't advance while paused, in the codex, or when the window loses focus; it resets to ready at the start of each wave.
- It uses CharacterController sweep movement, so walls, obstacles, solid stone-armored enemies, and the arena boundary end the dash early.
  The dash is not a teleport; long frames don't extend the distance or pass through thin walls.
- The player is immune to damage during the dash and its final movement frame. Stopping early on a collision ends the protection. Immunity doesn't use up the emergency shield or trigger hurt effects;
  the existing 0.7-second invulnerability timer from damage/shield is kept separately.
- Pausing or opening the codex freezes an unfinished dash, and the remaining movement continues on resume. Holding the button in a menu doesn't trigger a dash by accident on resume or at the start of a run.
  Settlement, defeat, and restart clear the dash state and trails.

## Presentation

The character briefly glows blue-white, and two short trails at its sides show the dash. The HUD element sits to the right of the three weapon slots, with a double-arrow icon,
a Space / LB / L1 hint, the remaining cooldown in seconds, and a recovery bar; it flashes for 0.6 seconds when the cooldown finishes.
Only the gamepad that triggered the dash gets a 0.08-second light rumble; pausing, losing focus, disabling, disconnecting, or destroying stops this feedback.

Code responsibilities: `PlayerController.Dash` handles input, movement, cooldown, and feedback; `PlayerHealth` combines independent immunity sources;
`BattleUI` displays the state read-only through `ICombatView`. `PlayerController` runs before the default combat scripts,
so a dash input on a given frame is processed before enemy damage is resolved.

## Verification

Unity menu `Tools > Survivors > Verify Short Dash`, or batchmode `-executeMethod DashChecks.Run`.
The tests use a virtual keyboard/gamepad and cover input edges, direction, fixed-length movement, cooldown, rumble requests and stopping, actual bone-needle hits,
shield independence, thin walls/stone armor/arena boundary, pause/codex/focus loss, and cleanup at each phase.
The success marker is `DASH_CHECKS_PASSED`. A virtual gamepad can only verify the rumble control logic; the strength on a real gamepad needs actual playtesting.

In-game HUD screenshots: `Dash-Ready.png`, `Dash-Active.png`, `Dash-Cooldown.png`.
