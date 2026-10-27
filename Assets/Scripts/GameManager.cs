using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeLab2D
{
    /// <summary>
    /// Connects the round rules to Unity: input, sounds, the exit, saving and HUD.
    /// PlayerController2D still handles movement and reports collisions here.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GameManager : MonoBehaviour
    {
        [Header("Medium difficulty rules (Easy/Hard adjust health and time)")]
        [Min(1)] public int requiredCells = 10;
        [Min(1)] public int startingHealth = 3;
        [Min(1f)] public float gameSeconds = 180f;

        [Header("Exit: assigned in the saved scene")]
        public SpriteRenderer exitRenderer;
        public Sprite unlockedExitSprite;

        [Header("Level 3: four different sound effects")]
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private AudioClip damageSound;
        [SerializeField] private AudioClip unlockSound;
        [SerializeField] private AudioClip winSound;

        private const string BestScoreKey = "EscapeLab2D.BestScore";
        private const string DifficultyKey = "EscapeLab2D.Difficulty";
        private GameSession session;
        private GameHud hud;
        private AudioSource sounds;
        private bool finishHandled;

        // Keeping this API lets the existing player and pickup scripts keep working.
        public bool Playing => session != null && session.CanAct;
        public bool Paused => session != null && session.Paused;
        public bool Finished => session != null && !session.Playing;
        public int Cells => session == null ? 0 : session.Cells;
        public int SpecialCells => session == null ? 0 : session.SpecialCells;
        public int CollectedCells => session == null ? 0 : session.CollectedCells;
        public int Health => session == null ? 0 : session.Health;
        public int MaxHealth => session == null ? 0 : session.MaxHealth;
        public int Score => session == null ? 0 : session.Score;
        public int BestScore { get; private set; }
        public float SecondsLeft => session == null ? 0f : session.SecondsLeft;
        public float CollectionProgress => session == null ? 0f : session.CollectionProgress;
        public bool ExitOpen => session != null && session.ExitOpen;
        public Difficulty CurrentDifficulty { get; private set; }

        public string ResultText
        {
            get
            {
                if (session == null) return "";
                switch (session.Result)
                {
                    case GameResult.Won: return "YOU ESCAPED!";
                    case GameResult.NoHealth: return "NO HEALTH LEFT!";
                    case GameResult.TimeUp: return "TIME UP!";
                    default: return "";
                }
            }
        }

        private void Awake()
        {
            // A previous paused/finished scene must not freeze a fresh round.
            Time.timeScale = 1f;
            AudioListener.pause = false;
            CurrentDifficulty = (Difficulty)Mathf.Clamp(
                PlayerPrefs.GetInt(DifficultyKey, (int)Difficulty.Medium), 0, 2);
            session = GameSession.Create(CurrentDifficulty, requiredCells, startingHealth, gameSeconds);
            BestScore = Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0));
            hud = new GameHud();
            sounds = GetComponent<AudioSource>();
            if (sounds == null) sounds = gameObject.AddComponent<AudioSource>();
            sounds.playOnAwake = false;
            sounds.spatialBlend = 0f;
            sounds.volume = 0.65f;
        }

        private void Update()
        {
            // Update still runs when timeScale is zero, so P can also unpause.
            if (Input.GetKeyDown(KeyCode.P)) TogglePause();
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartRound();
                return;
            }
            session.Tick(Time.deltaTime);
            HandleFinishedRound();
        }

        public void TogglePause()
        {
            if (!session.Playing) return;
            session.TogglePause();
            Time.timeScale = session.Paused ? 0f : 1f;
            AudioListener.pause = session.Paused;
        }

        public void RestartRound()
        {
            SaveBestScore();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void SelectDifficulty(Difficulty difficulty)
        {
            PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
            PlayerPrefs.Save();
            // Reloading also restores collected objects and the locked exit.
            RestartRound();
        }

        public GameSession PreviewDifficulty(Difficulty difficulty)
        {
            return GameSession.Create(difficulty, requiredCells, startingHealth, gameSeconds);
        }

        public void CollectCell() { Collect(false); }
        public void CollectSpecialCell() { Collect(true); }

        private void Collect(bool special)
        {
            if (!Playing) return;
            bool wasOpen = session.ExitOpen;
            session.CollectCell(special);
            PlaySound(collectSound);
            SaveBestScore();

            // Unlock once, when the goal is first reached, including special cells.
            if (!wasOpen && session.ExitOpen)
            {
                if (exitRenderer != null)
                {
                    if (unlockedExitSprite != null) exitRenderer.sprite = unlockedExitSprite;
                    else exitRenderer.color = Color.green;
                }
                PlaySound(unlockSound);
            }
        }

        public void CollectPotion()
        {
            if (!Playing) return;
            session.Heal();
            PlaySound(collectSound);
        }

        public void CollectPoison() { Damage(2); }
        public void HitPlayer() { Damage(1); }

        private void Damage(int amount)
        {
            if (!Playing) return;
            session.TakeDamage(amount);
            PlaySound(damageSound);
            HandleFinishedRound();
        }

        public void TouchExit()
        {
            session.TryEscape();
            HandleFinishedRound();
        }

        private void HandleFinishedRound()
        {
            if (session.Playing || finishHandled) return;
            finishHandled = true;
            SaveBestScore();
            Time.timeScale = 0f;
            AudioListener.pause = false;
            if (session.Result == GameResult.Won) PlaySound(winSound);
            Debug.Log($"Escape Lab | {ResultText} | {CurrentDifficulty} | " +
                $"Cells: {CollectedCells} | Score: {Score} | Best: {BestScore}");
        }

        private void SaveBestScore()
        {
            if (Score <= BestScore) return;
            BestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            // Save on improvement so leaving halfway through a run retains it.
            PlayerPrefs.Save();
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null) sounds.PlayOneShot(clip);
        }

        private void OnGUI()
        {
            if (hud != null) hud.Draw(this);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (hud != null) hud.Dispose();
        }
    }
}
