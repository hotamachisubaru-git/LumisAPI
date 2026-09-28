namespace Lumis;

/// <summary>Provides convenience scene transition methods as extensions on SceneManager.</summary>
/// <remarks>
/// <para>These methods add transition effects to scene switching. When a transition is active,
/// scene switch requests are queued and applied in order after the transition completes.</para>
/// <para>All methods must be called on the game thread only.</para>
/// </remarks>
public static class SceneManagerExtensions
{
    /// <summary>Switches to a scene with a fade-out transition.</summary>
    /// <param name="manager">The scene manager.</param>
    /// <param name="scene">The scene to activate.</param>
    /// <param name="fadeDuration">The duration of the fade-out in seconds.</param>
    /// <remarks>
    /// The current scene fades to black, then the new scene fades in from black.
    /// </remarks>
    public static void SwitchFadeOut(this SceneManager manager, LumisScene scene, float fadeDuration)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fadeDuration);

        var fadeOut = SceneTransition.FadeOut(fadeDuration);
        manager.Switch(scene, fadeOut);
    }

    /// <summary>Switches to a scene with a fade-in transition.</summary>
    /// <param name="manager">The scene manager.</param>
    /// <param name="scene">The scene to activate.</param>
    /// <param name="fadeDuration">The duration of the fade-in in seconds.</param>
    public static void SwitchFadeIn(this SceneManager manager, LumisScene scene, float fadeDuration)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fadeDuration);

        var fadeIn = SceneTransition.FadeIn(fadeDuration);
        manager.Switch(scene, fadeIn);
    }

    /// <summary>Switches to a scene with a slide transition.</summary>
    /// <param name="manager">The scene manager.</param>
    /// <param name="scene">The scene to activate.</param>
    /// <param name="direction">The slide direction.</param>
    /// <param name="slideDuration">The duration of the slide in seconds.</param>
    public static void SwitchSlide(this SceneManager manager, LumisScene scene, SceneTransition.SlideDirection direction, float slideDuration)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slideDuration);

        var slide = direction switch
        {
            SceneTransition.SlideDirection.Left => SceneTransition.SlideLeft(slideDuration),
            SceneTransition.SlideDirection.Right => SceneTransition.SlideRight(slideDuration),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported slide direction.")
        };
        manager.Switch(scene, slide);
    }

    /// <summary>Switches to a scene with a custom transition.</summary>
    /// <param name="manager">The scene manager.</param>
    /// <param name="scene">The scene to activate.</param>
    /// <param name="transition">The transition effect to apply.</param>
    /// <remarks>
    /// If the transition is already complete or has zero duration, the switch is applied immediately.
    /// </remarks>
    public static void Switch(this SceneManager manager, LumisScene scene, SceneTransition transition)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(transition);
        manager.Switch(scene, transition);
    }
}
