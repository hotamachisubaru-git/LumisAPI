namespace Lumis.Tests;

public sealed class AudioTests
{
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    public void InvalidVolumeCannotReachNativeMixer(float volume)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AudioDevice.ValidateVolume(volume));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    public void VolumeAcceptsInclusiveSilentToFullRange(float volume)
    {
        AudioDevice.ValidateVolume(volume);
    }

    [Fact]
    public void DisabledAudioRejectsPlaybackOperationsAndAllowsStreamUpdates()
    {
        var context = new GameContext();
        context.Activate();
        var audio = new AudioDevice(context);
        audio.Initialize(false);

        Assert.False(audio.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => audio.LoadSound("sound.wav"));
        Assert.Throws<InvalidOperationException>(() => audio.LoadMusic("music.ogg"));
        Assert.Throws<InvalidOperationException>(() => audio.MasterVolume);
        Assert.Throws<InvalidOperationException>(() => audio.MasterVolume = 0.5f);
        audio.UpdateStreams();
        audio.Shutdown();
    }

    [Fact]
    public void AudioOperationsRequireRunningContext()
    {
        var context = new GameContext();
        var audio = new AudioDevice(context);

        Assert.False(audio.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => audio.LoadSound("sound.wav"));
        Assert.Throws<InvalidOperationException>(() => audio.Initialize(false));

        context.Activate();
        audio.Initialize(false);
        context.Deactivate();

        Assert.False(audio.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => audio.LoadMusic("music.ogg"));
        Assert.Throws<InvalidOperationException>(() => audio.UpdateStreams());
    }
}
