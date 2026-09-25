using System.Numerics;
using NativeRaylib = Raylib_cs.Raylib;

namespace Lumis;

/// <summary>Loads textures and draws text, shapes, and sprites in screen coordinates.</summary>
/// <remarks>Drawing is allowed only during the game's draw callback, on its owning thread.</remarks>
public sealed class Graphics2D
{
    private readonly GameContext _context;

    internal Graphics2D(GameContext context) => _context = context;

    /// <summary>Clears the current frame to a color.</summary>
    /// <param name="color">The background color.</param>
    public void Clear(Color color)
    {
        _context.EnsureDrawing();
        // Earlier draw calls must reach the framebuffer before a mid-frame clear.
        Raylib_cs.Rlgl.DrawRenderBatchActive();
        NativeRaylib.ClearBackground(color.ToNative());
    }

    /// <summary>Draws text using the built-in font, which primarily supports basic Latin characters.</summary>
    /// <param name="text">The text to draw. Newlines are supported.</param>
    /// <param name="position">The top-left position in pixels.</param>
    /// <param name="fontSize">The positive font height in pixels.</param>
    /// <param name="color">The text color.</param>
    public void DrawText(string text, Vector2 position, int fontSize, Color color)
    {
        _context.EnsureDrawing();
        ValidateText(text, fontSize);
        ValidateVector(position, nameof(position));
        NativeRaylib.DrawTextEx(NativeRaylib.GetFontDefault(), text, position, fontSize, TextSpacing(fontSize), color.ToNative());
    }

    /// <summary>Measures text with the same built-in font and spacing used by <see cref="DrawText"/>.</summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontSize">The positive font height in pixels.</param>
    /// <returns>The text width and height in pixels.</returns>
    public Vector2 MeasureText(string text, int fontSize)
    {
        _context.EnsureActive();
        ValidateText(text, fontSize);
        return NativeRaylib.MeasureTextEx(NativeRaylib.GetFontDefault(), text, fontSize, TextSpacing(fontSize));
    }

    /// <summary>Saves everything drawn so far in the current frame as a PNG image.</summary>
    /// <param name="path">The PNG output path. Its parent directory must already exist.</param>
    /// <remarks>Call at the end of a Draw callback, on the game thread. Queued drawing is flushed before capture. An existing file is overwritten.</remarks>
    public void CaptureScreenshot(string path)
    {
        _context.EnsureDrawing();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.Contains('\0'))
            throw new ArgumentException("The path must not contain a null character.", nameof(path));
        string fullPath = Path.GetFullPath(path);
        if (!string.Equals(Path.GetExtension(fullPath), ".png", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The screenshot path must end in .png.", nameof(path));
        string? directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"The screenshot directory does not exist: '{directory}'.");

        Raylib_cs.Rlgl.DrawRenderBatchActive();
        var screenshot = NativeRaylib.LoadImageFromScreen();
        try
        {
            if (!NativeRaylib.IsImageValid(screenshot))
                throw new IOException("Could not read the screen image from the graphics device.");
            if (!NativeRaylib.ExportImage(screenshot, fullPath))
                throw new IOException($"Could not save screenshot '{fullPath}'.");
        }
        finally
        {
            NativeRaylib.UnloadImage(screenshot);
        }
    }

    /// <summary>Draws a filled rectangle.</summary>
    /// <param name="rectangle">The rectangle in pixels.</param>
    /// <param name="color">The fill color.</param>
    public void DrawRectangle(Rect rectangle, Color color)
    {
        _context.EnsureDrawing();
        NativeRaylib.DrawRectangleRec(rectangle.ToNative(), color.ToNative());
    }

    /// <summary>Draws a rectangle outline.</summary>
    /// <param name="rectangle">The rectangle in pixels.</param>
    /// <param name="thickness">The positive outline thickness in pixels.</param>
    /// <param name="color">The outline color.</param>
    public void DrawRectangleLines(Rect rectangle, float thickness, Color color)
    {
        _context.EnsureDrawing();
        ValidatePositive(thickness, nameof(thickness));
        NativeRaylib.DrawRectangleLinesEx(rectangle.ToNative(), thickness, color.ToNative());
    }

    /// <summary>Draws a filled circle.</summary>
    /// <param name="center">The circle center in pixels.</param>
    /// <param name="radius">The nonnegative circle radius in pixels.</param>
    /// <param name="color">The fill color.</param>
    public void DrawCircle(Vector2 center, float radius, Color color)
    {
        _context.EnsureDrawing();
        ValidateVector(center, nameof(center));
        if (!float.IsFinite(radius) || radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "The radius must be finite and nonnegative.");
        NativeRaylib.DrawCircleV(center, radius, color.ToNative());
    }

    /// <summary>Draws a line between two points.</summary>
    /// <param name="start">The starting position in pixels.</param>
    /// <param name="end">The ending position in pixels.</param>
    /// <param name="thickness">The positive line thickness in pixels.</param>
    /// <param name="color">The line color.</param>
    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
    {
        _context.EnsureDrawing();
        ValidateVector(start, nameof(start));
        ValidateVector(end, nameof(end));
        ValidatePositive(thickness, nameof(thickness));
        NativeRaylib.DrawLineEx(start, end, thickness, color.ToNative());
    }

    /// <summary>Loads an image file into a GPU texture while the game window is open.</summary>
    /// <param name="path">An image file path. Relative paths resolve from the current working directory.</param>
    /// <returns>A texture owned by this game.</returns>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="IOException">The backend cannot decode or upload the image.</exception>
    public Texture LoadTexture(string path)
    {
        _context.EnsureActive();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.Contains('\0'))
            throw new ArgumentException("The path must not contain a null character.", nameof(path));
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The texture file was not found.", fullPath);

        var texture = NativeRaylib.LoadTexture(fullPath);
        if (!NativeRaylib.IsTextureValid(texture))
        {
            if (texture.Id != 0)
                NativeRaylib.UnloadTexture(texture);
            throw new IOException($"Could not load texture '{fullPath}'. Check the image format and graphics device.");
        }

        return new Texture(_context, texture);
    }

    /// <summary>Draws an entire texture at its original size.</summary>
    /// <param name="texture">A live texture loaded by this game.</param>
    /// <param name="position">The top-left position in pixels.</param>
    /// <param name="tint">An optional color tint; defaults to white.</param>
    public void DrawTexture(Texture texture, Vector2 position, Color? tint = null)
    {
        _context.EnsureDrawing();
        ArgumentNullException.ThrowIfNull(texture);
        ValidateVector(position, nameof(position));
        NativeRaylib.DrawTextureV(texture.GetNative(_context), position, (tint ?? Color.White).ToNative());
    }

    /// <summary>Draws an entire texture stretched to a destination rectangle.</summary>
    /// <param name="texture">A live texture loaded by this game.</param>
    /// <param name="destination">The destination rectangle in pixels.</param>
    /// <param name="tint">An optional color tint; defaults to white.</param>
    public void DrawTexture(Texture texture, Rect destination, Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        DrawTexture(texture, new Rect(0, 0, texture.Width, texture.Height), destination, Vector2.Zero, 0, tint);
    }

    /// <summary>Draws a texture region with scaling and clockwise rotation.</summary>
    /// <param name="texture">A live texture loaded by this game.</param>
    /// <param name="source">The region within the texture, in texture pixels.</param>
    /// <param name="destination">The destination size and pivot position in screen pixels.</param>
    /// <param name="origin">The pivot offset from the destination's top left, in destination pixels.</param>
    /// <param name="rotation">The clockwise rotation in degrees around the pivot.</param>
    /// <param name="tint">An optional color tint; defaults to white.</param>
    /// <remarks>With a zero origin, the destination position is the top-left corner. With a centered origin, it is the sprite center.</remarks>
    public void DrawTexture(Texture texture, Rect source, Rect destination, Vector2 origin, float rotation, Color? tint = null)
    {
        _context.EnsureDrawing();
        ArgumentNullException.ThrowIfNull(texture);
        ValidateVector(origin, nameof(origin));
        if (!float.IsFinite(rotation))
            throw new ArgumentOutOfRangeException(nameof(rotation), "The rotation must be finite.");
        NativeRaylib.DrawTexturePro(texture.GetNative(_context), source.ToNative(), destination.ToNative(), origin, rotation, (tint ?? Color.White).ToNative());
    }

    private static float TextSpacing(int fontSize) => fontSize / 10f;

    private static void ValidateText(string text, int fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Contains('\0'))
            throw new ArgumentException("The text must not contain a null character.", nameof(text));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fontSize);
    }

    private static void ValidateVector(Vector2 vector, string parameterName)
    {
        if (!float.IsFinite(vector.X) || !float.IsFinite(vector.Y))
            throw new ArgumentOutOfRangeException(parameterName, "Both coordinates must be finite.");
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "The value must be finite and positive.");
    }
}
