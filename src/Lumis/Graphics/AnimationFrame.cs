using System.Numerics;

namespace Lumis;

/// <summary>An immutable frame of an animation.</summary>
/// <remarks>Represents a single frame with its texture, source rectangle, display duration, and origin offset.</remarks>
public readonly record struct AnimationFrame
{
    /// <summary>Creates an animation frame.</summary>
    /// <param name="texture">The texture for this frame.</param>
    /// <param name="sourceRect">The region within the texture (sprite sheet region).</param>
    /// <param name="duration">The display duration in seconds.</param>
    /// <param name="origin">The origin offset for rendering.</param>
    public AnimationFrame(Texture texture, Rect sourceRect, float duration, Vector2 origin)
    {
        Texture = texture;
        SourceRect = sourceRect;
        Duration = duration;
        Origin = origin;
    }

    /// <summary>Gets the texture for this frame.</summary>
    public Texture Texture { get; }

    /// <summary>Gets the source rectangle within the texture.</summary>
    public Rect SourceRect { get; }

    /// <summary>Gets the display duration in seconds.</summary>
    public float Duration { get; }

    /// <summary>Gets the origin offset for rendering.</summary>
    public Vector2 Origin { get; }
}
