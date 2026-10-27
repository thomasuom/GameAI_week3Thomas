using UnityEngine;

namespace EscapeLab2D
{
    /// <summary>Draws hearts, collection progress and menus using the existing IMGUI system.</summary>
    public sealed class GameHud
    {
        private Texture2D heart;
        private GUIStyle title;
        private GUIStyle label;
        private GUIStyle small;
        private GUIStyle button;
        private readonly Color panel = new Color(0.035f, 0.06f, 0.12f, 0.95f);
        private readonly Color accent = new Color(0.2f, 0.85f, 0.7f);

        public void Draw(GameManager game)
        {
            if (title == null) CreateStyles();
            if (heart == null) CreateHeart();
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            if (scale <= 0f) return;

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            GUI.color = Color.white;

            Fill(new Rect(16, 16, 344, 192), panel);
            GUI.Label(new Rect(32, 25, 310, 28), "ESCAPE LAB 2D", title);
            DrawHearts(game);
            GUI.Label(new Rect(32, 105, 300, 24),
                $"Score  {game.Score}       Best  {game.BestScore}", label);
            GUI.Label(new Rect(32, 133, 300, 22),
                $"Energy cells  {game.CollectedCells}/{game.requiredCells}", small);
            Fill(new Rect(32, 159, 312, 14), new Color(0.16f, 0.2f, 0.28f));
            Fill(new Rect(34, 161, 308 * game.CollectionProgress, 10), accent);
            GUI.Label(new Rect(32, 181, 312, 22),
                $"{game.CurrentDifficulty}  |  {Mathf.CeilToInt(game.SecondsLeft)}s  |  " +
                (game.ExitOpen ? "EXIT OPEN" : "EXIT LOCKED"), small);

            if (!game.Finished && GUI.Button(new Rect(width - 146, 16, 130, 34),
                game.Paused ? "Resume [P]" : "Pause [P]", button)) game.TogglePause();

            Fill(new Rect(0, height - 34, width, 34), panel);
            GUI.Label(new Rect(16, height - 29, width - 32, 25),
                "WASD / arrows: move   |   Shift: sprint   |   P: pause   |   R: restart", small);

            if (game.Paused || game.Finished) DrawMenu(game, width, height);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.enabled = oldEnabled;
        }

        private void DrawHearts(GameManager game)
        {
            // Draw actual icons, so a missing heart character in a font cannot break the HUD.
            int visible = Mathf.Min(game.MaxHealth, 10);
            for (int i = 0; i < visible; i++)
            {
                GUI.color = i < game.Health ? new Color(1f, 0.3f, 0.4f) : new Color(0.25f, 0.28f, 0.35f);
                GUI.DrawTexture(new Rect(32 + i * 27, 60, 24, 22), heart);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(32, 84, 300, 20), $"Health  {game.Health}/{game.MaxHealth}", small);
        }

        private void DrawMenu(GameManager game, float width, float height)
        {
            bool paused = game.Paused;
            Fill(new Rect(0, 0, width, height), new Color(0f, 0f, 0f, 0.6f));
            Rect box = new Rect((width - 440) / 2f, (height - 360) / 2f, 440, 360);
            Fill(box, panel);
            GUI.Label(new Rect(box.x + 24, box.y + 18, 392, 36),
                paused ? "PAUSED" : game.ResultText, title);
            GUI.Label(new Rect(box.x + 24, box.y + 59, 392, 26),
                $"Score {game.Score}    |    Best {game.BestScore}", label);

            float y = box.y + 99;
            if (paused && GUI.Button(new Rect(box.x + 24, y, 188, 36), "Resume [P]", button))
                game.TogglePause();
            if (GUI.Button(new Rect(box.x + (paused ? 228 : 24), y,
                paused ? 188 : 392, 36), "Restart [R]", button)) game.RestartRound();

            GUI.Label(new Rect(box.x + 24, y + 48, 392, 24),
                "Difficulty - selecting starts a fresh round", small);
            Difficulty[] choices = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
            for (int i = 0; i < choices.Length; i++)
            {
                Difficulty choice = choices[i];
                GameSession preview = game.PreviewDifficulty(choice);
                string current = choice == game.CurrentDifficulty ? " (current)" : "";
                string text = $"{choice}{current}  |  {preview.Health} hearts  |  " +
                    $"{Mathf.CeilToInt(preview.SecondsLeft)} seconds";
                if (GUI.Button(new Rect(box.x + 24, y + 80 + i * 43, 392, 35), text, button))
                    game.SelectDifficulty(choice);
            }
            GUI.Label(new Rect(box.x + 24, box.y + 322, 392, 24),
                "The collection goal is unchanged on every difficulty.", small);
        }

        private void CreateStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = 17 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            title.normal.textColor = label.normal.textColor = small.normal.textColor = Color.white;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void CreateHeart()
        {
            string[] pixels =
            {
                "  ###   ###  ", " ##### ##### ", "#############", "#############",
                "#############", " ########### ", "  #########  ", "   #######   ",
                "    #####    ", "     ###     ", "      #      "
            };
            heart = new Texture2D(13, 11, TextureFormat.RGBA32, false);
            heart.filterMode = FilterMode.Point;
            for (int y = 0; y < pixels.Length; y++)
                for (int x = 0; x < pixels[y].Length; x++)
                    heart.SetPixel(x, pixels.Length - 1 - y, pixels[y][x] == '#' ? Color.white : Color.clear);
            heart.Apply();
        }

        public void Dispose()
        {
            if (heart != null) Object.Destroy(heart);
        }
    }
}
