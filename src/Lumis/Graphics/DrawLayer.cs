namespace Lumis;

/// <summary>Represents a drawing layer for z-sorted rendering.</summary>
/// <remarks>
/// <para>Layers are immutable once created. Use the static factory properties or <see cref="Create"/> to obtain instances.</para>
/// <para>Smaller <see cref="Depth"/> values are rendered behind larger ones.</para>
/// </remarks>
public sealed class DrawLayer
{
    private readonly string _name;

    internal DrawLayer(string name, int depth)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        Depth = depth;
    }

    /// <summary>Gets the layer name.</summary>
    public string Name => _name;

    /// <summary>Gets the layer depth. Smaller values are behind larger ones.</summary>
    public int Depth { get; }

    /// <summary>Gets the background layer (depth 0).</summary>
    public static DrawLayer Background { get; } = new("Background", 0);

    /// <summary>Gets the foreground layer (depth 1).</summary>
    public static DrawLayer Foreground { get; } = new("Foreground", 1);

    /// <summary>Gets the UI layer (depth 2).</summary>
    public static DrawLayer UI { get; } = new("UI", 2);

    /// <summary>Creates a custom drawing layer.</summary>
    /// <param name="name">The layer name. Must not be null or empty.</param>
    /// <param name="depth">The layer depth. Smaller values are rendered behind larger ones.</param>
    /// <returns>A new <see cref="DrawLayer"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    public static DrawLayer Create(string name, int depth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new DrawLayer(name, depth);
    }

    /// <summary>Tests whether this object is equal to another <see cref="DrawLayer"/>.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>true if the other object is a <see cref="DrawLayer"/> with the same name; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not DrawLayer other)
            return false;
        return _name == other._name;
    }

    /// <summary>Returns the hash code based on the layer name.</summary>
    public override int GetHashCode() => _name.GetHashCode();

    /// <summary>Returns a string in the form "DrawLayer{Name}".</summary>
    public override string ToString() => $"DrawLayer{{{Name}}}";
}
