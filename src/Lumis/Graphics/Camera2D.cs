using System.Numerics;

namespace Lumis;

/// <summary>2D camera that provides coordinate transformation between world and screen space.</summary>
/// <remarks>This class is not thread-safe. It must only be used from the game's main thread.</remarks>
public sealed class Camera2D
{
    private Vector2 _position;
    private Vector2 _target;
    private float _rotation;
    private float _zoom;
    private Vector2 _offset;
    private Vector2 _screenSize;
    private bool _isActive;

    /// <summary>Creates a new Camera2D instance.</summary>
    public Camera2D()
    {
        _target = Vector2.Zero;
        _rotation = 0f;
        _zoom = 1f;
        _offset = Vector2.Zero;
        _screenSize = Vector2.Zero;
        _isActive = false;
    }

    /// <summary>Gets or sets the camera position in world coordinates.</summary>
    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    /// <summary>Gets or sets the camera target (the point the camera looks at).</summary>
    /// <remarks>
    /// When <see cref="Target"/> is set, <see cref="Position"/> is updated so that
    /// the target aligns with <see cref="Offset"/>. Defaults to <see cref="Position"/>.
    /// </remarks>
    public Vector2 Target
    {
        get => _target;
        set
        {
            _target = value;
            _position = _target - _offset;
        }
    }

    /// <summary>Gets or sets the camera rotation in degrees (clockwise).</summary>
    public float Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }

    /// <summary>Gets or sets the camera zoom level. Default is 1.0.</summary>
    public float Zoom
    {
        get => _zoom;
        set => _zoom = value;
    }

    /// <summary>Gets or sets the screen offset. Default is the screen center.</summary>
    public Vector2 Offset
    {
        get => _offset;
        set => _offset = value;
    }

    /// <summary>Gets or sets the screen size in pixels.</summary>
    /// <remarks>
    /// This property must be set before <see cref="Offset"/> is used.
    /// When <see cref="Offset"/> is set, it defaults to the screen center.
    /// </remarks>
    public Vector2 ScreenSize
    {
        get => _screenSize;
        set
        {
            _screenSize = value;
            if (_offset == Vector2.Zero)
                _offset = new Vector2(_screenSize.X / 2f, _screenSize.Y / 2f);
        }
    }

    /// <summary>Gets whether the camera is currently active.</summary>
    /// <remarks>
    /// Set to <see langword="true"/> by <see cref="Begin"/> and to <see langword="false"/>
    /// by <see cref="End"/>. Graphics2D reads this flag to apply camera transformations during drawing.
    /// </remarks>
    public bool IsActive => _isActive;

    /// <summary>Converts a world-space position to screen-space coordinates.</summary>
    /// <param name="worldPosition">The position in world coordinates.</param>
    /// <returns>The corresponding screen position.</returns>
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        float rad = _rotation * (float)(System.Math.PI / 180.0);
        float cos = (float)System.Math.Cos(rad);
        float sin = (float)System.Math.Sin(rad);

        // (world - position) * zoom
        float dx = (worldPosition.X - _position.X) * _zoom;
        float dy = (worldPosition.Y - _position.Y) * _zoom;

        // Apply clockwise rotation
        float rx = dx * cos - dy * sin;
        float ry = dx * sin + dy * cos;

        return new Vector2(rx + _offset.X, ry + _offset.Y);
    }

    /// <summary>Converts a screen-space position to world-space coordinates.</summary>
    /// <param name="screenPosition">The position in screen coordinates.</param>
    /// <returns>The corresponding world position.</returns>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        float rad = _rotation * (float)(System.Math.PI / 180.0);
        float cos = (float)System.Math.Cos(rad);
        float sin = (float)System.Math.Sin(rad);

        // (screen - offset) / zoom
        float dx = (screenPosition.X - _offset.X) / _zoom;
        float dy = (screenPosition.Y - _offset.Y) / _zoom;

        // Apply inverse rotation (counter-clockwise, so use -rad or cos(-rad)=cos, sin(-rad)=-sin)
        float rx = dx * cos + dy * sin;
        float ry = -dx * sin + dy * cos;

        return new Vector2(rx + _position.X, ry + _position.Y);
    }

    /// <summary>Activates the camera.</summary>
    /// <remarks>
    /// Sets <see cref="IsActive"/> to <see langword="true"/>. Call this at the start
    /// of a draw frame. Graphics2D checks <see cref="IsActive"/> to apply transformations.
    /// </remarks>
    public void Begin() => _isActive = true;

    /// <summary>Deactivates the camera.</summary>
    /// <remarks>
    /// Sets <see cref="IsActive"/> to <see langword="false"/>. Call this at the end
    /// of a draw frame.
    /// </remarks>
    public void End() => _isActive = false;
}
