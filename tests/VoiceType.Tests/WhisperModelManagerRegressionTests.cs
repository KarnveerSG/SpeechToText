using VoiceType.Services;
using VoiceType.Tests.Support;

namespace VoiceType.Tests;

public class WhisperModelManagerRegressionTests
{
    [Fact]
    public void Dispose_can_be_called_twice_without_throwing()
    {
        var settings = new InMemorySettingsService();
        var manager = new WhisperModelManager(settings, new FakeLogService(), new FakeNotificationService());

        var ex = Record.Exception(() =>
        {
            manager.Dispose();
            manager.Dispose();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Invalidate_after_dispose_does_not_throw()
    {
        var settings = new InMemorySettingsService();
        var manager = new WhisperModelManager(settings, new FakeLogService(), new FakeNotificationService());
        manager.Dispose();

        var ex = Record.Exception(() => settings.NotifyChanged());
        Assert.Null(ex);
    }

    [Fact]
    public void ThreadCount_is_at_least_one()
    {
        var settings = new InMemorySettingsService();
        using var manager = new WhisperModelManager(settings, new FakeLogService(), new FakeNotificationService());

        Assert.True(manager.ThreadCount >= 1);
        Assert.True(Directory.Exists(manager.ModelsDirectory));
    }
}
