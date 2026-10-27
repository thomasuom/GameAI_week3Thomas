# Level 3 validation

Baseline: laptop-unity-work, commit be6ab8b. Scope: PDF tasks 10–14.

## Passed

- 19 C# checks run against the actual GameSession and GameManager code, with
  test-only Unity service stand-ins. The HUD code also compiled with these
  stand-ins and passed an active/pause/end-state smoke check.
- Behaviour checks cover normal/special-cell scoring, unlock/win conditions,
  health/pickups/poison, timeout, pause/resume, difficulty rules and persistence,
  best-score persistence across new managers, one-time unlock/win cues, and
  restart/cleanup restoring global time/audio state.
- Package manifest and lockfile agree, including the built-in audio module.
- No duplicate asset GUIDs.
- All 19 asset GUID references and four attached script references in the enabled
  build scene resolve.
- All six tags used by gameplay scripts are declared.
- Four mono PCM WAV files have valid data and assigned AudioClip references.
- Exactly one enabled AudioListener and one configured AudioSource are wired to
  the camera and Game Manager.
- Earlier scene objects, player references, gameplay tags and hunter script remain.
- Git whitespace validation passes.

## Limits

Unity Editor is unavailable in this cloud workspace. Test stand-ins do not
validate real Unity serialization, compilation, rendering, sound or physics.
Complete the Play-mode checklist in LEVEL3_GUIDE.md before submission.

The standalone test project lives under Tools, outside Unity's Assets folder.
Its generated bin/obj files and Unity Library/Temp data are excluded from delivery.
