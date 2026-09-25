using System.Numerics;

namespace Lumis.Tests;

public sealed class GraphicsTests
{
    [Theory]
    [InlineData(float.NaN, 0, 1, 1)]
    [InlineData(0, float.PositiveInfinity, 1, 1)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 1, -1)]
    [InlineData(0, 0, float.PositiveInfinity, 1)]
    [InlineData(0, 0, 1, float.NaN)]
    [InlineData(float.MaxValue, 0, float.MaxValue, 1)]
    [InlineData(0, float.MaxValue, 1, float.MaxValue)]
    public void RectangleRejectsInvalidOrOverflowingEdges(float x, float y, float width, float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rect(x, y, width, height));
    }

    [Fact]
    public void AdjacentRectanglesDoNotDoubleCountSharedBoundary()
    {
        var left = new Rect(0, 0, 10, 10);
        var right = new Rect(10, 0, 10, 10);
        var sharedEdge = new Vector2(10, 5);

        Assert.False(left.Contains(sharedEdge));
        Assert.True(right.Contains(sharedEdge));
        Assert.False(left.Contains(new Vector2(5, 10)));
        Assert.False(left.Intersects(right));
        Assert.False(right.Intersects(left));
        Assert.True(left.Intersects(new Rect(9, 0, 10, 10)));
    }

    [Fact]
    public void EmptyRectanglesNeitherContainPointsNorIntersect()
    {
        var full = new Rect(0, 0, 10, 10);
        var empty = new Rect(5, 5, 0, 0);

        Assert.False(empty.Contains(new Vector2(5, 5)));
        Assert.False(empty.Intersects(full));
        Assert.False(full.Intersects(empty));
        Assert.False(default(Rect).Intersects(full));
    }

    [Fact]
    public void WindowOperationsRejectInactiveContextBeforeReachingNativeBackend()
    {
        var context = new GameContext();
        var graphics = new Graphics2D(context);

        Assert.Throws<InvalidOperationException>(() => graphics.LoadTexture("unopened.png"));
        Assert.Throws<InvalidOperationException>(() => graphics.MeasureText("text", 16));
        Assert.Throws<InvalidOperationException>(() => graphics.Clear(Color.Black));

        context.Activate();
        context.Deactivate();

        Assert.Throws<InvalidOperationException>(() => graphics.CaptureScreenshot("closed.png"));
        Assert.Throws<InvalidOperationException>(() => graphics.DrawCircle(Vector2.Zero, 10, Color.White));
    }

    [Fact]
    public void DrawingOutsideDrawCallbackIsRejected()
    {
        var context = new GameContext();
        context.Activate();
        var graphics = new Graphics2D(context);

        Assert.Throws<InvalidOperationException>(() => graphics.Clear(Color.Black));
        Assert.Throws<InvalidOperationException>(() => graphics.DrawRectangle(new Rect(0, 0, 5, 5), Color.Red));
        Assert.Throws<InvalidOperationException>(() => graphics.DrawText("text", Vector2.Zero, 16, Color.White));
        Assert.Throws<InvalidOperationException>(() => graphics.CaptureScreenshot("outside-draw.png"));
    }

    [Fact]
    public void InvalidDrawingArgumentsAreRejectedBeforeNativeCalls()
    {
        var context = new GameContext();
        context.Activate();
        context.IsDrawing = true;
        var graphics = new Graphics2D(context);

        Assert.Throws<ArgumentOutOfRangeException>(() => graphics.DrawCircle(Vector2.Zero, float.NaN, Color.White));
        Assert.Throws<ArgumentOutOfRangeException>(() => graphics.DrawCircle(new Vector2(float.PositiveInfinity, 0), 1, Color.White));
        Assert.Throws<ArgumentOutOfRangeException>(() => graphics.DrawLine(Vector2.Zero, Vector2.One, 0, Color.White));
        Assert.Throws<ArgumentOutOfRangeException>(() => graphics.DrawText("text", Vector2.Zero, 0, Color.White));
        Assert.Throws<ArgumentException>(() => graphics.DrawText("text\0hidden", Vector2.Zero, 16, Color.White));
        Assert.Throws<ArgumentNullException>(() => graphics.DrawTexture(null!, Vector2.Zero));
    }
}
