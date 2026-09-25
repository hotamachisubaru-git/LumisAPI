using Raylib_cs;

namespace Lumis;

/// <summary>Loads and controls audio belonging to a running game.</summary>
/// <remarks>Call audio operations on the thread that runs the game. Music streams are updated automatically.</remarks>
public sealed class AudioDevice
{
    private readonly GameContext _context;
    private readonly HashSet<MusicTrack> _music = [];
    private bool _initialized;

    internal AudioDevice(GameContext context) => _context = context;

    /// <summary>Gets whether the game has an initialized audio device.</summary>
    public bool IsAvailable => _context.IsActive && _initialized;

    /// <summary>Gets or sets the overall output volume, from zero (silent) to one (full volume).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite or is outside the range zero to one.</exception>
    public float MasterVolume
    {
        get
        {
            EnsureAvailable();
            return Raylib.GetMasterVolume();
        }
        set
        {
            EnsureAvailable();
            ValidateVolume(value);
            Raylib.SetMasterVolume(value);
        }
    }

    /// <summary>Loads a short sound into memory. Dispose the returned clip when it is no longer needed.</summary>
    /// <param name="path">A file path, relative to the current working directory or absolute.</param>
    /// <returns>A sound clip owned by this game.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file cannot be decoded into a valid sound.</exception>
    /// <exception cref="InvalidOperationException">The game is not running, the thread is wrong, or audio is unavailable.</exception>
    public SoundClip LoadSound(string path)
    {
        EnsureAvailable();
        string fullPath = ResolveFile(path);
        Sound sound = Raylib.LoadSound(fullPath);
        try
        {
            if (!Raylib.IsSoundValid(sound))
                throw new InvalidDataException($"The audio file could not be loaded as a sound: {fullPath}");

            var clip = new SoundClip(_context, this, sound);
            _context.Track(clip);
            return clip;
        }
        catch
        {
            Raylib.UnloadSound(sound);
            throw;
        }
    }

    /// <summary>Opens a music file for streaming. Dispose the returned track when it is no longer needed.</summary>
    /// <param name="path">A file path, relative to the current working directory or absolute.</param>
    /// <returns>A music track owned by this game, with looping enabled by default.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file cannot be decoded into a valid music stream.</exception>
    /// <exception cref="InvalidOperationException">The game is not running, the thread is wrong, or audio is unavailable.</exception>
    public MusicTrack LoadMusic(string path)
    {
        EnsureAvailable();
        string fullPath = ResolveFile(path);
        Music music = Raylib.LoadMusicStream(fullPath);
        MusicTrack? track = null;
        try
        {
            // IsMusicValid checks decoder metadata; the stream buffer needs its own check.
            if (!Raylib.IsMusicValid(music) || !Raylib.IsAudioStreamValid(music.Stream))
                throw new InvalidDataException($"The audio file could not be loaded as music: {fullPath}");

            track = new MusicTrack(_context, this, music);
            _context.Track(track);
            _music.Add(track);
            return track;
        }
        catch
        {
            if (track is not null)
            {
                _context.Untrack(track);
                _music.Remove(track);
            }
            Raylib.UnloadMusicStream(music);
            throw;
        }
    }

    internal void Initialize(bool enabled)
    {
        _context.EnsureActive();
        if (!enabled || _initialized)
            return;

        Raylib.InitAudioDevice();
        if (!Raylib.IsAudioDeviceReady())
            throw new InvalidOperationException("The default audio output device could not be initialized. Check the output device or disable audio in the game options.");

        _initialized = true;
        try
        {
            Raylib.SetMasterVolume(1f);
        }
        catch
        {
            Raylib.CloseAudioDevice();
            _initialized = false;
            throw;
        }
    }

    internal void UpdateStreams()
    {
        _context.EnsureActive();
        if (!_initialized)
            return;

        foreach (MusicTrack track in _music)
            track.UpdateStream();
    }

    internal void Shutdown()
    {
        if (!_initialized)
            return;

        _context.EnsureActive();
        // GameContext releases its tracked resources before the device is closed.
        Raylib.CloseAudioDevice();
        _initialized = false;
        _music.Clear();
    }

    internal void EnsureAvailable()
    {
        _context.EnsureActive();
        if (!_initialized)
            throw new InvalidOperationException("Audio is unavailable. Enable audio in the game options before running the game.");
    }

    internal void Unregister(MusicTrack track) => _music.Remove(track);

    internal static void ValidateVolume(float value)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Volume must be finite and between zero and one.");
    }

    private static string ResolveFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The audio file does not exist.", fullPath);
        return fullPath;
    }
}
