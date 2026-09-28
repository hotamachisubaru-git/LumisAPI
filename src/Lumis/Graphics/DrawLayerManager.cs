using System.Collections.Generic;

namespace Lumis;

/// <summary>Manages drawing layers and renders them in z-sorted order.</summary>
/// <remarks>
/// <para>This class is intended for use on the game thread only.</para>
/// <para>Layers are automatically sorted by depth when registered or unregistered.</para>
/// </remarks>
public sealed class DrawLayerManager
{
    private readonly List<DrawLayer> _layers = new();

    /// <summary>Gets the number of registered layers.</summary>
    public int LayerCount => _layers.Count;

    /// <summary>Gets all registered layers sorted by depth in ascending order.</summary>
    public DrawLayer[] RegisteredLayers
    {
        get
        {
            _layers.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            return _layers.ToArray();
        }
    }

    /// <summary>Registers a drawing layer.</summary>
    /// <param name="layer">The layer to register. Must not be null.</param>
    /// <remarks>If the layer is already registered, it is not added again.</remarks>
    public void RegisterLayer(DrawLayer layer)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));
        if (!_layers.Contains(layer))
            _layers.Add(layer);
    }

    /// <summary>Unregisters a drawing layer.</summary>
    /// <param name="layer">The layer to unregister. Must not be null.</param>
    public void UnregisterLayer(DrawLayer layer)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));
        _layers.Remove(layer);
    }

    /// <summary>Draws all registered layers in depth-sorted order.</summary>
    /// <param name="graphics">The graphics context to draw with.</param>
    /// <param name="drawCallback">A callback invoked for each layer in ascending depth order.</param>
    /// <remarks>
    /// <para>Layers are sorted by depth before drawing. Each layer is passed to <paramref name="drawCallback"/> in order.</para>
    /// <para>This method is intended for use on the game thread only.</para>
    /// </remarks>
    public void DrawLayered(Graphics2D graphics, Action<DrawLayer, Graphics2D> drawCallback)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(drawCallback);

        var sorted = RegisteredLayers;
        for (int i = 0; i < sorted.Length; i++)
        {
            drawCallback(sorted[i], graphics);
        }
    }

    /// <summary>Draws all registered layers in depth-sorted order with shared state.</summary>
    /// <typeparam name="T">The type of the shared state object.</typeparam>
    /// <param name="graphics">The graphics context to draw with.</param>
    /// <param name="drawCallback">A callback invoked for each layer with the shared state.</param>
    /// <param name="state">A shared state object passed to every invocation of <paramref name="drawCallback"/>.</param>
    /// <remarks>
    /// <para>Layers are sorted by depth before drawing. Each layer is passed to <paramref name="drawCallback"/> in order along with <paramref name="state"/>.</para>
    /// <para>This method is intended for use on the game thread only.</para>
    /// </remarks>
    public void DrawLayered<T>(Graphics2D graphics, Action<DrawLayer, Graphics2D, T> drawCallback, T state)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(drawCallback);

        var sorted = RegisteredLayers;
        for (int i = 0; i < sorted.Length; i++)
        {
            drawCallback(sorted[i], graphics, state);
        }
    }
}
