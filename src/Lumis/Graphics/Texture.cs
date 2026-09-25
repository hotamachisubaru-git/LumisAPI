using NativeRaylib = Raylib_cs.Raylib;
using NativeTexture = Raylib_cs.Texture2D;

namespace Lumis;

/// <summary>A GPU texture owned by the game that loaded it.</summary>
/// <remarks>Dispose on the game thread. Remaining textures are released automatically before the game window closes.</remarks>
public sealed class Texture : IDisposable
{
    private readonly GameContext _context;
    private readonly NativeTexture _texture;

    internal Texture(GameContext context, NativeTexture texture)
    {
        _context = context;
        _texture = texture;
        Width = texture.Width;
        Height = texture.Height;
        try
        {
            context.Track(this);
        }
        catch
        {
            NativeRaylib.UnloadTexture(texture);
            throw;
        }
    }

    /// <summary>Gets the original texture width in pixels, including after disposal.</summary>
    public int Width { get; }

    /// <summary>Gets the original texture height in pixels, including after disposal.</summary>
    public int Height { get; }

    /// <summary>Gets whether the texture has been released.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Releases the GPU texture. Calling this again after disposal has no effect.</summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        _context.EnsureActive();
        if (_context.IsDrawing)
            Raylib_cs.Rlgl.DrawRenderBatchActive();
        NativeRaylib.UnloadTexture(_texture);
        IsDisposed = true;
        _context.Untrack(this);
    }

    internal NativeTexture GetNative(GameContext context)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!ReferenceEquals(_context, context))
            throw new InvalidOperationException("The texture belongs to a different game.");

        _context.EnsureActive();
        return _texture;
    }
}
