using System.Collections.Generic;

namespace Lumis;

/// <summary>
/// Manages game assets (textures, sound clips, and music tracks) centrally as a singleton.
/// <remarks>
/// All access must come from the game thread. The singleton is not thread-safe.
/// </remarks>
/// </summary>
public sealed class AssetManager : IDisposable
{
    private static AssetManager? _instance;
    private readonly TextureCache _textureCache = new();
    private readonly AudioCache _audioCache = new();
    private bool _initialized;
    private string _basePath = string.Empty;
    private bool _disposed;

    /// <summary>
    /// Gets the singleton instance. Initializes it automatically if it is null.
    /// </summary>
    public static AssetManager Instance
    {
        get => _instance ??= new AssetManager();
    }

    /// <summary>
    /// Gets whether this manager has been initialized with a base path.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Gets the base directory path used to resolve relative asset paths.
    /// </summary>
    public string BasePath => _basePath;

    /// <summary>
    /// Gets the total number of assets currently loaded (textures + sounds + music).
    /// </summary>
    public int TotalAssetsLoaded => _textureCache.Count + _audioCache.SoundCount + _audioCache.MusicCount;

    /// <summary>
    /// Initializes the manager with a base path.
    /// </summary>
    /// <param name="basePath">
    /// The base directory path. Relative asset paths are resolved from this directory.
    /// </param>
    /// <exception cref="DirectoryNotFoundException">
    /// The specified base path does not exist.
    /// </exception>
    public void Initialize(string basePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        string fullPath = Path.GetFullPath(basePath);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"The base path does not exist: '{fullPath}'.");

        _basePath = fullPath;
        _initialized = true;
    }

    /// <summary>
    /// Initializes the manager using the current working directory as the base path.
    /// </summary>
    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Initialize(Directory.GetCurrentDirectory());
    }

    /// <summary>
    /// Loads a texture by path, returning a cached instance if available.
    /// </summary>
    /// <param name="graphics">
    /// The <see cref="Graphics2D"/> instance used to load the texture.
    /// </param>
    /// <param name="path">
    /// A relative path from the base directory, or an absolute path.
    /// </param>
    /// <returns>
    /// The cached or newly loaded <see cref="Texture"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// The manager has been disposed.
    /// </exception>
    public Texture LoadTexture(Graphics2D graphics, string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string resolvedPath = ResolvePath(path);
        return _textureCache.Load(graphics, resolvedPath);
    }

    /// <summary>
    /// Loads a sound clip by path, returning a cached instance if available.
    /// </summary>
    /// <param name="device">
    /// The <see cref="AudioDevice"/> instance used to load the sound.
    /// </param>
    /// <param name="path">
    /// A relative path from the base directory, or an absolute path.
    /// </param>
    /// <returns>
    /// The cached or newly loaded <see cref="SoundClip"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// The manager has been disposed.
    /// </exception>
    public SoundClip LoadSound(AudioDevice device, string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string resolvedPath = ResolvePath(path);
        return _audioCache.LoadSound(device, resolvedPath);
    }

    /// <summary>
    /// Loads a music track by path, returning a cached instance if available.
    /// </summary>
    /// <param name="device">
    /// The <see cref="AudioDevice"/> instance used to load the music.
    /// </param>
    /// <param name="path">
    /// A relative path from the base directory, or an absolute path.
    /// </param>
    /// <returns>
    /// The cached or newly loaded <see cref="MusicTrack"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// The manager has been disposed.
    /// </exception>
    public MusicTrack LoadMusic(AudioDevice device, string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string resolvedPath = ResolvePath(path);
        return _audioCache.LoadMusic(device, resolvedPath);
    }

    /// <summary>
    /// Removes a cached texture by path. Does not dispose it.
    /// </summary>
    /// <param name="path">
    /// A relative path from the base directory, or an absolute path.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// The manager has been disposed.
    /// </exception>
    public void UnloadTexture(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string resolvedPath = ResolvePath(path);
        _textureCache.Remove(resolvedPath);
    }

    /// <summary>
    /// Removes a cached sound clip by path. Does not dispose it.
    /// </summary>
    /// <param name="path">
    /// A relative path from the base directory, or an absolute path.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// The manager has been disposed.
    /// </exception>
    public void UnloadSound(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string resolvedPath = ResolvePath(path);
        _audioCache.RemoveSound(resolvedPath);
    }

    /// <summary>
    /// Removes a cached music track by path. Does not dispose it.
    /// </summary>
    /// <param name="path">
    /// A relative path from the base directory, or an absolute path.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// The manager has been disposed.
    /// </exception>
    public void UnloadMusic(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string resolvedPath = ResolvePath(path);
        _audioCache.RemoveMusic(resolvedPath);
    }

    /// <summary>
    /// Removes all cached assets (textures, sounds, and music). Does not dispose them.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The manager has been disposed.
    /// </exception>
    public void UnloadAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _textureCache.Clear();
        _audioCache.Clear();
    }

    /// <summary>
    /// Disposes all cached assets and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _textureCache.Dispose();
        _audioCache.Dispose();
        _disposed = true;
    }

    private string ResolvePath(string path)
    {
        if (!_initialized)
            throw new InvalidOperationException("AssetManager is not initialized. Call Initialize() first.");

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(_basePath, path));
    }
}
