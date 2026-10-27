using System;
using System.Linq;
using System.Reflection;
using EscapeLab2D;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class Program
{
    private static int passed;

    private static void Main()
    {
        Check("normal and special pickups retain different scores", () =>
        {
            var s = new GameSession(3, 3, 180);
            s.CollectCell(); s.CollectCell(true);
            Equal(300, s.Score); Equal(2, s.CollectedCells);
            Near(2f / 3f, s.CollectionProgress);
        });
        Check("locked exit cannot win; reaching the goal alone does not win", () =>
        {
            var s = new GameSession(1, 3, 180);
            s.TryEscape(); True(s.Playing);
            s.CollectCell(); True(s.ExitOpen); True(s.Playing);
            s.TryEscape(); Equal(GameResult.Won, s.Result);
        });
        Check("progress stops at a full bar after extra pickups", () =>
        {
            var s = new GameSession(1, 3, 180);
            s.CollectCell(); s.CollectCell();
            Near(1, s.CollectionProgress); Equal(200, s.Score);
        });
        Check("pause freezes timer, pickups, damage, healing and escape", () =>
        {
            var s = new GameSession(1, 3, 180);
            s.CollectCell(); s.TogglePause();
            s.Tick(50); s.CollectCell(true); s.TakeDamage(20); s.Heal(); s.TryEscape();
            Near(180, s.SecondsLeft); Equal(3, s.Health); Equal(100, s.Score);
            True(s.Playing); True(s.Paused); True(!s.CanAct);
        });
        Check("resume continues the same round", () =>
        {
            var s = new GameSession(3, 3, 180);
            s.Tick(10); s.TogglePause(); s.Tick(100); s.TogglePause(); s.Tick(5);
            Near(165, s.SecondsLeft); True(s.CanAct);
        });
        Check("timeout clamps at zero and cannot be resumed", () =>
        {
            var s = new GameSession(3, 3, 1);
            s.Tick(10); s.TogglePause(); s.Heal(); s.CollectCell();
            Equal(GameResult.TimeUp, s.Result); Near(0, s.SecondsLeft);
            Equal(0, s.Score); True(!s.Paused);
        });
        Check("fatal damage clamps health and prevents post-game changes", () =>
        {
            var s = new GameSession(3, 1, 180);
            s.TakeDamage(2); s.Heal(); s.CollectCell(); s.Tick(10);
            Equal(GameResult.NoHealth, s.Result); Equal(0, s.Health);
            Equal(0, s.Score); Near(180, s.SecondsLeft);
        });
        Check("health pickup restores health and displays any extra heart", () =>
        {
            var s = new GameSession(3, 3, 180);
            s.TakeDamage(); s.Heal(); Equal(3, s.Health);
            s.Heal(); Equal(4, s.Health); Equal(4, s.MaxHealth);
        });
        Check("difficulty changes health/time while preserving the goal", () =>
        {
            var easy = GameSession.Create(Difficulty.Easy, 10, 3, 180);
            var medium = GameSession.Create(Difficulty.Medium, 10, 3, 180);
            var hard = GameSession.Create(Difficulty.Hard, 10, 3, 180);
            Equal(5, easy.Health); Near(240, easy.SecondsLeft);
            Equal(3, medium.Health); Near(180, medium.SecondsLeft);
            Equal(2, hard.Health); Near(120, hard.SecondsLeft);
            Equal(10, easy.RequiredCells); Equal(10, hard.RequiredCells);
        });
        Check("difficulty honours existing custom Inspector values", () =>
        {
            var medium = GameSession.Create(Difficulty.Medium, 7, 5, 100);
            Equal(7, medium.RequiredCells); Equal(5, medium.Health); Near(100, medium.SecondsLeft);
            True(GameSession.Create(Difficulty.Hard, 7, 1, 100).Health >= 1);
        });
        Check("invalid limits and invalid elapsed time do not break the round", () =>
        {
            var s = new GameSession(0, 0, float.NaN);
            Equal(1, s.RequiredCells); Equal(1, s.Health); Near(1, s.SecondsLeft);
            s.Tick(-1); s.Tick(float.NaN); s.Tick(float.PositiveInfinity);
            Near(1, s.SecondsLeft);
        });
        Check("P pauses physics/audio and P resumes without restarting", () =>
        {
            var g = NewManager();
            Input.Down.Add(KeyCode.P); Call(g, "Update"); Input.Down.Clear();
            True(g.Paused); True(!g.Playing); Near(0, Time.timeScale); True(AudioListener.pause);
            Call(g, "Update"); Near(180, g.SecondsLeft);
            Input.Down.Add(KeyCode.P); Call(g, "Update"); Input.Down.Clear();
            True(g.Playing); Near(1, Time.timeScale); True(!AudioListener.pause);
        });
        Check("best score survives a new manager and never decreases", () =>
        {
            var g = NewManager();
            g.CollectCell(); g.CollectSpecialCell();
            Equal(300, PlayerPrefs.GetInt("EscapeLab2D.BestScore"));
            True(PlayerPrefs.Saves > 0);
            var second = NewManager(false);
            Equal(300, second.BestScore);
            second.CollectCell();
            Equal(300, second.BestScore);
            Equal(300, PlayerPrefs.GetInt("EscapeLab2D.BestScore"));
        });
        Check("unlock and win sounds trigger once at their actual events", () =>
        {
            var g = NewManager();
            g.requiredCells = 2; Call(g, "Awake");
            var collect = new AudioClip(); var unlock = new AudioClip(); var win = new AudioClip();
            Set(g, "collectSound", collect); Set(g, "unlockSound", unlock); Set(g, "winSound", win);
            g.exitRenderer = new SpriteRenderer(); g.unlockedExitSprite = new Sprite();
            g.CollectCell(); True(!g.ExitOpen);
            g.CollectSpecialCell(); g.CollectCell();
            True(g.ExitOpen); True(g.exitRenderer.sprite == g.unlockedExitSprite);
            var audio = g.GetComponent<AudioSource>();
            Equal(3, audio.Played.Count(x => x == collect));
            Equal(1, audio.Played.Count(x => x == unlock));
            g.TouchExit(); g.TouchExit();
            Equal(1, audio.Played.Count(x => x == win));
            True(g.Finished); Near(0, Time.timeScale); True(!AudioListener.pause);
        });
        Check("poison uses the damage sound and removes two hearts", () =>
        {
            var g = NewManager(); var damage = new AudioClip(); Set(g, "damageSound", damage);
            g.CollectPoison(); Equal(1, g.Health);
            Equal(1, g.GetComponent<AudioSource>().Played.Count(x => x == damage));
        });
        Check("restart while paused restores time/audio and reloads the scene", () =>
        {
            var g = NewManager(); g.CollectCell(); g.TogglePause(); g.RestartRound();
            Near(1, Time.timeScale); True(!AudioListener.pause);
            Equal("EscapeLab2D", SceneManager.LastLoaded);
            Equal(100, PlayerPrefs.GetInt("EscapeLab2D.BestScore"));
        });
        Check("difficulty selection persists and applies to a fresh round", () =>
        {
            var g = NewManager(); g.TogglePause(); g.SelectDifficulty(Difficulty.Hard);
            var next = NewManager(false);
            Equal(Difficulty.Hard, next.CurrentDifficulty);
            Equal(2, next.Health); Near(120, next.SecondsLeft);
            True(next.Playing); True(!next.ExitOpen); Equal(0, next.Score);
        });
        Check("ending cannot be undone by pause input; disposal restores global state", () =>
        {
            var g = NewManager(); g.HitPlayer(); g.HitPlayer(); g.HitPlayer(); g.TogglePause();
            True(g.Finished); True(!g.Paused); Near(0, Time.timeScale);
            Call(g, "OnDestroy"); Near(1, Time.timeScale); True(!AudioListener.pause);
        });
        Check("HUD code builds and runs through active, pause and end states with test doubles", () =>
        {
            var g = NewManager(); Call(g, "OnGUI"); g.TogglePause(); Call(g, "OnGUI");
            g.TogglePause(); g.HitPlayer(); g.HitPlayer(); g.HitPlayer(); Call(g, "OnGUI");
            Call(g, "OnDestroy");
        });
        Check("HUD stays compact and inside common game resolutions", () =>
        {
            int[,] sizes = { { 960, 540 }, { 1280, 720 }, { 1920, 1080 },
                { 2560, 1440 }, { 3440, 1440 }, { 1024, 768 } };
            for (int i = 0; i < sizes.GetLength(0); i++)
            {
                Screen.width = sizes[i, 0]; Screen.height = sizes[i, 1];
                var g = NewManager(); GUI.Draws.Clear(); Call(g, "OnGUI");
                Rect hud = GUI.Draws[0].Bounds;
                True(hud.width < Screen.width * 0.24f);
                True(hud.height < Screen.height * 0.22f);
                foreach (var draw in GUI.Draws)
                {
                    Rect r = draw.Bounds;
                    True(r.x >= 0 && r.y >= 0);
                    True(r.x + r.width <= Screen.width + 0.01f);
                    True(r.y + r.height <= Screen.height + 0.01f);
                }
                Call(g, "OnDestroy");
            }
        });
        Check("control hints remain visible when a zoomed QHD view crops the bottom", () =>
        {
            Screen.width = 2560; Screen.height = 1440;
            var g = NewManager(); GUI.Draws.Clear(); Call(g, "OnGUI");
            var hints = GUI.Draws.Single(x => x.Text != null && x.Text.Contains("WASD"));
            True(hints.Text.Contains("P: pause/resume") && hints.Text.Contains("R: restart"));
            True(hints.Bounds.y + hints.Bounds.height < Screen.height * 0.85f);
            Call(g, "OnDestroy");
            Screen.width = 1920; Screen.height = 1080;
        });
        Console.WriteLine($"{passed} checks passed. Test doubles do not validate Unity rendering, audio or physics.");
    }

    private static GameManager NewManager(bool reset = true)
    {
        if (reset) PlayerPrefs.ResetForTest();
        Input.Down.Clear(); Time.deltaTime = 0.02f;
        SceneManager.LastLoaded = null;
        var game = new GameManager(); Call(game, "Awake"); return game;
    }
    private static void Call(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
    private static void Check(string name, Action test)
    {
        test(); passed++; Console.WriteLine("PASS: " + name);
    }
    private static void True(bool value) { if (!value) throw new Exception("Expected true"); }
    private static void Equal<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
    }
    private static void Near(float expected, float actual)
    {
        if (Math.Abs(expected-actual) > 0.001f) throw new Exception($"Expected {expected}, got {actual}");
    }
}
