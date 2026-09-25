namespace Lumis;

// Native resources must be released while their window/audio device is still alive.
internal sealed class GameContext
{
    private readonly List<IDisposable> resources = [];
    private int ownerThread;

    internal bool IsActive { get; private set; }
    internal bool IsDrawing { get; set; }

    internal void Activate()
    {
        ownerThread = Environment.CurrentManagedThreadId;
        IsActive = true;
    }

    internal void Deactivate()
    {
        IsDrawing = false;
        IsActive = false;
    }

    internal void EnsureActive()
    {
        if (!IsActive)
            throw new InvalidOperationException("The game must be running to use this service.");
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Game services must be used on the thread that called Run().");
    }

    internal void EnsureDrawing()
    {
        EnsureActive();
        if (!IsDrawing)
            throw new InvalidOperationException("Drawing is only allowed inside Draw callbacks.");
    }

    internal void Track(IDisposable resource)
    {
        EnsureActive();
        resources.Add(resource);
    }

    internal void Untrack(IDisposable resource)
    {
        EnsureActive();
        resources.Remove(resource);
    }

    internal void DisposeResources()
    {
        List<Exception> errors = [];
        for (int i = resources.Count - 1; i >= 0; i--)
        {
            try { resources[i].Dispose(); }
            catch (Exception ex) { errors.Add(ex); }
        }
        resources.Clear();
        if (errors.Count > 0)
            throw new AggregateException("Some game resources could not be released.", errors);
    }
}
