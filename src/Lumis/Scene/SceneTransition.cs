namespace Lumis;

/// <summary>Represents a transition effect between scenes.</summary>
/// <remarks>
/// Transition effects are applied during scene switching. The transition state
/// is updated each frame via <see cref="Update"/> and is considered complete
/// when <see cref="Progress"/> reaches 1.0.
/// </remarks>
public sealed class SceneTransition
{
    /// <summary>The direction of a slide transition.</summary>
    public enum SlideDirection
    {
        /// <summary>Slide to the left.</summary>
        Left,

        /// <summary>Slide to the right.</summary>
        Right,
    }

    /// <summary>Gets the name of the transition effect.</summary>
    public string Name { get; }

    /// <summary>Gets the total duration of the transition in seconds.</summary>
    public float Duration { get; }

    /// <summary>Gets the current progress of the transition (0.0 to 1.0).</summary>
    public float Progress { get; private set; }

    /// <summary>Gets whether the transition has completed.</summary>
    public bool IsComplete => Progress >= 1.0f;

    /// <summary>Gets the slide direction, or <see langword="null"/> for non-slide transitions.</summary>
    public SlideDirection? Direction { get; }

    /// <summary>Gets the starting offset for slide transitions, or <see langword="null"/> for non-slide transitions.</summary>
    public System.Numerics.Vector2? StartOffset { get; }

    private SceneTransition(string name, float duration, SlideDirection? slideDirection = null, System.Numerics.Vector2? startOffset = null)
    {
        Name = name;
        Duration = duration;
        Progress = 0.0f;
        Direction = slideDirection;
        StartOffset = startOffset;
    }

    /// <summary>Updates the transition progress by the given delta time.</summary>
    /// <param name="deltaTime">Elapsed time since the last update, in seconds.</param>
    public void Update(float deltaTime)
    {
        if (IsComplete)
        {
            return;
        }

        Progress = Math.Min(1.0f, Progress + deltaTime / Duration);
    }

    /// <summary>Creates a fade-out transition.</summary>
    /// <param name="duration">The duration of the fade-out in seconds.</param>
    /// <returns>A new <see cref="SceneTransition"/> for fade-out.</returns>
    public static SceneTransition FadeOut(float duration)
    {
        return new SceneTransition("FadeOut", duration);
    }

    /// <summary>Creates a fade-in transition.</summary>
    /// <param name="duration">The duration of the fade-in in seconds.</param>
    /// <returns>A new <see cref="SceneTransition"/> for fade-in.</returns>
    public static SceneTransition FadeIn(float duration)
    {
        return new SceneTransition("FadeIn", duration);
    }

    /// <summary>Creates a slide-left transition.</summary>
    /// <param name="duration">The duration of the slide in seconds.</param>
    /// <returns>A new <see cref="SceneTransition"/> for slide-left.</returns>
    public static SceneTransition SlideLeft(float duration)
    {
        return new SceneTransition("SlideLeft", duration, SlideDirection.Left, new System.Numerics.Vector2(-1.0f, 0.0f));
    }

    /// <summary>Creates a slide-right transition.</summary>
    /// <param name="duration">The duration of the slide in seconds.</param>
    /// <returns>A new <see cref="SceneTransition"/> for slide-right.</returns>
    public static SceneTransition SlideRight(float duration)
    {
        return new SceneTransition("SlideRight", duration, SlideDirection.Right, new System.Numerics.Vector2(1.0f, 0.0f));
    }

    /// <summary>Creates a transition with no effect (instant scene switch).</summary>
    /// <returns>A new <see cref="SceneTransition"/> that completes immediately.</returns>
    public static SceneTransition None()
    {
        return new SceneTransition("None", 0.0f);
    }
}
