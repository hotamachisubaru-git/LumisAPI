namespace Lumis;

/// <summary>Manages the playback state of an animation.</summary>
/// <remarks>This class is game-thread-only. It advances the frame timer on each Update call and provides methods to play, pause, resume, and stop animations.</remarks>
public class AnimationController
{
    private string _currentAnimationName = string.Empty;
    private Animation _currentAnimation = default;
    private int _currentFrameIndex;
    private float _frameTimer;
    private bool _isPlaying;
    private bool _isLooping;
    private bool _isFinished;

    /// <summary>Gets the name of the current animation.</summary>
    public string CurrentAnimationName => _currentAnimationName;

    /// <summary>Gets the current frame index.</summary>
    public int CurrentFrameIndex => _currentFrameIndex;

    /// <summary>Gets the timer within the current frame.</summary>
    public float FrameTimer => _frameTimer;

    /// <summary>Gets whether the animation is currently playing.</summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>Gets whether the animation is set to loop.</summary>
    public bool IsLooping => _isLooping;

    /// <summary>Gets the current frame (read-only).</summary>
    public AnimationFrame CurrentFrame => _currentAnimation.GetFrame(_currentFrameIndex);

    /// <summary>Gets whether the animation has finished (only for non-looping animations).</summary>
    public bool IsFinished => _isFinished;

    /// <summary>Plays the specified animation from the beginning.</summary>
    /// <param name="animationName">The name of the animation to play.</param>
    public void Play(string animationName)
    {
        _currentAnimationName = animationName;
        _currentAnimation = LoadAnimation(animationName);
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _isPlaying = true;
        _isLooping = _currentAnimation.Looping;
        _isFinished = false;
    }

    /// <summary>Pauses the current animation.</summary>
    public void Pause() => _isPlaying = false;

    /// <summary>Resumes a paused animation.</summary>
    public void Resume() => _isPlaying = true;

    /// <summary>Stops the animation and resets to the beginning.</summary>
    public void Stop()
    {
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _isPlaying = false;
        _isFinished = false;
    }

    /// <summary>Advances the frame timer. Must be called externally each frame.</summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    public void Update(float deltaTime)
    {
        if (!_isPlaying || _isFinished)
            return;

        _frameTimer += deltaTime;
        var frameDuration = _currentAnimation.FrameDuration;

        if (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;

            if (_isLooping)
            {
                _currentFrameIndex = (_currentFrameIndex + 1) % _currentAnimation.FrameCount;
            }
            else
            {
                if (_currentFrameIndex + 1 >= _currentAnimation.FrameCount)
                {
                    _isFinished = true;
                    _isPlaying = false;
                    _currentFrameIndex = _currentAnimation.FrameCount - 1;
                    return;
                }
                _currentFrameIndex++;
            }
        }
    }

    /// <summary>Switches to a different animation (seek).</summary>
    /// <param name="animationName">The name of the animation to switch to.</param>
    public void SetAnimation(string animationName)
    {
        _currentAnimationName = animationName;
        _currentAnimation = LoadAnimation(animationName);
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _isPlaying = true;
        _isLooping = _currentAnimation.Looping;
        _isFinished = false;
    }

    private Animation LoadAnimation(string name)
    {
        // TODO: Replace with actual animation data source
        throw new NotImplementedException("Animation data source not implemented.");
    }
}
