namespace Lumis;

/// <summary>An immutable color with red, green, blue, and alpha channels.</summary>
public readonly record struct Color
{
    /// <summary>Creates a color. Alpha defaults to fully opaque.</summary>
    /// <param name="r">Red channel, from 0 to 255.</param>
    /// <param name="g">Green channel, from 0 to 255.</param>
    /// <param name="b">Blue channel, from 0 to 255.</param>
    /// <param name="a">Alpha channel, from transparent (0) to opaque (255).</param>
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>Gets the red channel.</summary>
    public byte R { get; }

    /// <summary>Gets the green channel.</summary>
    public byte G { get; }

    /// <summary>Gets the blue channel.</summary>
    public byte B { get; }

    /// <summary>Gets the alpha channel.</summary>
    public byte A { get; }

    /// <summary>Gets transparent black.</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    /// <summary>Gets opaque white.</summary>
    public static Color White => new(255, 255, 255);

    /// <summary>Gets opaque black.</summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>Gets opaque light gray.</summary>
    public static Color LightGray => new(200, 200, 200);

    /// <summary>Gets opaque gray.</summary>
    public static Color Gray => new(128, 128, 128);

    /// <summary>Gets opaque dark gray.</summary>
    public static Color DarkGray => new(64, 64, 64);

    /// <summary>Gets opaque red.</summary>
    public static Color Red => new(255, 0, 0);

    /// <summary>Gets opaque green.</summary>
    public static Color Green => new(0, 255, 0);

    /// <summary>Gets opaque blue.</summary>
    public static Color Blue => new(0, 0, 255);

    /// <summary>Gets opaque yellow.</summary>
    public static Color Yellow => new(255, 255, 0);

    /// <summary>Gets opaque orange.</summary>
    public static Color Orange => new(255, 165, 0);

    /// <summary>Gets opaque cyan.</summary>
    public static Color Cyan => new(0, 255, 255);

    /// <summary>Gets opaque magenta.</summary>
    public static Color Magenta => new(255, 0, 255);

    /// <summary>Gets opaque purple.</summary>
    public static Color Purple => new(128, 0, 128);

    /// <summary>Gets opaque pink.</summary>
    public static Color Pink => new(255, 192, 203);

    /// <summary>Returns the same RGB channels with a different alpha channel.</summary>
    /// <param name="alpha">The replacement alpha channel.</param>
    public Color WithAlpha(byte alpha) => new(R, G, B, alpha);

    internal Raylib_cs.Color ToNative() => new(R, G, B, A);
}
