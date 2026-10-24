# Student guide | Escape Lab 2D | Week 1

## The whole game in one minute

You control the blue player. Collect ten gold cells, avoid the red Hunter, and reach the exit. Collecting all ten cells changes the exit to green. The game ends if you escape, lose all three health, or run out of the three-minute timer.

This is **a 2D game**, not a 3D game. The X axis means left/right. The Y axis means up/down. The Z axis is not used for movement.

## What you should understand

There are **three gameplay scripts**:

1. **PlayerController2D.cs** moves the player and detects what the player touches.
2. **GameManager.cs** stores the rules and determines whether the player has won or lost.
3. **HunterMovement.cs** steers the Hunters toward the player nearby and lets them wander farther away.

The walls, collectables and exit use components already included in Unity rather than their own scripts.

## PlayerController2D.cs: read the code in this order

| Method | Plain-English meaning |
|---|---|
| `Start()` | Find the player's Rigidbody2D and the GameManager once. |
| `Update()` | Check which movement keys are being held down. |
| `FixedUpdate()` | Move the Rigidbody2D using Unity's physics clock. |
| `OnTriggerEnter2D()` | When you first touch a gold cell, tell the manager and hide that cell. |
| `OnTriggerStay2D()` | While touching the Hunter, lose health at a controlled rate; while touching the exit, check if you can escape. |

## HunterMovement.cs

The Hunter moves in the scene's XY plane. It wanders when the player is outside its detection radius and uses an arrive behavior to slow down near the player. Before moving, it casts against non-trigger colliders so it cannot cross walls.

**Important terms:**

- `Vector2` is a pair of numbers: X and Y.
- `Time.fixedDeltaTime` is the duration of one physics step.
- `normalized` keeps diagonal movement at the same speed as straight movement.
- `Collider2D` is the shape used for collision detection.
- **Trigger** colliders detect overlaps without acting as solid walls.
- **Tag** is a text label attached to a scene object: `Cell`, `Hunter` or `Exit`.

## GameManager.cs: read the code in this order

| Method | Plain-English meaning |
|---|---|
| `Start()` | Set the starting health and timer. |
| `Update()` | Count down time and check whether R should restart the game. |
| `CollectCell()` | Add a cell to the count and unlock the exit when the target is met. |
| `HitPlayer()` | Subtract health and check for defeat. |
| `TouchExit()` | Win, but only if enough cells have been collected. |
| `EndGame()` | Stop the game and log its final statistics. |
| `OnGUI()` | Show cells, health, score, time and the end-of-game message. |

`GameManager` does **not** read player movement keys. Keeping responsibilities apart is the important design habit you will use when adding AI later.

## Suggested 2-hour practical

| Time | Task |
|---|---|
| 0-15 min | Open the scene. Identify the player, Hunter, collectables, walls and exit. |
| 15-35 min | Read `PlayerController2D.cs` and trace what happens when W is pressed. |
| 35-55 min | Read `GameManager.cs` and trace what happens after collecting a cell. |
| 55-75 min | Play the game. Win once. Identify the win condition. |
| 75-90 min | Observe the Hunter chase and navigate along walls. Touch it to observe health loss, then lose by time running out. |
| 90-105 min | Change `Required Cells` in the Inspector to 3. Test again. |
| 105-120 min | Explain how the Hunter's steering and wall checks work together. |

## Three experiments (no new scripts)

1. Select the Player and change `Move Speed` from 6 to 3. What changes?
2. Select the Game Manager and change `Game Seconds` from 180 to 30. How does the game feel?
3. In Scene view, move one gold energy cell to a different reachable area. Will the player still be able to collect it?

## Milestone checklist

- [ ] I can move with WASD or the arrow keys.
- [ ] I cannot move through walls.
- [ ] Gold cells disappear when touched.
- [ ] Collecting cells updates the count and score.
- [ ] I can only escape after the exit unlocks.
- [ ] The Hunters pursue me nearby and do not pass through walls.
- [ ] The Hunter reduces health on contact.
- [ ] The timer can cause a loss.
- [ ] Pressing R after a game ends restarts a fresh run.
- [ ] I can explain which script handles input and which script handles rules.

**Reflection:** Why might the Hunter movement script avoid storing the score and timer itself?
