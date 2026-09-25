namespace Lumis;

/// <summary>Represents one screen or state of a game.</summary>
/// <remarks>
/// Scene callbacks run on the game thread. Pass any services a scene needs through its
/// constructor. A scene manager does not own or dispose resources held by a scene.
/// </remarks>
public abstract class LumisScene
{
    /// <summary>Called when this scene becomes the current scene.</summary>
    public virtual void OnEnter()
    {
    }

    /// <summary>Called when this scene stops being the current scene.</summary>
    public virtual void OnExit()
    {
    }

    /// <summary>Updates this scene's game state.</summary>
    /// <param name="deltaTime">Elapsed time since the previous frame, in seconds.</param>
    public virtual void Update(float deltaTime)
    {
    }

    /// <summary>Draws this scene during an active drawing frame.</summary>
    /// <param name="graphics">The game's 2D drawing service.</param>
    public virtual void Draw(Graphics2D graphics)
    {
    }
}
