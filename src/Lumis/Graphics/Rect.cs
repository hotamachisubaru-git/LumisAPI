using System.Numerics;

namespace Lumis;

/// <summary>An immutable axis-aligned rectangle in screen coordinates.</summary>
/// <remarks>The origin is at the top left. X increases rightward and Y increases downward.</remarks>
public readonly record struct Rect
{
    /// <summary>Creates a rectangle with finite coordinates and nonnegative dimensions.</summary>
    /// <param name="x">The left edge.</param>
    /// <param name="y">The top edge.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    public Rect(float x, float y, float width, float height)
    {
        if (!float.IsFinite(x))
            throw new ArgumentOutOfRangeException(nameof(x), "The coordinate must be finite.");
        if (!float.IsFinite(y))
            throw new ArgumentOutOfRangeException(nameof(y), "The coordinate must be finite.");
        if (!float.IsFinite(width) || width < 0 || !float.IsFinite(x + width))
            throw new ArgumentOutOfRangeException(nameof(width), "The width must be nonnegative and the right edge finite.");
        if (!float.IsFinite(height) || height < 0 || !float.IsFinite(y + height))
            throw new ArgumentOutOfRangeException(nameof(height), "The height must be nonnegative and the bottom edge finite.");

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Gets the left edge.</summary>
    public float X { get; }

    /// <summary>Gets the top edge.</summary>
    public float Y { get; }

    /// <summary>Gets the width.</summary>
    public float Width { get; }

    /// <summary>Gets the height.</summary>
    public float Height { get; }

    /// <summary>Gets the right edge.</summary>
    public float Right => X + Width;

    /// <summary>Gets the bottom edge.</summary>
    public float Bottom => Y + Height;

    /// <summary>Gets the top-left position.</summary>
    public Vector2 Position => new(X, Y);

    /// <summary>Gets the width and height.</summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>Gets the center position.</summary>
    public Vector2 Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>Tests whether a point is inside this rectangle, excluding the right and bottom edges.</summary>
    /// <param name="point">The point to test.</param>
    public bool Contains(Vector2 point) =>
        point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    /// <summary>Tests whether two rectangles overlap with positive area; touching edges do not intersect.</summary>
    /// <param name="other">The other rectangle.</param>
    public bool Intersects(Rect other) =>
        Width > 0 && Height > 0 && other.Width > 0 && other.Height > 0 &&
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    internal Raylib_cs.Rectangle ToNative() => new(X, Y, Width, Height);
}
