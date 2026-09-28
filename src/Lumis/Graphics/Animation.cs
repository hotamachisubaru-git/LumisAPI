namespace Lumis;

/// <summary>An immutable sequence of animation frames.</summary>
/// <remarks>Defines a read-only animation with a name, frames, loop setting, and frame duration.</remarks>
public readonly record struct Animation
{
    /// <summary>Creates an animation.</summary>
    /// <param name="name">The animation name.</param>
    /// <param name="frames">The sequence of frames.</param>
    /// <param name="frameDuration">The display duration for each frame in seconds.</param>
    /// <param name="looping">Whether the animation loops; defaults to true.</param>
    public Animation(string name, AnimationFrame[] frames, float frameDuration, bool looping = true)
    {
        Name = name;
        Frames = frames;
        FrameDuration = frameDuration;
        Looping = looping;
    }

    /// <summary>Gets the animation name.</summary>
    public string Name { get; }

    /// <summary>Gets the frame sequence.</summary>
    public AnimationFrame[] Frames { get; }

    /// <summary>Gets whether the animation loops.</summary>
    public bool Looping { get; }

    /// <summary>Gets the display duration for each frame in seconds.</summary>
    public float FrameDuration { get; }

    /// <summary>Gets the total duration of all frames (read-only).</summary>
    public float TotalDuration => Frames.Sum(f => f.Duration);

    /// <summary>Gets the frame count (read-only).</summary>
    public int FrameCount => Frames.Length;

    /// <summary>Gets a frame by index.</summary>
    /// <param name="index">The zero-based frame index.</param>
    /// <returns>The frame at the specified index.</returns>
    public AnimationFrame GetFrame(int index) => Frames[index];

    /// <summary>Creates an animation from frames.</summary>
    /// <param name="name">The animation name.</param>
    /// <param name="frames">The sequence of frames.</param>
    /// <param name="frameDuration">The display duration for each frame in seconds.</param>
    /// <param name="looping">Whether the animation loops; defaults to true.</param>
    /// <returns>The created animation.</returns>
    public static Animation Create(string name, AnimationFrame[] frames, float frameDuration, bool looping = true)
        => new(name, frames, frameDuration, looping);
}
