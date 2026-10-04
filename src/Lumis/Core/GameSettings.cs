namespace Lumis;

/// <summary>Configures the window and services created by a <see cref="LumisGame"/>.</summary>
/// <remarks>
/// Properties use normal setters so the public configuration surface can be consumed from
/// C# 8 through the latest supported C# version. Configure the instance before constructing
/// the game.
/// </remarks>
public sealed class GameSettings
{
    /// <summary>Gets or sets the initial window width in pixels.</summary>
    public int Width { get; set; } = 960;

    /// <summary>Gets or sets the initial window height in pixels.</summary>
    public int Height { get; set; } = 540;

    /// <summary>Gets or sets the window title.</summary>
    public string Title { get; set; } = "LumisAPI";

    /// <summary>Gets or sets the desired frame rate, from 1 through 1000 frames per second.</summary>
    public int TargetFps { get; set; } = 60;

    /// <summary>Gets or sets whether the user can resize the window.</summary>
    public bool Resizable { get; set; } = true;

    /// <summary>Gets or sets whether to synchronize frame presentation to the display refresh rate.</summary>
    public bool VSync { get; set; } = true;

    /// <summary>Gets or sets whether to initialize audio. Disable this on machines without an audio device.</summary>
    public bool EnableAudio { get; set; } = true;

    /// <summary>Gets or sets the optional anti-cheat configuration.</summary>
    public AntiCheatSettings AntiCheat { get; set; } = new AntiCheatSettings();

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Height);
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        if (Title.Contains('\0'))
            throw new ArgumentException("The window title cannot contain a null character.", nameof(Title));
        if (TargetFps is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(TargetFps), "TargetFps must be between 1 and 1000.");

        ArgumentNullException.ThrowIfNull(AntiCheat);
        AntiCheat.Validate();
    }
}
