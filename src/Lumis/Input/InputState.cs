using System.Numerics;
using NativeRaylib = Raylib_cs.Raylib;
using NativeKey = Raylib_cs.KeyboardKey;
using NativeMouseButton = Raylib_cs.MouseButton;

namespace Lumis;

/// <summary>Polls keyboard and mouse input for the current frame.</summary>
/// <remarks>Use on the game thread while the window is open. Pressed and released states remain true for the whole frame and are not consumed by queries.</remarks>
public sealed class InputState
{
    private readonly GameContext _context;

    internal InputState(GameContext context) => _context = context;

    /// <summary>Gets the mouse position relative to the top-left corner of the client area, in screen pixels.</summary>
    public Vector2 MousePosition
    {
        get
        {
            _context.EnsureActive();
            return NativeRaylib.GetMousePosition();
        }
    }

    /// <summary>Gets the mouse movement since the previous input poll, in pixels.</summary>
    public Vector2 MouseDelta
    {
        get
        {
            _context.EnsureActive();
            return NativeRaylib.GetMouseDelta();
        }
    }

    /// <summary>Gets horizontal and vertical scroll amounts for the current frame.</summary>
    /// <remarks>Positive Y indicates upward scrolling. Values can be fractional on trackpads.</remarks>
    public Vector2 MouseWheelDelta
    {
        get
        {
            _context.EnsureActive();
            return NativeRaylib.GetMouseWheelMoveV();
        }
    }

    /// <summary>Gets whether the cursor is inside the game window's client area.</summary>
    public bool IsCursorOnScreen
    {
        get
        {
            _context.EnsureActive();
            return NativeRaylib.IsCursorOnScreen();
        }
    }

    /// <summary>Tests whether a key is currently held down.</summary>
    /// <param name="key">The key to query.</param>
    public bool IsKeyDown(Key key)
    {
        _context.EnsureActive();
        return NativeRaylib.IsKeyDown(ToNative(key));
    }

    /// <summary>Tests whether a key changed from released to pressed this frame, excluding key repeat.</summary>
    /// <param name="key">The key to query.</param>
    public bool IsKeyPressed(Key key)
    {
        _context.EnsureActive();
        return NativeRaylib.IsKeyPressed(ToNative(key));
    }

    /// <summary>Tests whether a key changed from pressed to released this frame.</summary>
    /// <param name="key">The key to query.</param>
    public bool IsKeyReleased(Key key)
    {
        _context.EnsureActive();
        return NativeRaylib.IsKeyReleased(ToNative(key));
    }

    /// <summary>Tests whether a key is currently released.</summary>
    /// <param name="key">The key to query.</param>
    public bool IsKeyUp(Key key)
    {
        _context.EnsureActive();
        return NativeRaylib.IsKeyUp(ToNative(key));
    }

    /// <summary>Tests whether a mouse button is currently held down.</summary>
    /// <param name="button">The mouse button to query.</param>
    public bool IsMouseButtonDown(MouseButton button)
    {
        _context.EnsureActive();
        return NativeRaylib.IsMouseButtonDown(ToNative(button));
    }

    /// <summary>Tests whether a mouse button changed from released to pressed this frame.</summary>
    /// <param name="button">The mouse button to query.</param>
    public bool IsMouseButtonPressed(MouseButton button)
    {
        _context.EnsureActive();
        return NativeRaylib.IsMouseButtonPressed(ToNative(button));
    }

    /// <summary>Tests whether a mouse button changed from pressed to released this frame.</summary>
    /// <param name="button">The mouse button to query.</param>
    public bool IsMouseButtonReleased(MouseButton button)
    {
        _context.EnsureActive();
        return NativeRaylib.IsMouseButtonReleased(ToNative(button));
    }

    /// <summary>Tests whether a mouse button is currently released.</summary>
    /// <param name="button">The mouse button to query.</param>
    public bool IsMouseButtonUp(MouseButton button)
    {
        _context.EnsureActive();
        return NativeRaylib.IsMouseButtonUp(ToNative(button));
    }

    private static NativeKey ToNative(Key key)
    {
        if (!Enum.IsDefined(key))
            throw new ArgumentOutOfRangeException(nameof(key), key, "The key is not supported.");
        return (NativeKey)key;
    }

    private static NativeMouseButton ToNative(MouseButton button)
    {
        if (!Enum.IsDefined(button))
            throw new ArgumentOutOfRangeException(nameof(button), button, "The mouse button is not supported.");
        return (NativeMouseButton)button;
    }
}
