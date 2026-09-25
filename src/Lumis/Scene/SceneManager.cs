namespace Lumis;

/// <summary>Coordinates deferred transitions between game scenes.</summary>
/// <remarks>
/// Use the manager only on the thread that created it. The game applies at most one
/// requested transition at the start of each frame. Requests made in scene callbacks
/// are deferred until a subsequent frame; when several requests are made, the last wins.
/// The manager invokes lifecycle callbacks but never disposes scene objects.
/// If an enter or exit callback throws, no scene remains current and the exception
/// propagates to the game loop. A scene whose enter callback failed is not exited.
/// </remarks>
public sealed class SceneManager : IDisposable
{
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;
    private LumisScene? _current;
    private LumisScene? _pending;
    private bool _disposed;

    /// <summary>Creates an empty scene manager on the calling thread.</summary>
    public SceneManager()
    {
    }

    /// <summary>Gets the current scene, or <see langword="null"/> when none is active.</summary>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Called from a different thread.</exception>
    public LumisScene? Current
    {
        get
        {
            EnsureUsable();
            return _current;
        }
    }

    /// <summary>Requests a scene to become current at the start of the next frame.</summary>
    /// <param name="scene">The scene to activate.</param>
    /// <remarks>
    /// This replaces any previous pending request. Requesting the current scene
    /// cancels a pending transition without invoking its lifecycle callbacks again.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="scene"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Called from a different thread.</exception>
    public void Switch(LumisScene scene)
    {
        EnsureUsable();
        ArgumentNullException.ThrowIfNull(scene);
        _pending = scene;
    }

    internal void ApplyPending()
    {
        EnsureUsable();

        LumisScene? next = _pending;
        _pending = null;
        if (next is null || ReferenceEquals(next, _current))
        {
            return;
        }

        LumisScene? previous = _current;
        _current = null;
        previous?.OnExit();

        // An exit callback can dispose the manager. Do not enter another scene then.
        if (_disposed)
        {
            return;
        }

        _current = next;
        try
        {
            next.OnEnter();
        }
        catch
        {
            _current = null;
            throw;
        }
    }

    internal void Update(float deltaTime)
    {
        EnsureUsable();
        _current?.Update(deltaTime);
    }

    internal void Draw(Graphics2D graphics)
    {
        EnsureUsable();
        _current?.Draw(graphics);
    }

    /// <summary>Exits the active scene and cancels any pending transition.</summary>
    /// <remarks>
    /// Disposal is idempotent. The manager remains disposed even if a scene's exit
    /// callback throws. Resources owned by scene objects must be released separately.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Called from a different thread.</exception>
    public void Dispose()
    {
        EnsureOwnerThread();
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pending = null;
        LumisScene? previous = _current;
        _current = null;
        previous?.OnExit();
    }

    private void EnsureUsable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureOwnerThread();
    }

    private void EnsureOwnerThread()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
        {
            throw new InvalidOperationException("SceneManager must be used on the thread that created it.");
        }
    }
}
