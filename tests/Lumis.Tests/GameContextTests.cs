namespace Lumis.Tests;

public sealed class GameContextTests
{
    [Fact]
    public void ServicesRequireAnActiveGame()
    {
        var context = new GameContext();
        Assert.Throws<InvalidOperationException>(() => context.EnsureActive());

        context.Activate();
        context.EnsureActive();
        Assert.True(context.IsActive);

        context.Deactivate();
        Assert.False(context.IsActive);
        Assert.Throws<InvalidOperationException>(() => context.EnsureActive());
    }

    [Fact]
    public void DrawingRequiresBothActiveGameAndDrawingCallback()
    {
        var context = new GameContext();
        context.Activate();
        Assert.Throws<InvalidOperationException>(() => context.EnsureDrawing());

        context.IsDrawing = true;
        context.EnsureDrawing();

        context.Deactivate();
        Assert.False(context.IsDrawing);
        Assert.Throws<InvalidOperationException>(() => context.EnsureDrawing());
    }

    [Fact]
    public void ActiveServicesRejectAnotherThread()
    {
        var context = new GameContext();
        context.Activate();
        context.IsDrawing = true;
        Exception? activeFailure = null;
        Exception? drawingFailure = null;
        var otherThread = new Thread(() =>
        {
            activeFailure = Record.Exception(context.EnsureActive);
            drawingFailure = Record.Exception(context.EnsureDrawing);
        });

        otherThread.Start();
        otherThread.Join();

        Assert.IsType<InvalidOperationException>(activeFailure);
        Assert.IsType<InvalidOperationException>(drawingFailure);
        context.EnsureDrawing();
    }

    [Fact]
    public void ResourceCleanupRunsInReverseOrderDespiteFailureAndSelfUntracking()
    {
        var context = new GameContext();
        context.Activate();
        var events = new List<string>();
        var expectedFailure = new InvalidOperationException("second failed");
        var first = new TrackedResource(context, () => events.Add("first"));
        var second = new TrackedResource(context, () => { events.Add("second"); throw expectedFailure; });
        var third = new TrackedResource(context, () => events.Add("third"));
        context.Track(first);
        context.Track(second);
        context.Track(third);

        var failure = Assert.Throws<AggregateException>(context.DisposeResources);

        Assert.Equal(new[] { "third", "second", "first" }, events);
        Assert.Same(expectedFailure, Assert.Single(failure.InnerExceptions));
        context.DisposeResources();
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.Equal(1, third.DisposeCount);
    }

    [Fact]
    public void ExplicitlyReleasedResourcesAreNotReleasedAgainDuringCleanup()
    {
        var context = new GameContext();
        context.Activate();
        var resource = new TrackedResource(context, () => { });
        context.Track(resource);

        resource.Dispose();
        context.DisposeResources();

        Assert.Equal(1, resource.DisposeCount);
    }

    private sealed class TrackedResource(GameContext context, Action dispose) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            context.Untrack(this);
            dispose();
        }
    }
}
