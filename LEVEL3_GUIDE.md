# Level 3 — Build Game Features

This completes tasks 10–14 in WK1-Escape_Lab_2D_Student_Improvement_Tasks.pdf.
The starting point is the fork's saved laptop-unity-work branch (be6ab8b), so
the earlier sprint, special cells, pickups, maze and hunters remain available.

## Open and use

1. Extract the project ZIP into a NEW folder.
2. In Unity Hub, add that folder as a project. It contains Assets, Packages and ProjectSettings.
3. Use the project's Unity 6000.6.3f1 version, or the compatible 6000.6 patch already installed.
4. Open Assets/Scenes/EscapeLab2D.unity and press Play.
5. Collect energy cells, avoid hunters and use the unlocked exit.

Controls: WASD/arrows move, Left Shift sprints, P pauses/resumes, R restarts.
Pause or finish a round to see the Easy, Medium and Hard buttons.
Selecting a difficulty starts a fresh round and saves that choice.

The HUD uses a compact layout. Keyboard hints sit beneath it rather than at the
bottom edge, so a zoomed fixed-resolution Game view does not crop the hints away.

## What each task adds

| Task | Implemented behaviour |
|---|---|
| 10: sounds | Four assigned WAV clips for collection, damage, the first exit unlock, and winning. |
| 11: HUD | Filled/empty heart icons, a collection progress bar, score, best score, timer and exit status. |
| 12: pause | P or the pause button freezes movement, damage, collection, the timer and playing sounds. P/Resume resumes the same round. |
| 13: difficulty | Medium uses the Game Manager's existing health/time settings. Easy adds two hearts and gives 4/3 of the time; Hard removes one heart (minimum one) and gives 2/3 of the time. |
| 14: best score | The best score appears in the HUD and menus and is saved whenever it improves, including before a run finishes. |

The required collection count stays the same across difficulties, so a mode never
requires more cells than the level supplies. Both normal and special cells fill
the progress bar. Normal cells score 100; special cells score 200, as before.
Health pickups can still add an extra heart.

The best score and selected difficulty use PlayerPrefs. They persist across
restarts and application sessions on the same machine; Git does not sync them
between the laptop and PC.

## Understand the code

- **GameSession.cs** holds the rules and current round state. CollectCell changes
  the counts, TakeDamage changes health, Tick advances time, and TryEscape checks
  the win condition. CanAct is false while paused or after the round ends.
- **GameManager.cs** connects those rules to Unity. It handles P/R, plays sounds,
  updates the exit sprite, saves PlayerPrefs and reloads the scene. The existing
  player methods still call CollectCell, CollectSpecialCell, CollectPotion,
  CollectPoison, HitPlayer and TouchExit.
- **GameHud.cs** draws the heart icons, bar and menus using the project's existing
  IMGUI approach. It does not require a Canvas, a UI download or manual setup.
- **PlayerController2D.cs** has a movement guard for paused/finished rounds and
  clears the sprint flag during pause, preventing a stuck sprint after resuming.

The four sound files are original synthesized tones under Assets/Audio/Level3,
already assigned to GAME MANAGER. The saved scene also has an AudioSource and a
camera AudioListener. To replace a sound, drag another AudioClip into the matching
Game Manager field. The built-in audio module is enabled; no paid AI service is used.

## Check in Unity before submitting

- [ ] Hear a collection sound and see the bar advance for normal and special cells.
- [ ] Touch a hunter or poison: hear damage and see the heart count decrease.
- [ ] Reach the collection goal: hear the unlock cue once and see EXIT OPEN.
- [ ] Touch the unlocked exit: hear the win cue once and see YOU ESCAPED.
- [ ] Press P while moving. Wait several seconds: timer, hunter movement and damage
      stay frozen. Resume with P and with the menu button.
- [ ] Release Shift while paused. Resume and check sprint is no longer latched.
- [ ] Switch among all three difficulties from the pause menu. Each starts a fresh
      round with restored cells, the chosen health/time settings and a locked exit.
- [ ] Restart while paused and after winning/losing: time and movement work again.
- [ ] Set a score, stop Play mode and start again: the best score remains.
- [ ] Exit Play mode while paused: the next Play session runs normally.

## Automated checks and limits

The included Tools/Level3Checks project has 21 checks covering the real rule code
and GameManager using test-only stand-ins for Unity services. It also compiles the
HUD code against those stand-ins. Layout checks cover six common resolutions and
control visibility when a zoomed QHD Game view crops the bottom. Run with a .NET 8 SDK:

    dotnet run --project Tools/Level3Checks/Level3Checks.csproj

The checks passed in the cloud workspace. Asset/scene reference checks also passed.
The cloud workspace has no Unity Editor, so actual Unity compilation, rendering,
audio playback, physics and Play-mode behaviour have NOT been verified here.
The check project is outside Assets and does not become part of the game.

The original Level 3 delivery did not rewrite Hunter steering. Subsequent work
on Thomas's assigned steering tasks is described in STEERING_TASKS.md.
