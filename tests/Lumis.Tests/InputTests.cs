namespace Lumis.Tests;

public sealed class InputTests
{
    [Fact]
    public void PollingBeforeAndAfterWindowLifetimeIsRejected()
    {
        var context = new GameContext();
        var input = new InputState(context);

        Assert.Throws<InvalidOperationException>(() => input.IsKeyDown(Key.Space));
        Assert.Throws<InvalidOperationException>(() => input.MousePosition);

        context.Activate();
        context.Deactivate();

        Assert.Throws<InvalidOperationException>(() => input.IsMouseButtonPressed(MouseButton.Left));
        Assert.Throws<InvalidOperationException>(() => input.MouseWheelDelta);
    }

    [Fact]
    public void UnknownInputCodesAreRejectedBeforeNativePolling()
    {
        var context = new GameContext();
        context.Activate();
        var input = new InputState(context);

        Assert.Throws<ArgumentOutOfRangeException>(() => input.IsKeyPressed((Key)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => input.IsMouseButtonDown((MouseButton)9999));
    }

    [Fact]
    public void PollingOnAnotherThreadIsRejected()
    {
        var context = new GameContext();
        context.Activate();
        var input = new InputState(context);
        Exception? failure = null;
        var otherThread = new Thread(() => failure = Record.Exception(() => input.IsKeyDown(Key.Space)));

        otherThread.Start();
        otherThread.Join();

        Assert.IsType<InvalidOperationException>(failure);
    }
}
