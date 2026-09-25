using Raylib_cs;

namespace Lumis;

/// <summary>A short sound held in memory and owned by a running game.</summary>
/// <remarks>
/// A clip has one playback voice; playing it again restarts that voice.
/// Call its members, including disposal, on the game thread. The game disposes remaining clips on shutdown.
/// </remarks>
public sealed class SoundClip : IDisposable
{
    private readonly GameContext _context;
    private readonly AudioDevice _device;
    private readonly Sound _sound;
    private float _volume = 1f;
    private bool _disposed;

    internal SoundClip(GameContext context, AudioDevice device, Sound sound)
    {
        _context = context;
        _device = device;
        _sound = sound;
    }

    /// <summary>Gets whether this clip is currently playing. Paused clips return false.</summary>
    public bool IsPlaying
    {
        get
        {
            EnsureUsable();
            return Raylib.IsSoundPlaying(_sound);
        }
    }

    /// <summary>Gets or sets this clip's volume, from zero (silent) to one (full volume).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite or is outside the range zero to one.</exception>
    public float Volume
    {
        get
        {
            EnsureUsable();
            return _volume;
        }
        set
        {
            EnsureUsable();
            AudioDevice.ValidateVolume(value);
            Raylib.SetSoundVolume(_sound, value);
            _volume = value;
        }
    }

    /// <summary>Plays this clip from the beginning, restarting it if it was already playing.</summary>
    public void Play()
    {
        EnsureUsable();
        Raylib.PlaySound(_sound);
    }

    /// <summary>Stops playback and returns the clip to its beginning.</summary>
    public void Stop()
    {
        EnsureUsable();
        Raylib.StopSound(_sound);
    }

    /// <summary>Pauses playback while retaining the current position.</summary>
    public void Pause()
    {
        EnsureUsable();
        Raylib.PauseSound(_sound);
    }

    /// <summary>Resumes a paused clip from its current position.</summary>
    public void Resume()
    {
        EnsureUsable();
        Raylib.ResumeSound(_sound);
    }

    /// <summary>Stops playback and releases the native sound. Repeated calls have no effect.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _context.EnsureActive();
        Raylib.UnloadSound(_sound);
        _disposed = true;
        _context.Untrack(this);
    }

    private void EnsureUsable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _device.EnsureAvailable();
    }
}
