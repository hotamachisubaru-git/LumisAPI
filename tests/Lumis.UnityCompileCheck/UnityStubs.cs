#nullable enable
// Compile-only API shapes. NOT UnityEngine, NOT included in the package, and NOT a Unity runtime test.
using System;
namespace UnityEngine
{
    public class Object
    {
        public static void DontDestroyOnLoad(Object target) { }
    }
    public class GameObject : Object { }
    public class Transform : Object { public Transform? parent { get; set; } }
    public class MonoBehaviour : Object
    {
        public bool enabled { get; set; }
        public GameObject gameObject { get; } = new GameObject();
        public Transform transform { get; } = new Transform();
    }
    public sealed class SerializeField : Attribute { }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class AddComponentMenu : Attribute { public AddComponentMenu(string name) { } }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public static class Application
    {
        public static string dataPath => "/game/Game_Data";
        public static string streamingAssetsPath => "/game/Game_Data/StreamingAssets";
        public static string persistentDataPath => "/game/user";
        public static void Quit(int code) { }
    }
    public static class Debug
    {
        public static void LogWarning(object message, Object? context = null) { }
        public static void LogError(object message, Object? context = null) { }
        public static void LogException(Exception error, Object? context = null) { }
    }
    public static class Time { public static float unscaledDeltaTime => 0.016f; }
}
namespace UnityEngine.SceneManagement
{
    public static class SceneManager { public static void LoadScene(string name) { } }
}
namespace UnityEditor
{
    public static class EditorApplication { public static bool isPlaying { get; set; } }
}
