# Thomas's Week 2 steering tasks

Team split: Thomas **1, 4, 7, 10, 13, 16**; teammate B **2, 5, 8, 11, 14, 17**;
teammate C **3, 6, 9, 12, 15**.

Source briefs: WK2-Escape_Lab_2D_Steering.pdf (task numbers) and 2_Steering.pdf
(the Seek/Arrive/Wander and steering explanation). This branch builds on the
working Level 3 project and compact HUD, commit a558bc8.

## Implementation checklist

| Thomas's task | Implementation |
|---|---|
| 01 Basic Seek | BasicSeek mode directly uses direction-to-player times maxSpeed. |
| 04 Arrival | Arrival mode slows inside slowingRadius and stops within stopRadius of the assigned point (or player). |
| 07 Wander & Seek | Choose random world-space points, reject points inside walls, seek each target, and replace reached/stale targets. |
| 10 Obstacle Avoidance | One forward ray detects Walls. Wall-tangent steering takes priority over target seeking. |
| 13 Smooth Patrol | Advance waypoints within waypointRadius and retain velocity through turns. An assignable waypoint list is included. |
| 16 Separation | Nearby active Hunters repel each other; closer neighbours repel more, including exact coincident positions. |

Other tasks are not separate deliverables in this branch. The original detection
switch is retained. Shared acceleration-limited integration supports arrival and
patrol. Task 13 includes the minimum waypoint-list scaffold it needs; the owner of
task 12 can reuse or replace that list instead of creating a competing controller.

No Flee/Rival, three-feeler avoidance, Pursuit, Evade, Surround or A* system is added.
The existing hunter-ai-steering branch is not merged.

## Scene setup already included

- Keep using Assets/Scenes/EscapeLab2D.unity.
- **HUNTER - AI** uses ExistingDetection: the original near-player arrival response,
  with point-based wandering outside detection range.
- **HUNTER 2 - AI** uses SmoothPatrol and four assigned points in the clear left
  side of the arena.
- **STEERING PRACTICE POINTS** contains the Arrival target and patrol points.
- Layer 6 is **Walls**, assigned to the nine solid wall/obstacle objects.
  Hunter queries ignore trigger pickups, player and Hunter colliders.
- Hunter 1 starts at (4.3, 2.2), slightly away from the wall it originally overlapped.
  Art, player controls, score rules, Level 3 sounds, HUD and menus are retained.

Both Hunters still use the existing HunterMovement component and script GUID.
Enable or disable Obstacle Avoidance and Separation in its Inspector to isolate
a behaviour. The passive shape sweep still prevents movement through solid walls.

## Test one task at a time in Unity

### 01 — Basic Seek

Select HUNTER - AI, choose **BasicSeek** under Behaviour and press Play.
Temporarily disable Separation and Obstacle Avoidance to see direct chasing.
The first movement frame immediately uses full speed; smooth Steering Seek is
your teammate's task 03. A solid wall can still block direct chasing.

### 04 — Arrival

Choose **Arrival** on HUNTER - AI. The saved Arrival Target is at (4.3, 0), in clear
space below that Hunter's starting position. Watch it slow near the point and stop.
Move the target object in Scene view to compare long and short approaches.
With the Hunter selected, Gizmos display the slowing and stopping radii.

### 07 — Wander & Seek

Choose **WanderAndSeek**, enable Obstacle Avoidance, and watch several targets.
The selected Hunter's current target is shown by a cyan Gizmo circle.
Wander Bounds is a world-space rectangle. A target is replaced when reached or
after Wander Retarget Seconds, so an unreachable point is not pursued forever.

This is point-based wandering, matching task 07. Your teammate's task 06 can add
the lecture's gently jittered forward-circle Wander separately.

### 10 — One forward obstacle ray

Enable Obstacle Avoidance and put a Hunter on a course toward a solid wall.
In Scene view with Gizmos enabled, inspect the yellow forward ray. The Hunter
should turn along/away from the detected wall, and its shape sweep prevents crossing.
Only one forward sensing ray is used; left/right feelers belong to task 11.
The shape sweep is collision safety, not extra sensing feelers.

This is local avoidance, not navigation. It does not guarantee a route through
every maze or out of enclosed corners. A* belongs to the next navigation stage.

### 13 — Smooth Patrol

HUNTER 2 - AI is already set to **SmoothPatrol** with four waypoints.
Watch the Hunter turn toward the next point before reaching the exact position.
It keeps moving through turns rather than stopping at each point.
Increase Waypoint Radius for earlier turns; decrease it for tighter point tracking.
The demo uses radius 1.6 and acceleration 24 with that Hunter's existing speed 8.

The owner of task 12 can assign their Transforms to Patrol Waypoints, or call:

    hunter.SetPatrolWaypoints(theirWaypoints);

The list loops, skips missing entries and safely handles an empty list.
A single point uses Arrival instead of continually circling that point.

### 16 — Separation

Enable Separation on both active Hunters. Start them near each other in a clear
area, or temporarily give them identical positions. They should spread apart.
Compare Separation Radius and Separation Weight. The calculation only includes
other active HunterMovement components, excludes self, and handles exact overlap.
This keeps Hunters apart; it does not implement task 17's surround/orbit positions.

### Check compatibility

- [ ] Level 3 hearts, collection bar, sounds, difficulty and best score still work.
- [ ] P freezes Hunters and the timer; resuming does not jump their positions.
- [ ] After winning/losing, Hunters stop and R starts a normal fresh run.
- [ ] Enable both Hunters and verify they retain independent targets/waypoint state.
- [ ] Check the Console and inspect the Walls mask if a wall is not detected.
- [ ] Restore the saved demo settings after temporary Play-mode experiments.

## Code walkthrough

- **HunterMovement.cs** connects the six tasks to the scene. Inspector headers and
  method comments identify task numbers. Update chooses a goal, adds separation,
  prioritises wall avoidance and applies movement.
- **SteeringMath.cs** contains small vector calculations: seek, arrival, capped
  steering, radius tests, repulsion and wall-tangent direction.
- **ChooseWanderTarget** samples clear points inside bounds.
- **PatrolVelocity** changes the point index while within the waypoint radius.
- **AvoidObstacles** uses one ray and briefly keeps a chosen turn to avoid flipping.
- **MoveWithoutCrossingWalls** retains collision safety, adds clearance, uses the
  actual collider centre, and allows parallel movement at zero-distance contact.

## Verification

32 standalone C# checks passed against the real helper and controller code with
test-only Unity/physics stand-ins. They cover direct seek, arrival simulation,
wandering bounds/retries, one-ray priority, contact sliding, full-distance sweeps,
multi-lap patrol, separation, active registration and Level 3 pause gating.

Hunters use local registration numbers for overlap tie-breaking and turn choice.
There is no dependency on Unity's obsolete GetInstanceID API. The test stand-in
rejects that API as a compilation error, matching the project's Unity version.

Run with a .NET 8 SDK:

    dotnet run --project Tools/SteeringChecks/SteeringChecks.csproj

The Level 3 checks remain available separately:

    dotnet run --project Tools/Level3Checks/Level3Checks.csproj

Saved scene references, waypoint links, wall masks and spawn clearance are checked
structurally. Unity Editor is unavailable in the cloud workspace. Real Unity
compilation, visual behaviour and physics must still be checked in Play mode.
Group integration into the original team repository and the final milestone tag
remain team activities after everyone reviews/tests their assigned work.
