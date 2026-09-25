using System.Numerics;
using Lumis;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            bool smoke = false;
            bool audio = true;
            string? capture = null;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--smoke": smoke = true; break;
                    case "--no-audio": audio = false; break;
                    case "--capture" when i + 1 < args.Length: capture = args[++i]; break;
                    default: throw new ArgumentException($"Unknown or incomplete argument: {args[i]}");
                }
            }
            if (capture is not null && !smoke)
                throw new ArgumentException("Use --capture with --smoke.");

            using var game = new HelloGame(audio, smoke, capture);
            game.Run();
            if (smoke)
            {
                game.VerifyShutdown();
                Console.WriteLine($"SMOKE PASS: graphics, texture, input polling, scenes, cleanup; audio={(audio ? "enabled" : "disabled")}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}

internal sealed class HelloGame(bool enableAudio, bool smoke, string? capture) : LumisGame(new GameSettings
{
    Title = "HelloLumis | LumisAPI 0.1.0",
    EnableAudio = enableAudio,
    Width = 960,
    Height = 600
})
{
    private DemoScene demo = null!;
    private InfoScene info = null!;
    private int frame;
    private int draws;
    private bool unloaded;
    private bool musicStarted;
    internal Texture Sprite { get; private set; } = null!;
    internal SoundClip? Chime { get; private set; }
    internal MusicTrack? Music { get; private set; }
    internal int SceneEntries { get; set; }
    internal static Color Accent => new(109, 226, 214);

    protected override void OnLoad()
    {
        string assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        Sprite = Graphics.LoadTexture(Path.Combine(assets, "lumis.png"));
        Check(Sprite.Width == 64 && Sprite.Height == 64, "Texture dimensions");
        if (Audio.IsAvailable)
        {
            Audio.MasterVolume = 0.35f;
            Chime = Audio.LoadSound(Path.Combine(assets, "chime.wav"));
            Music = Audio.LoadMusic(Path.Combine(assets, "loop.wav"));
            Music.Volume = 0.35f;
            Music.IsLooping = true;
            Check(Music.Duration.TotalSeconds > 1, "Music duration");
        }
        demo = new DemoScene(this);
        info = new InfoScene(this);
        Scenes.Switch(demo);
    }

    protected override void Update(float deltaTime)
    {
        frame++;
        if (Input.IsKeyPressed(Key.Escape)) Exit();
        if (Input.IsKeyPressed(Key.Tab)) ToggleScene();
        if (Input.IsKeyPressed(Key.Space)) Chime?.Play();
        if (Input.IsKeyPressed(Key.M) && Music is not null)
        {
            if (Music.IsPlaying) Music.Pause();
            else if (musicStarted) Music.Resume();
            else { Music.Play(); musicStarted = true; }
        }

        if (!smoke) return;
        // Exercise input queries against the native window even without human input.
        _ = Input.MousePosition;
        _ = Input.MouseWheelDelta;
        _ = Input.IsKeyReleased(Key.Space);
        _ = Input.IsMouseButtonReleased(MouseButton.Left);
        if (frame == 5 && Music is not null && Chime is not null)
        {
            Chime.Volume = 0.5f;
            Chime.Play();
            Check(Chime.IsPlaying, "Sound starts");
            Chime.Pause();
            Check(!Chime.IsPlaying, "Sound pauses");
            Chime.Resume();
            Check(Chime.IsPlaying, "Sound resumes");
            Music.Play();
            Check(Music.IsPlaying, "Music starts");
        }
        if (frame == 15 && Music is not null && Chime is not null)
        {
            Chime.Stop();
            Check(!Chime.IsPlaying, "Sound stops");
            Music.Pause();
            Check(!Music.IsPlaying, "Music pauses");
        }
        if (frame == 20 && Music is not null)
        {
            Music.Resume();
            Check(Music.IsPlaying, "Music resumes");
        }
        if (frame is 30 or 45) ToggleScene();
        if (frame == 60)
        {
            Check(SceneEntries == 3, "Two deferred scene transitions");
            Check(draws >= 59, "Draw callbacks completed");
            if (Music is not null)
            {
                Check(Music.Position > TimeSpan.Zero, "Music streaming progresses");
                Music.Stop();
                Check(!Music.IsPlaying, "Music stops");
            }
            Exit();
        }
    }

    protected override void Draw() => draws++;

    protected override void OnUnload() => unloaded = true;

    internal void ToggleScene() => Scenes.Switch(ReferenceEquals(Scenes.Current, demo) ? info : demo);

    internal void CaptureIfRequested()
    {
        if (smoke && frame == 59 && capture is not null) Graphics.CaptureScreenshot(capture);
    }

    internal void VerifyShutdown()
    {
        Check(unloaded && Sprite.IsDisposed && !Audio.IsAvailable, "Automatic shutdown");
        // Disposing a resource a second time after the window closes must be safe.
        Sprite.Dispose();
        Chime?.Dispose();
        Music?.Dispose();
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException($"Smoke assertion failed: {name}");
    }
}

internal sealed class DemoScene(HelloGame game) : LumisScene
{
    private Vector2 position = new(450, 310);
    private float elapsed;

    public override void OnEnter() => game.SceneEntries++;

    public override void Update(float deltaTime)
    {
        elapsed += deltaTime;
        Vector2 movement = Vector2.Zero;
        if (game.Input.IsKeyDown(Key.A) || game.Input.IsKeyDown(Key.Left)) movement.X--;
        if (game.Input.IsKeyDown(Key.D) || game.Input.IsKeyDown(Key.Right)) movement.X++;
        if (game.Input.IsKeyDown(Key.W) || game.Input.IsKeyDown(Key.Up)) movement.Y--;
        if (game.Input.IsKeyDown(Key.S) || game.Input.IsKeyDown(Key.Down)) movement.Y++;
        if (movement != Vector2.Zero) position += Vector2.Normalize(movement) * 240 * deltaTime;
        if (game.Input.IsMouseButtonPressed(MouseButton.Left)) position = game.Input.MousePosition;
        position.X = Math.Clamp(position.X, 32, Math.Max(32, game.Width - 32));
        position.Y = Math.Clamp(position.Y, 160, Math.Max(160, game.Height - 115));
    }

    public override void Draw(Graphics2D graphics)
    {
        int width = game.Width;
        int height = game.Height;
        Color muted = new(142, 154, 180);
        graphics.DrawRectangle(new Rect(0, 0, width, 134), new Color(25, 31, 47));
        graphics.DrawText("LUMIS / 01", new Vector2(32, 24), 18, HelloGame.Accent);
        graphics.DrawText("A little light. A playable world.", new Vector2(32, 56), 30, Color.White);
        graphics.DrawText("C#  /  2D  /  OPEN SOURCE", new Vector2(34, 101), 15, muted);
        for (int x = 32; x < width; x += 40)
            graphics.DrawLine(new Vector2(x, 150), new Vector2(x, Math.Max(150, height - 100)), 1, new Color(29, 36, 52));
        for (int y = 150; y < height - 100; y += 40)
            graphics.DrawLine(new Vector2(32, y), new Vector2(Math.Max(32, width - 32), y), 1, new Color(29, 36, 52));

        float glow = 36 + 4 * MathF.Sin(elapsed * 3);
        graphics.DrawCircle(position, glow + 10, HelloGame.Accent.WithAlpha(18));
        graphics.DrawCircle(position, glow, HelloGame.Accent.WithAlpha(30));
        graphics.DrawTexture(game.Sprite, new Rect(0, 0, 64, 64), new Rect(position.X, position.Y, 64, 64), new Vector2(32), elapsed * 18, Color.White);
        graphics.DrawRectangleLines(new Rect(32, 150, Math.Max(0, width - 64), Math.Max(0, height - 250)), 1, new Color(49, 63, 79));

        graphics.DrawRectangle(new Rect(0, Math.Max(0, height - 90), width, 90), new Color(25, 31, 47));
        graphics.DrawText("WASD / ARROWS  move    CLICK  place    SPACE  chime", new Vector2(32, height - 66), 17, Color.White);
        string audio = game.Audio.IsAvailable ? "M  music" : "audio disabled";
        graphics.DrawText($"{audio}    TAB  scenes    ESC  exit", new Vector2(32, height - 36), 17, muted);
        game.CaptureIfRequested();
    }
}

internal sealed class InfoScene(HelloGame game) : LumisScene
{
    public override void OnEnter() => game.SceneEntries++;

    public override void Draw(Graphics2D graphics)
    {
        graphics.DrawText("SCENE / 02", new Vector2(48, 48), 20, HelloGame.Accent);
        graphics.DrawText("One API. Room to build.", new Vector2(48, 100), 36, Color.White);
        graphics.DrawText("Window + loop\n2D rendering + textures\nKeyboard + mouse\nSound effects + music\nScene lifecycle", new Vector2(48, 180), 24, new Color(170, 187, 210));
        graphics.DrawText("Press TAB to return", new Vector2(48, game.Height - 72), 20, HelloGame.Accent);
        game.CaptureIfRequested();
    }
}
