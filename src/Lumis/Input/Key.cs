namespace Lumis;

/// <summary>Desktop keyboard keys, using the backend's US keyboard layout mapping.</summary>
public enum Key
{
    /// <summary>No key.</summary>
    None = 0,
    /// <summary>The space bar.</summary>
    Space = 32,
    /// <summary>The apostrophe key.</summary>
    Apostrophe = 39,
    /// <summary>The comma key.</summary>
    Comma = 44,
    /// <summary>The minus key.</summary>
    Minus = 45,
    /// <summary>The period key.</summary>
    Period = 46,
    /// <summary>The slash key.</summary>
    Slash = 47,
    /// <summary>The top-row 0 key.</summary>
    D0 = 48,
    /// <summary>The top-row 1 key.</summary>
    D1 = 49,
    /// <summary>The top-row 2 key.</summary>
    D2 = 50,
    /// <summary>The top-row 3 key.</summary>
    D3 = 51,
    /// <summary>The top-row 4 key.</summary>
    D4 = 52,
    /// <summary>The top-row 5 key.</summary>
    D5 = 53,
    /// <summary>The top-row 6 key.</summary>
    D6 = 54,
    /// <summary>The top-row 7 key.</summary>
    D7 = 55,
    /// <summary>The top-row 8 key.</summary>
    D8 = 56,
    /// <summary>The top-row 9 key.</summary>
    D9 = 57,
    /// <summary>The semicolon key.</summary>
    Semicolon = 59,
    /// <summary>The equals key.</summary>
    Equal = 61,
    /// <summary>The A key.</summary>
    A = 65,
    /// <summary>The B key.</summary>
    B = 66,
    /// <summary>The C key.</summary>
    C = 67,
    /// <summary>The D key.</summary>
    D = 68,
    /// <summary>The E key.</summary>
    E = 69,
    /// <summary>The F key.</summary>
    F = 70,
    /// <summary>The G key.</summary>
    G = 71,
    /// <summary>The H key.</summary>
    H = 72,
    /// <summary>The I key.</summary>
    I = 73,
    /// <summary>The J key.</summary>
    J = 74,
    /// <summary>The K key.</summary>
    K = 75,
    /// <summary>The L key.</summary>
    L = 76,
    /// <summary>The M key.</summary>
    M = 77,
    /// <summary>The N key.</summary>
    N = 78,
    /// <summary>The O key.</summary>
    O = 79,
    /// <summary>The P key.</summary>
    P = 80,
    /// <summary>The Q key.</summary>
    Q = 81,
    /// <summary>The R key.</summary>
    R = 82,
    /// <summary>The S key.</summary>
    S = 83,
    /// <summary>The T key.</summary>
    T = 84,
    /// <summary>The U key.</summary>
    U = 85,
    /// <summary>The V key.</summary>
    V = 86,
    /// <summary>The W key.</summary>
    W = 87,
    /// <summary>The X key.</summary>
    X = 88,
    /// <summary>The Y key.</summary>
    Y = 89,
    /// <summary>The Z key.</summary>
    Z = 90,
    /// <summary>The left bracket key.</summary>
    LeftBracket = 91,
    /// <summary>The backslash key.</summary>
    Backslash = 92,
    /// <summary>The right bracket key.</summary>
    RightBracket = 93,
    /// <summary>The grave accent key.</summary>
    Grave = 96,
    /// <summary>The Escape key.</summary>
    Escape = 256,
    /// <summary>The Enter key.</summary>
    Enter = 257,
    /// <summary>The Tab key.</summary>
    Tab = 258,
    /// <summary>The Backspace key.</summary>
    Backspace = 259,
    /// <summary>The Insert key.</summary>
    Insert = 260,
    /// <summary>The Delete key.</summary>
    Delete = 261,
    /// <summary>The right arrow key.</summary>
    Right = 262,
    /// <summary>The left arrow key.</summary>
    Left = 263,
    /// <summary>The down arrow key.</summary>
    Down = 264,
    /// <summary>The up arrow key.</summary>
    Up = 265,
    /// <summary>The Page Up key.</summary>
    PageUp = 266,
    /// <summary>The Page Down key.</summary>
    PageDown = 267,
    /// <summary>The Home key.</summary>
    Home = 268,
    /// <summary>The End key.</summary>
    End = 269,
    /// <summary>The Caps Lock key.</summary>
    CapsLock = 280,
    /// <summary>The Scroll Lock key.</summary>
    ScrollLock = 281,
    /// <summary>The Num Lock key.</summary>
    NumLock = 282,
    /// <summary>The Print Screen key.</summary>
    PrintScreen = 283,
    /// <summary>The Pause key.</summary>
    Pause = 284,
    /// <summary>The F1 key.</summary>
    F1 = 290,
    /// <summary>The F2 key.</summary>
    F2 = 291,
    /// <summary>The F3 key.</summary>
    F3 = 292,
    /// <summary>The F4 key.</summary>
    F4 = 293,
    /// <summary>The F5 key.</summary>
    F5 = 294,
    /// <summary>The F6 key.</summary>
    F6 = 295,
    /// <summary>The F7 key.</summary>
    F7 = 296,
    /// <summary>The F8 key.</summary>
    F8 = 297,
    /// <summary>The F9 key.</summary>
    F9 = 298,
    /// <summary>The F10 key.</summary>
    F10 = 299,
    /// <summary>The F11 key.</summary>
    F11 = 300,
    /// <summary>The F12 key.</summary>
    F12 = 301,
    /// <summary>The numeric keypad 0 key.</summary>
    Keypad0 = 320,
    /// <summary>The numeric keypad 1 key.</summary>
    Keypad1 = 321,
    /// <summary>The numeric keypad 2 key.</summary>
    Keypad2 = 322,
    /// <summary>The numeric keypad 3 key.</summary>
    Keypad3 = 323,
    /// <summary>The numeric keypad 4 key.</summary>
    Keypad4 = 324,
    /// <summary>The numeric keypad 5 key.</summary>
    Keypad5 = 325,
    /// <summary>The numeric keypad 6 key.</summary>
    Keypad6 = 326,
    /// <summary>The numeric keypad 7 key.</summary>
    Keypad7 = 327,
    /// <summary>The numeric keypad 8 key.</summary>
    Keypad8 = 328,
    /// <summary>The numeric keypad 9 key.</summary>
    Keypad9 = 329,
    /// <summary>The numeric keypad decimal separator.</summary>
    KeypadDecimal = 330,
    /// <summary>The numeric keypad division key.</summary>
    KeypadDivide = 331,
    /// <summary>The numeric keypad multiplication key.</summary>
    KeypadMultiply = 332,
    /// <summary>The numeric keypad subtraction key.</summary>
    KeypadSubtract = 333,
    /// <summary>The numeric keypad addition key.</summary>
    KeypadAdd = 334,
    /// <summary>The numeric keypad Enter key.</summary>
    KeypadEnter = 335,
    /// <summary>The numeric keypad equals key.</summary>
    KeypadEqual = 336,
    /// <summary>The left Shift key.</summary>
    LeftShift = 340,
    /// <summary>The left Control key.</summary>
    LeftControl = 341,
    /// <summary>The left Alt key.</summary>
    LeftAlt = 342,
    /// <summary>The left Windows or Command key.</summary>
    LeftSuper = 343,
    /// <summary>The right Shift key.</summary>
    RightShift = 344,
    /// <summary>The right Control key.</summary>
    RightControl = 345,
    /// <summary>The right Alt key.</summary>
    RightAlt = 346,
    /// <summary>The right Windows or Command key.</summary>
    RightSuper = 347,
    /// <summary>The keyboard context menu key.</summary>
    Menu = 348
}
