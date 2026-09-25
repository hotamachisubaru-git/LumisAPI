using Xunit;

namespace Lumis.Tests;

public sealed class SceneManagerTests
{
    [Fact]
    public void SwitchDefersTransitionAndExitsBeforeEnteringReplacement()
    {
        using var manager = new SceneManager();
        var events = new List<string>();
        var first = new TestScene { Enter = () => events.Add("first enter"), Exit = () => events.Add("first exit") };
        var second = new TestScene { Enter = () => events.Add("second enter") };

        manager.Switch(first);
        Assert.Null(manager.Current);
        Assert.Empty(events);

        manager.ApplyPending();
        Assert.Same(first, manager.Current);
        manager.Switch(second);
        Assert.Same(first, manager.Current);
        manager.ApplyPending();

        Assert.Same(second, manager.Current);
        Assert.Equal(new[] { "first enter", "first exit", "second enter" }, events);
    }

    [Fact]
    public void LastPendingRequestWins()
    {
        using var manager = new SceneManager();
        var skipped = new TestScene();
        var selected = new TestScene();

        manager.Switch(skipped);
        manager.Switch(selected);
        manager.ApplyPending();

        Assert.Same(selected, manager.Current);
        Assert.Equal(0, skipped.EnterCount);
        Assert.Equal(1, selected.EnterCount);
    }

    [Fact]
    public void SelectingCurrentSceneCancelsTransitionWithoutReentering()
    {
        using var manager = new SceneManager();
        var current = new TestScene();
        var skipped = new TestScene();
        manager.Switch(current);
        manager.ApplyPending();

        manager.Switch(skipped);
        manager.Switch(current);
        manager.ApplyPending();

        Assert.Same(current, manager.Current);
        Assert.Equal(1, current.EnterCount);
        Assert.Equal(0, current.ExitCount);
        Assert.Equal(0, skipped.EnterCount);
    }

    [Fact]
    public void SwitchFromUpdateWaitsUntilNextFrame()
    {
        using var manager = new SceneManager();
        var next = new TestScene();
        var current = new TestScene { Tick = delta => { Assert.Equal(0.25f, delta); manager.Switch(next); } };
        manager.Switch(current);
        manager.ApplyPending();

        manager.Update(0.25f);
        Assert.Same(current, manager.Current);
        Assert.Equal(0, next.EnterCount);
        manager.ApplyPending();

        Assert.Same(next, manager.Current);
        Assert.Equal(1, current.ExitCount);
    }

    [Fact]
    public void SwitchFromEnterWaitsUntilSubsequentFrame()
    {
        using var manager = new SceneManager();
        var next = new TestScene();
        var first = new TestScene { Enter = () => manager.Switch(next) };

        manager.Switch(first);
        manager.ApplyPending();
        Assert.Same(first, manager.Current);
        Assert.Equal(0, next.EnterCount);

        manager.ApplyPending();
        Assert.Same(next, manager.Current);
    }

    [Fact]
    public void SwitchFromDrawWaitsUntilSubsequentFrame()
    {
        using var manager = new SceneManager();
        var graphics = new Graphics2D(new GameContext());
        var next = new TestScene();
        var current = new TestScene
        {
            Render = drawing => { Assert.Same(graphics, drawing); manager.Switch(next); }
        };
        manager.Switch(current);
        manager.ApplyPending();

        manager.Draw(graphics);
        Assert.Same(current, manager.Current);
        Assert.Equal(0, next.EnterCount);

        manager.ApplyPending();
        Assert.Same(next, manager.Current);
    }

    [Fact]
    public void SwitchFromExitWaitsUntilSubsequentFrame()
    {
        using var manager = new SceneManager();
        var requestedFromExit = new TestScene();
        var first = new TestScene { Exit = () => manager.Switch(requestedFromExit) };
        var second = new TestScene();
        manager.Switch(first);
        manager.ApplyPending();

        manager.Switch(second);
        manager.ApplyPending();
        Assert.Same(second, manager.Current);
        Assert.Equal(0, requestedFromExit.EnterCount);

        manager.ApplyPending();
        Assert.Same(requestedFromExit, manager.Current);
    }

    [Fact]
    public void FailedEntryLeavesManagerEmptyAndAllowsRecovery()
    {
        using var manager = new SceneManager();
        var failure = new InvalidOperationException("enter failed");
        var broken = new TestScene { Enter = () => throw failure };
        var recovery = new TestScene();

        manager.Switch(broken);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => manager.ApplyPending()));
        Assert.Null(manager.Current);
        Assert.Equal(0, broken.ExitCount);

        manager.Switch(recovery);
        manager.ApplyPending();
        Assert.Same(recovery, manager.Current);
    }

    [Fact]
    public void FailedExitLeavesManagerEmptyAndDoesNotEnterReplacement()
    {
        using var manager = new SceneManager();
        var failure = new InvalidOperationException("exit failed");
        var broken = new TestScene { Exit = () => throw failure };
        var next = new TestScene();
        manager.Switch(broken);
        manager.ApplyPending();

        manager.Switch(next);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => manager.ApplyPending()));
        Assert.Null(manager.Current);
        Assert.Equal(0, next.EnterCount);

        manager.Switch(next);
        manager.ApplyPending();
        Assert.Same(next, manager.Current);
    }

    [Fact]
    public void UpdateFailurePropagatesWithoutLosingCurrentScene()
    {
        using var manager = new SceneManager();
        var failure = new InvalidOperationException("update failed");
        var scene = new TestScene { Tick = _ => throw failure };
        manager.Switch(scene);
        manager.ApplyPending();

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => manager.Update(0.01f)));
        Assert.Same(scene, manager.Current);
    }

    [Fact]
    public void DisposeExitsOnceCancelsPendingAndDoesNotDisposeSceneResources()
    {
        var manager = new SceneManager();
        var current = new TestScene();
        var pending = new TestScene();
        manager.Switch(current);
        manager.ApplyPending();
        manager.Switch(pending);

        manager.Dispose();
        manager.Dispose();

        Assert.Equal(1, current.ExitCount);
        Assert.Equal(0, pending.EnterCount);
        Assert.Equal(0, current.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => manager.Switch(pending));
        Assert.Throws<ObjectDisposedException>(() => manager.ApplyPending());
        Assert.Throws<ObjectDisposedException>(() => manager.Update(0.01f));
        Assert.Throws<ObjectDisposedException>(() => manager.Draw(new Graphics2D(new GameContext())));
        Assert.Throws<ObjectDisposedException>(() => manager.Current);
    }

    [Fact]
    public void DisposeRemainsFinalWhenExitThrows()
    {
        var manager = new SceneManager();
        var failure = new InvalidOperationException("exit failed");
        var scene = new TestScene { Exit = () => throw failure };
        manager.Switch(scene);
        manager.ApplyPending();

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => manager.Dispose()));
        manager.Dispose();

        Assert.Equal(1, scene.ExitCount);
        Assert.Throws<ObjectDisposedException>(() => manager.Switch(new TestScene()));
    }

    [Fact]
    public void DisposalDuringExitPreventsEnteringReplacement()
    {
        var manager = new SceneManager();
        var current = new TestScene { Exit = manager.Dispose };
        var next = new TestScene();
        manager.Switch(current);
        manager.ApplyPending();

        manager.Switch(next);
        manager.ApplyPending();

        Assert.Equal(1, current.ExitCount);
        Assert.Equal(0, next.EnterCount);
        Assert.Throws<ObjectDisposedException>(() => manager.Current);
        manager.Dispose();
    }

    [Fact]
    public void SwitchingNullDoesNotLoseExistingPendingRequest()
    {
        using var manager = new SceneManager();
        var next = new TestScene();
        manager.Switch(next);

        Assert.Throws<ArgumentNullException>(() => manager.Switch(null!));
        manager.ApplyPending();

        Assert.Same(next, manager.Current);
    }

    [Fact]
    public void CrossThreadAccessIsRejectedWithoutChangingState()
    {
        using var manager = new SceneManager();
        var scene = new TestScene();
        Exception? failure = null;
        var otherThread = new Thread(() =>
        {
            failure = Record.Exception(() => manager.Switch(scene));
        });

        otherThread.Start();
        otherThread.Join();

        Assert.IsType<InvalidOperationException>(failure);
        manager.ApplyPending();
        Assert.Null(manager.Current);
        Assert.Equal(0, scene.EnterCount);
    }

    private sealed class TestScene : LumisScene, IDisposable
    {
        public Action? Enter { get; init; }
        public Action? Exit { get; init; }
        public Action<float>? Tick { get; init; }
        public Action<Graphics2D>? Render { get; init; }
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public int DisposeCount { get; private set; }

        public override void OnEnter()
        {
            EnterCount++;
            Enter?.Invoke();
        }

        public override void OnExit()
        {
            ExitCount++;
            Exit?.Invoke();
        }

        public override void Update(float deltaTime) => Tick?.Invoke(deltaTime);

        public override void Draw(Graphics2D graphics) => Render?.Invoke(graphics);

        public void Dispose() => DisposeCount++;
    }
}
