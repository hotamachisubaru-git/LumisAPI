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
    private SceneTransition? _activeTransition;
    private readonly Queue<LumisScene> _transitionQueue = new();
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

    /// <summary>Gets the current active transition, or <see langword="null"/> when no transition is in progress.</summary>
    public SceneTransition? ActiveTransition => _activeTransition;

    /// <summary>Gets the number of pending scene switch requests queued during an active transition.</summary>
    public int TransitionQueueCount => _transitionQueue.Count;

    /// <summary>Requests a scene to become current at the start of the next frame.</summary>
    /// <param name="scene">The scene to activate.</param>
    /// <param name="transition">The transition effect to apply during the switch.</param>
    /// <remarks>
    /// When a transition is specified, the scene switch is deferred until the transition completes.
    /// Additional scene requests made during an active transition are queued and applied in order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="scene"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The manager has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Called from a different thread.</exception>
    public void Switch(LumisScene scene, SceneTransition transition)
    {
        EnsureUsable();
        ArgumentNullException.ThrowIfNull(scene);

        if (transition.IsComplete || transition.Duration <= 0.0f)
        {
            _pending = scene;
            return;
        }

        if (_activeTransition is null)
        {
            _activeTransition = transition;
        }

        _transitionQueue.Enqueue(scene);
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

        // If a transition is active, update it and do not apply scene changes yet.
        if (_activeTransition is not null)
        {
            return;
        }

        LumisScene? next = _pending;
        _pending = null;
        if (next is null || ReferenceEquals(next, _current))
        {
            return;
        }

        // If the transition queue has items, start transitioning to the first one.
        if (_transitionQueue.Count > 0)
        {
            next = _transitionQueue.Dequeue();
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

    /// <summary>Updates the active transition and applies queued scene switches when complete.</summary>
    /// <param name="deltaTime">Elapsed time since the last update, in seconds.</param>
    internal void UpdateTransition(float deltaTime)
    {
        EnsureUsable();

        if (_activeTransition is null)
        {
            return;
        }

        _activeTransition.Update(deltaTime);

        if (_activeTransition.IsComplete)
        {
            _activeTransition = null;

            // Apply queued scenes one at a time.
            while (_transitionQueue.Count > 0)
            {
                LumisScene? next = _transitionQueue.Dequeue();
                if (ReferenceEquals(next, _current))
                {
                    continue;
                }

                LumisScene? previous = _current;
                _current = null;
                previous?.OnExit();

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

                // Only apply one queued switch per frame to keep transitions smooth.
                break;
            }
        }
    }

    internal void Update(float deltaTime)
    {
        EnsureUsable();
        _current?.Update(deltaTime);
        UpdateTransition(deltaTime);
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
