namespace Lumis.Tests;

public sealed class LumisGameTests
{
    [Fact]
    public void RunOnAnotherThreadFailsBeforeStartupAndAllowsOwnerThreadCleanup()
    {
        using var game = new UnstartedGame();

        var failure = OnAnotherThread(game.Run);

        Assert.IsType<InvalidOperationException>(failure);
        Assert.Equal(0, game.LoadCount);
        Assert.Null(game.Scenes.Current);
        game.Dispose();
        Assert.Throws<ObjectDisposedException>(() => game.Scenes.Current);
    }

    [Fact]
    public void DisposeOnAnotherThreadDoesNotPartiallyDisposeUnstartedGame()
    {
        using var game = new UnstartedGame();

        var failure = OnAnotherThread(game.Dispose);

        Assert.IsType<InvalidOperationException>(failure);
        Assert.Null(game.Scenes.Current);
        game.Dispose();
        Assert.Throws<ObjectDisposedException>(() => game.Scenes.Current);
        Assert.Equal(0, game.LoadCount);
    }

    [Fact]
    public void DisposedGameCannotRunOrInitializeNativeServices()
    {
        var game = new UnstartedGame();

        game.Dispose();
        game.Dispose();

        Assert.Throws<ObjectDisposedException>(game.Run);
        Assert.Equal(0, game.LoadCount);
        Assert.Throws<InvalidOperationException>(() => game.Width);
        Assert.False(game.Audio.IsAvailable);
    }

    private static Exception? OnAnotherThread(Action action)
    {
        Exception? failure = null;
        var otherThread = new Thread(() => failure = Record.Exception(action));
        otherThread.Start();
        otherThread.Join();
        return failure;
    }

    private sealed class UnstartedGame : LumisGame
    {
        public int LoadCount { get; private set; }

        protected override void OnLoad() => LoadCount++;
    }
}
