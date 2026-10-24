# Structural validation

- Saved Unity 6 2D scene with orthographic camera.
- Three gameplay C# scripts (PlayerController2D, GameManager and HunterMovement).
- Visible sprites, player Rigidbody2D and 2D colliders are retained.
- Ten active collectable triggers, plus Hunter and Exit triggers, are tagged.
- TagManager declares Cell, Hunter and Exit.
- Scene references the three gameplay script GUIDs; HunterMovement is assigned to both Hunter objects and references the Player.
- No editor-only scripts or runtime-level generator needed.
- Level content, win/loss rules and build scene are preserved.

**Limitation:** Unity imported and compiled the scripts successfully, but hunter movement and wall behavior still need to be verified in Play mode. The student/teacher guides describe the playtests to carry out locally.
