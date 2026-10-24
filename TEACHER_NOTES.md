# Teacher notes | Escape Lab minimal 2D foundation

## Scope and rationale

Matches the project objectives: reusable project, four central entities (Player, Hunters, collectables, exit), short repeated runs, a collection goal, timer and health loss, and clear boundaries between movement/input, AI and game rules.

**Deliberate reduction:** Earlier version used eight gameplay scripts. This teaching version uses **three**. The Hunters use simple steering; walls/collectables/exit work through Unity's standard components. OnGUI avoids an introductory Canvas lesson.

## Suggested class explanation

PlayerController2D senses keyboard input and overlap events, moves the player, then reports events to GameManager. GameManager evaluates rules and updates the displayed values. The scene supplies objects, tags and 2D colliders. This matches the slide concept of a modular game but prioritises few scripts over maximal separation.

## Three required playtests

1. **Win:** set Required Cells to 3 for a quick test, collect three cells, then enter the exit.
2. **Health loss:** touch a moving Hunter and observe the health loss rate.
3. **Timeout:** set Game Seconds to 5 and wait.

Restore settings to 10 required cells, 3 health and 180 seconds before distributing further. Check that R restarts after each outcome.

## Extension

Extend HunterMovement with obstacle avoidance or pathfinding. Keep player movement and game rules in their existing scripts.

## Reliability notes

This distribution was statically checked for saved scene objects, script references, sprite references, tags and collider definitions. It has not been compiled or played in a live Unity Editor here. Any Unity Console errors should be resolved before using the package with students.
