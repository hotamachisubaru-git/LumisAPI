using System.Collections.Generic;

namespace Lumis;

/// <summary>Caches audio clips and music tracks by path to avoid duplicate file reads.</summary>
/// <remarks>
/// Thread-affine to the game thread only. All access must come from the thread that runs the game.
/// The cache holds strong references to loaded <see cref="SoundClip"/> and <see cref="MusicTrack"/> instances.
/// </remarks>
public sealed class AudioCache : IDisposable
{
    private readonly Dictionary<string, SoundClip> _sounds = new();
    private readonly Dictionary<string, MusicTrack> _music = new();
    private bool _disposed;

    /// <summary>Gets the number of sound clips currently stored in the cache.</summary>
    public int SoundCount => _sounds.Count;

    /// <summary>Gets the number of music tracks currently stored in the cache.</summary>
    public int MusicCount => _music.Count;

    /// <summary>Loads a sound clip by path, returning a cached instance if available.</summary>
    /// <param name="device">The <see cref="AudioDevice"/> instance used to load the sound.</param>
    /// <param name="path">A file path, relative to the current working directory or absolute.</param>
    /// <returns>The cached or newly loaded <see cref="SoundClip"/>.</returns>
    /// <exception cref="ObjectDisposedException">The cache has been disposed.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file cannot be decoded into a valid sound.</exception>
    /// <exception cref="InvalidOperationException">The game is not running or audio is unavailable.</exception>
    public SoundClip LoadSound(AudioDevice device, string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string key = Path.GetFullPath(path);

        if (_sounds.TryGetValue(key, out SoundClip? cached))
            return cached;

        var clip = device.LoadSound(path);
        _sounds[key] = clip;
        return clip;
    }

    /// <summary>Loads a music track by path, returning a cached instance if available.</summary>
    /// <param name="device">The <see cref="AudioDevice"/> instance used to load the music.</param>
    /// <param name="path">A file path, relative to the current working directory or absolute.</param>
    /// <returns>The cached or newly loaded <see cref="MusicTrack"/>.</returns>
    /// <exception cref="ObjectDisposedException">The cache has been disposed.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file cannot be decoded into a valid music stream.</exception>
    /// <exception cref="InvalidOperationException">The game is not running or audio is unavailable.</exception>
    public MusicTrack LoadMusic(AudioDevice device, string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string key = Path.GetFullPath(path);

        if (_music.TryGetValue(key, out MusicTrack? cached))
            return cached;

        var track = device.LoadMusic(path);
        _music[key] = track;
        return track;
    }

    /// <summary>Attempts to get a cached sound clip by path without loading.</summary>
    /// <param name="path">A file path.</param>
    /// <param name="clip">The cached <see cref="SoundClip"/>, or null if not found.</param>
    /// <returns>True if the sound clip was found in the cache.</returns>
    public bool TryGetSound(string path, out SoundClip? clip)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string key = Path.GetFullPath(path);
        return _sounds.TryGetValue(key, out clip);
    }

    /// <summary>Attempts to get a cached music track by path without loading.</summary>
    /// <param name="path">A file path.</param>
    /// <param name="track">The cached <see cref="MusicTrack"/>, or null if not found.</param>
    /// <returns>True if the music track was found in the cache.</returns>
    public bool TryGetMusic(string path, out MusicTrack? track)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string key = Path.GetFullPath(path);
        return _music.TryGetValue(key, out track);
    }

    /// <summary>Removes a sound clip from the cache by path. Does not dispose it.</summary>
    /// <param name="path">The path of the sound clip to remove.</param>
    public void RemoveSound(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _sounds.Remove(Path.GetFullPath(path));
    }

    /// <summary>Removes a music track from the cache by path. Does not dispose it.</summary>
    /// <param name="path">The path of the music track to remove.</param>
    public void RemoveMusic(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _music.Remove(Path.GetFullPath(path));
    }

    /// <summary>Clears all sound clips and music tracks from the cache. Does not dispose them.</summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _sounds.Clear();
        _music.Clear();
    }

    /// <summary>Disposes all cached sound clips and music tracks, then clears the cache.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var clip in _sounds.Values)
            clip.Dispose();
        _sounds.Clear();

        foreach (var track in _music.Values)
            track.Dispose();
        _music.Clear();

        _disposed = true;
    }
}
