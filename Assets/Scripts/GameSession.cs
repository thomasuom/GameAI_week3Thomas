using System;

namespace EscapeLab2D
{
    public enum Difficulty { Easy, Medium, Hard }
    public enum GameResult { Playing, Won, NoHealth, TimeUp }

    /// <summary>The rules of one round, independent of Unity's input, audio and drawing.</summary>
    public sealed class GameSession
    {
        public Difficulty Difficulty { get; private set; }
        public int RequiredCells { get; private set; }
        public int Cells { get; private set; }
        public int SpecialCells { get; private set; }
        public int CollectedCells => Cells + SpecialCells;
        public int Score => Cells * 100 + SpecialCells * 200;
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public float SecondsLeft { get; private set; }
        public bool Paused { get; private set; }
        public GameResult Result { get; private set; } = GameResult.Playing;
        public bool Playing => Result == GameResult.Playing;
        public bool CanAct => Playing && !Paused;
        public bool ExitOpen => CollectedCells >= RequiredCells;
        public float CollectionProgress => Math.Min(1f, (float)CollectedCells / RequiredCells);

        public GameSession(int requiredCells, int health, float seconds,
            Difficulty difficulty = Difficulty.Medium)
        {
            RequiredCells = Math.Max(1, requiredCells);
            Health = MaxHealth = Math.Max(1, health);
            SecondsLeft = float.IsNaN(seconds) || float.IsInfinity(seconds)
                ? 1f : Math.Max(1f, seconds);
            Difficulty = difficulty;
        }

        // Existing Inspector values define Medium. The collection goal stays achievable.
        public static GameSession Create(Difficulty difficulty, int cells, int health, float seconds)
        {
            health = Math.Max(1, health);
            if (difficulty == Difficulty.Easy)
            {
                health += 2;
                seconds *= 4f / 3f;
            }
            else if (difficulty == Difficulty.Hard)
            {
                health = Math.Max(1, health - 1);
                seconds *= 2f / 3f;
            }
            return new GameSession(cells, health, seconds, difficulty);
        }

        public void Tick(float elapsedSeconds)
        {
            if (!CanAct || elapsedSeconds <= 0f || float.IsNaN(elapsedSeconds)
                || float.IsInfinity(elapsedSeconds)) return;
            SecondsLeft = Math.Max(0f, SecondsLeft - elapsedSeconds);
            if (SecondsLeft == 0f) Result = GameResult.TimeUp;
        }

        public void TogglePause()
        {
            if (Playing) Paused = !Paused;
        }

        public void CollectCell(bool special = false)
        {
            if (!CanAct) return;
            if (special) SpecialCells++;
            else Cells++;
        }

        public void TakeDamage(int amount = 1)
        {
            if (!CanAct || amount <= 0) return;
            Health = Math.Max(0, Health - amount);
            if (Health == 0) Result = GameResult.NoHealth;
        }

        public void Heal()
        {
            if (!CanAct) return;
            Health++;
            // Preserve the existing pickup behaviour, including extra hearts.
            MaxHealth = Math.Max(MaxHealth, Health);
        }

        public void TryEscape()
        {
            if (CanAct && ExitOpen) Result = GameResult.Won;
        }
    }
}
