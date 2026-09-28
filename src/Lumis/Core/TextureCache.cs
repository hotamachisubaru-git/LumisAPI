using System.Collections.Generic;

namespace Lumis;

/// <summary>Caches textures loaded by path to avoid duplicate file reads.</summary>
/// <remarks>
/// Thread-affine to the game thread only. All access must come from the thread that runs the game.
/// The cache holds strong references to loaded <see cref="Texture"/> instances.
/// </remarks>
public sealed class TextureCache : IDisposable
{
    private readonly Dictionary<string, Texture> _cache = new();
    private bool _disposed;

    /// <summary>Gets the number of textures currently stored in the cache.</summary>
    public int Count => _cache.Count;

    /// <summary>Gets the number of cache hits (paths that were already loaded).</summary>
    public int HitCount { get; private set; }

    /// <summary>Gets the number of cache misses (paths that required a new load).</summary>
    public int MissCount { get; private set; }

    /// <summary>Loads a texture by path, returning a cached instance if available.</summary>
    /// <param name="graphics">The <see cref="Graphics2D"/> instance used to load the texture.</param>
    /// <param name="path">An image file path. Relative paths resolve from the current working directory.</param>
    /// <returns>The cached or newly loaded <see cref="Texture"/>.</returns>
    /// <exception cref="ObjectDisposedException">The cache has been disposed.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="IOException">The backend cannot decode or upload the image.</exception>
    public Texture Load(Graphics2D graphics, string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string key = Path.GetFullPath(path);

        if (_cache.TryGetValue(key, out Texture? cached))
        {
            HitCount++;
            return cached;
        }

        MissCount++;
        var texture = graphics.LoadTexture(path);
        _cache[key] = texture;
        return texture;
    }

    /// <summary>Attempts to get a cached texture by path without loading.</summary>
    /// <param name="path">An image file path.</param>
    /// <param name="texture">The cached <see cref="Texture"/>, or null if not found.</param>
    /// <returns>True if the texture was found in the cache.</returns>
    public bool TryGet(string path, out Texture? texture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string key = Path.GetFullPath(path);
        return _cache.TryGetValue(key, out texture);
    }

    /// <summary>Removes a texture from the cache by path. Does not dispose it.</summary>
    /// <param name="path">The path of the texture to remove.</param>
    public void Remove(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cache.Remove(Path.GetFullPath(path));
    }

    /// <summary>Clears all textures from the cache. Does not dispose them.</summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cache.Clear();
    }

    /// <summary>Disposes all cached textures and clears the cache.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var texture in _cache.Values)
            texture.Dispose();

        _cache.Clear();
        _disposed = true;
    }
}
