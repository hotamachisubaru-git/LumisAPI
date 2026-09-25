using Raylib_cs;

namespace Lumis;

/// <summary>A streamed music track whose buffers are filled automatically by the game loop.</summary>
/// <remarks>
/// Call its members, including disposal, on the game thread. The source file must remain available
/// until the track is disposed. The game disposes remaining tracks on shutdown.
/// </remarks>
public sealed class MusicTrack : IDisposable
{
    private readonly GameContext _context;
    private readonly AudioDevice _device;
    private Music _music;
    private float _volume = 1f;
    private bool _disposed;

    internal MusicTrack(GameContext context, AudioDevice device, Music music)
    {
        _context = context;
        _device = device;
        _music = music;
    }

    /// <summary>Gets whether this track is currently playing. Paused tracks return false.</summary>
    public bool IsPlaying
    {
        get
        {
            EnsureUsable();
            return Raylib.IsMusicStreamPlaying(_music);
        }
    }

    /// <summary>Gets or sets whether playback repeats after reaching the end. Defaults to true.</summary>
    public bool IsLooping
    {
        get
        {
            EnsureUsable();
            return _music.Looping;
        }
        set
        {
            EnsureUsable();
            _music.Looping = value;
        }
    }

    /// <summary>Gets the duration reported by the music decoder.</summary>
    public TimeSpan Duration
    {
        get
        {
            EnsureUsable();
            return TimeSpan.FromSeconds(Raylib.GetMusicTimeLength(_music));
        }
    }

    /// <summary>Gets the current playback position reported by the music decoder.</summary>
    public TimeSpan Position
    {
        get
        {
            EnsureUsable();
            return TimeSpan.FromSeconds(Raylib.GetMusicTimePlayed(_music));
        }
    }

    /// <summary>Gets or sets this track's volume, from zero (silent) to one (full volume).</summary>
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
            Raylib.SetMusicVolume(_music, value);
            _volume = value;
        }
    }

    /// <summary>Plays this track from the beginning, restarting any current playback.</summary>
    public void Play()
    {
        EnsureUsable();
        Raylib.StopMusicStream(_music);
        Raylib.PlayMusicStream(_music);
        Raylib.UpdateMusicStream(_music);
    }

    /// <summary>Stops playback and returns the track to its beginning.</summary>
    public void Stop()
    {
        EnsureUsable();
        Raylib.StopMusicStream(_music);
    }

    /// <summary>Pauses playback while retaining the current position.</summary>
    public void Pause()
    {
        EnsureUsable();
        Raylib.PauseMusicStream(_music);
    }

    /// <summary>Resumes a paused track from its current position.</summary>
    public void Resume()
    {
        EnsureUsable();
        Raylib.ResumeMusicStream(_music);
    }

    /// <summary>Stops playback and releases the music stream. Repeated calls have no effect.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _context.EnsureActive();
        Raylib.UnloadMusicStream(_music);
        _disposed = true;
        _device.Unregister(this);
        _context.Untrack(this);
    }

    internal void UpdateStream()
    {
        EnsureUsable();
        Raylib.UpdateMusicStream(_music);
    }

    private void EnsureUsable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _device.EnsureAvailable();
    }
}
