using Raylib_cs;

namespace Lumis;

/// <summary>Owns a desktop window and runs update and drawing callbacks on the calling thread.</summary>
/// <remarks>
/// Create and run the game on the application's main thread. Each instance can run once.
/// Only one game may run at a time because the native backend has process-wide state.
/// Resources created through this game are released automatically before its window closes.
/// </remarks>
public abstract class LumisGame : IDisposable
{
    private static int runningGame;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly GameContext context = new();
    private bool hasRun;
    private bool isRunning;
    private bool disposed;
    private int exitRequested;

    /// <summary>Creates a game using the supplied settings or their defaults.</summary>
    /// <param name="settings">Window and service settings.</param>
    protected LumisGame(GameSettings? settings = null)
    {
        Settings = settings ?? new GameSettings();
        Settings.Validate();
        Graphics = new Graphics2D(context);
        Input = new InputState(context);
        Audio = new AudioDevice(context);
    }

    /// <summary>Gets the immutable settings used to create the game.</summary>
    public GameSettings Settings { get; }

    /// <summary>Gets the drawing and texture service.</summary>
    public Graphics2D Graphics { get; }

    /// <summary>Gets the keyboard and mouse input service.</summary>
    public InputState Input { get; }

    /// <summary>Gets the sound and streamed music service.</summary>
    public AudioDevice Audio { get; }

    /// <summary>Gets the manager for deferred scene transitions.</summary>
    public SceneManager Scenes { get; } = new();

    /// <summary>Gets or sets the color used to clear the window at the start of each frame.</summary>
    public Color BackgroundColor { get; set; } = new(18, 20, 32);

    /// <summary>Gets the current drawable window width in screen coordinates.</summary>
    public int Width { get { context.EnsureActive(); return Raylib.GetScreenWidth(); } }

    /// <summary>Gets the current drawable window height in screen coordinates.</summary>
    public int Height { get { context.EnsureActive(); return Raylib.GetScreenHeight(); } }

    /// <summary>Opens the window and blocks until the window closes or <see cref="Exit"/> is called.</summary>
    /// <remarks>Exceptions from callbacks are rethrown after resources, audio, and the window are cleaned up.</remarks>
    public void Run()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        EnsureOwnerThread();
        if (hasRun)
            throw new InvalidOperationException("A game instance can only run once.");
        if (Interlocked.CompareExchange(ref runningGame, 1, 0) != 0)
            throw new InvalidOperationException("Only one LumisGame can run at a time.");

        hasRun = true;
        isRunning = true;
        bool windowAttempted = false;
        bool loadStarted = false;
        List<Exception> errors = [];
        try
        {
            ConfigFlags flags = 0;
            if (Settings.Resizable) flags |= ConfigFlags.ResizableWindow;
            if (Settings.VSync) flags |= ConfigFlags.VSyncHint;
            Raylib.SetConfigFlags(flags);
            windowAttempted = true;
            Raylib.InitWindow(Settings.Width, Settings.Height, Settings.Title);
            if (!Raylib.IsWindowReady())
                throw new InvalidOperationException("The native window could not be created.");

            // raylib retains configuration bits between windows in the same process.
            ConfigFlags disabledFlags = 0;
            if (!Settings.Resizable) disabledFlags |= ConfigFlags.ResizableWindow;
            if (!Settings.VSync) disabledFlags |= ConfigFlags.VSyncHint;
            if (disabledFlags != 0) Raylib.ClearWindowState(disabledFlags);

            context.Activate();
            Raylib.SetTargetFPS(Settings.TargetFps);
            Raylib.SetExitKey(KeyboardKey.Null);
            Audio.Initialize(Settings.EnableAudio);
            loadStarted = true;
            OnLoad();

            while (Volatile.Read(ref exitRequested) == 0 && !Raylib.WindowShouldClose())
            {
                float deltaTime = Math.Clamp(Raylib.GetFrameTime(), 0f, 0.25f);
                Scenes.ApplyPending();
                Update(deltaTime);
                Scenes.Update(deltaTime);
                if (Volatile.Read(ref exitRequested) != 0) break;
                Audio.UpdateStreams();

                Raylib.BeginDrawing();
                context.IsDrawing = true;
                try
                {
                    Graphics.Clear(BackgroundColor);
                    Draw();
                    Scenes.Draw(Graphics);
                }
                finally
                {
                    context.IsDrawing = false;
                    Raylib.EndDrawing();
                }
            }
        }
        catch (Exception ex) { errors.Add(ex); }
        finally
        {
            // Each cleanup runs even if an earlier cleanup callback fails.
            Cleanup(Scenes.Dispose);
            if (loadStarted) Cleanup(OnUnload);
            if (context.IsActive)
            {
                Cleanup(context.DisposeResources);
                Cleanup(Audio.Shutdown);
                context.Deactivate();
            }
            if (windowAttempted) Cleanup(() => { if (Raylib.IsWindowReady()) Raylib.CloseWindow(); });
            isRunning = false;
            disposed = true;
            Interlocked.Exchange(ref runningGame, 0);
        }

        if (errors.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException("The game failed and one or more cleanup operations also failed.", errors);

        void Cleanup(Action cleanup)
        {
            try { cleanup(); }
            catch (Exception ex) { errors.Add(ex); }
        }
    }

    /// <summary>Requests the game loop to stop. This method may be called from any thread.</summary>
    public void Exit() => Interlocked.Exchange(ref exitRequested, 1);

    /// <summary>Releases an unstarted game, or requests shutdown if the game is running.</summary>
    /// <remarks>When called during Run, native cleanup occurs on the game thread before Run returns.</remarks>
    public void Dispose()
    {
        if (disposed) return;
        EnsureOwnerThread();
        if (isRunning) { Exit(); return; }
        disposed = true;
        Scenes.Dispose();
        GC.SuppressFinalize(this);
    }

    private void EnsureOwnerThread()
    {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Create, run, and dispose a game on the same main thread. Use Exit() to request shutdown from another thread.");
    }

    /// <summary>Loads game resources after the window and configured audio device are ready.</summary>
    protected virtual void OnLoad() { }

    /// <summary>Updates game state once per frame, before the current scene updates.</summary>
    /// <param name="deltaTime">Elapsed seconds, capped at 0.25 to limit jumps after a stall.</param>
    protected virtual void Update(float deltaTime) { }

    /// <summary>Draws the game before the current scene. Drawing services are available in this callback.</summary>
    protected virtual void Draw() { }

    /// <summary>Releases custom resources after the active scene exits and before managed game resources unload.</summary>
    /// <remarks>Called even when OnLoad throws; resources owned by Lumis are also released automatically.</remarks>
    protected virtual void OnUnload() { }
}
