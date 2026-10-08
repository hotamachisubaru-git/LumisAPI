#nullable enable
using Lumis.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this and LumisAntiCheat in a root GameObject in an otherwise empty bootstrap scene.
// Only the gameplay scene should contain scripts that change game state.
public sealed class LumisBootstrap : MonoBehaviour
{
    [SerializeField] private LumisAntiCheat antiCheat = null!;
    [SerializeField] private string gameplayScene = "Game";
    private void Start()
    {
        if (antiCheat == null || !antiCheat.IsReady)
        {
            Debug.LogError("Lumis Security initialization did not succeed. Gameplay was not loaded.");
            return;
        }
        SceneManager.LoadScene(gameplayScene);
    }
}
