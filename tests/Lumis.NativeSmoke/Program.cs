using System.Numerics;
using Lumis;
using Raylib_cs;
using Color = Lumis.Color;
using Texture = Lumis.Texture;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Any(arg => arg != "--no-audio"))
                throw new ArgumentException("Supported argument: --no-audio");
            bool audio = !args.Contains("--no-audio");
            RunCase(true, audio, Failure.None);
            // Same process: raylib retains flags unless Lumis explicitly clears them.
            RunCase(false, audio, Failure.Load);
            RunCase(false, audio, Failure.Draw);
            RunCase(false, audio, Failure.UpdateAndUnload);
            // An earlier callback/cleanup failure must not retain the global game lock.
            RunCase(false, audio, Failure.None);
            Console.WriteLine("NATIVE LIFECYCLE PASS: sequential settings, resource cleanup, callback failures, recovery");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void RunCase(bool flags, bool audio, Failure failure)
    {
        using var game = new ProbeGame(flags, audio, failure);
        Exception? caught = null;
        try { game.Run(); }
        catch (Exception ex) { caught = ex; }

        switch (failure)
        {
            case Failure.None:
                Check(caught is null, $"Unexpected failure: {caught}");
                break;
            case Failure.UpdateAndUnload:
                Check(caught is AggregateException aggregate && aggregate.InnerExceptions.Count == 2
                    && aggregate.InnerExceptions[0] is ProbeException { Message: "update" }
                    && aggregate.InnerExceptions[1] is ProbeException { Message: "unload" }, "Preserve primary and cleanup failures");
                break;
            default:
                Check(caught is ProbeException, $"Preserve callback failure: {caught}");
                break;
        }

        Check(game.Unloaded, "OnUnload runs");
        Check(game.Sprite is { IsDisposed: true }, "Texture released automatically");
        Check(!Raylib.IsWindowReady(), "Window closed");
        Check(!Raylib.IsAudioDeviceReady(), "Audio closed");
        game.Sprite!.Dispose();
        if (game.Clip is not null)
        {
            game.Clip.Dispose();
            bool rejected = false;
            try { game.Clip.Play(); }
            catch (ObjectDisposedException) { rejected = true; }
            Check(rejected, "Disposed audio resource rejects native call");
        }
    }

    internal static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}

internal enum Failure { None, Load, Draw, UpdateAndUnload }
internal sealed class ProbeException(string message) : Exception(message);

internal sealed class ProbeGame(bool flags, bool audio, Failure failure) : LumisGame(new GameSettings
{
    Title = "LumisAPI native lifecycle test",
    Width = 320,
    Height = 240,
    Resizable = flags,
    VSync = flags,
    EnableAudio = audio
})
{
    private int frames;
    internal Texture? Sprite { get; private set; }
    internal SoundClip? Clip { get; private set; }
    internal bool Unloaded { get; private set; }

    protected override void OnLoad()
    {
        Program.Check((bool)Raylib.IsWindowState(ConfigFlags.ResizableWindow) == flags, "Resizable setting retained from prior game");
        Program.Check((bool)Raylib.IsWindowState(ConfigFlags.VSyncHint) == flags, "VSync setting retained from prior game");
        Sprite = Graphics.LoadTexture(Path.Combine(AppContext.BaseDirectory, "Assets", "lumis.png"));
        if (audio) Clip = Audio.LoadSound(Path.Combine(AppContext.BaseDirectory, "Assets", "chime.wav"));
        using var nested = new ProbeGame(false, false, Failure.None);
        bool rejected = false;
        try { nested.Run(); }
        catch (InvalidOperationException) { rejected = true; }
        Program.Check(rejected, "Nested native game must be rejected");
        if (failure == Failure.Load) throw new ProbeException("load");
    }

    protected override void Update(float deltaTime)
    {
        if (failure == Failure.UpdateAndUnload) throw new ProbeException("update");
        if (++frames == 3) Exit();
    }

    protected override void Draw()
    {
        Graphics.DrawText("Lumis lifecycle test", new Vector2(8, 8), 16, Color.White);
        Graphics.DrawTexture(Sprite!, new Vector2(64, 64));
        if (failure == Failure.Draw) throw new ProbeException("draw");
    }

    protected override void OnUnload()
    {
        Unloaded = true;
        if (failure == Failure.UpdateAndUnload) throw new ProbeException("unload");
    }
}
