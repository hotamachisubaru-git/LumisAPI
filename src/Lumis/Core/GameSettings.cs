namespace Lumis;

/// <summary>Configures the window and services created by a <see cref="LumisGame"/>.</summary>
public sealed record GameSettings
{
    /// <summary>Gets the initial window width in pixels.</summary>
    public int Width { get; init; } = 960;

    /// <summary>Gets the initial window height in pixels.</summary>
    public int Height { get; init; } = 540;

    /// <summary>Gets the window title.</summary>
    public string Title { get; init; } = "LumisAPI";

    /// <summary>Gets the desired frame rate, from 1 through 1000 frames per second.</summary>
    public int TargetFps { get; init; } = 60;

    /// <summary>Gets whether the user can resize the window.</summary>
    public bool Resizable { get; init; } = true;

    /// <summary>Gets whether to synchronize frame presentation to the display refresh rate.</summary>
    public bool VSync { get; init; } = true;

    /// <summary>Gets whether to initialize audio. Disable this on machines without an audio device.</summary>
    public bool EnableAudio { get; init; } = true;

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Height);
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        if (Title.Contains('\0'))
            throw new ArgumentException("The window title cannot contain a null character.", nameof(Title));
        if (TargetFps is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(TargetFps), "TargetFps must be between 1 and 1000.");
    }
}
