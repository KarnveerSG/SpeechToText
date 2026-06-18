using VoiceType.Services;
using VoiceType.Tests.Support;

namespace VoiceType.Tests;

public class DictationCoordinatorRegressionTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* best effort */ }
        }
    }

    private string Track(string path)
    {
        _tempFiles.Add(path);
        return path;
    }

    private static DictationCoordinator CreateCoordinator(
        FakeHotkeyService hotkey,
        FakeAudioCaptureService capture,
        FakeSpeechToTextService stt,
        FakeTextInjectionService injection,
        out FakeNotificationService notify,
        out FakeLogService log)
    {
        notify = new FakeNotificationService();
        log = new FakeLogService();
        var cues = new FakeAudioCueService();
        var resolver = new SpeechToTextResolver(stt);

        return new DictationCoordinator(
            hotkey, cues, capture, resolver, injection, notify, log);
    }

    [Fact]
    public async Task Hotkey_cycle_transcribes_and_delivers_text()
    {
        var hotkey = new FakeHotkeyService();
        var capture = new FakeAudioCaptureService();
        var stt = new FakeSpeechToTextService { NextResult = "Does this work any better now?" };
        var injection = new FakeTextInjectionService();

        var wav = Track(TestWavFactory.CreateSilentWav(TimeSpan.FromMilliseconds(500)));
        capture.EnqueueStopResult(wav);

        using var coordinator = CreateCoordinator(hotkey, capture, stt, injection, out _, out _);
        coordinator.Start();

        hotkey.SimulatePress();
        hotkey.SimulateRelease();

        await WaitUntilAsync(() => injection.Delivered.Count > 0, TimeSpan.FromSeconds(3));

        Assert.Equal("Does this work any better now?", injection.Delivered.Single());
        Assert.Equal(1, stt.CallCount);
        Assert.False(File.Exists(wav));
    }

    [Fact]
    public async Task Second_recording_works_while_first_is_transcribing()
    {
        var hotkey = new FakeHotkeyService();
        var capture = new FakeAudioCaptureService();
        var stt = new FakeSpeechToTextService
        {
            TranscribeDelay = TimeSpan.FromMilliseconds(400),
        };
        stt.EnqueueResult("first");
        stt.EnqueueResult("second");
        var injection = new FakeTextInjectionService();

        var wav1 = Track(TestWavFactory.CreateSilentWav(TimeSpan.FromMilliseconds(500)));
        var wav2 = Track(TestWavFactory.CreateSilentWav(TimeSpan.FromMilliseconds(500)));
        capture.EnqueueStopResult(wav1);
        capture.EnqueueStopResult(wav2);

        using var coordinator = CreateCoordinator(hotkey, capture, stt, injection, out _, out _);
        coordinator.Start();

        hotkey.SimulatePress();
        hotkey.SimulateRelease();

        await WaitUntilAsync(() => stt.CallCount == 1, TimeSpan.FromSeconds(2));

        hotkey.SimulatePress();
        Assert.Equal(2, capture.StartCount);

        hotkey.SimulateRelease();
        await WaitUntilAsync(() => injection.Delivered.Count == 2, TimeSpan.FromSeconds(5));

        Assert.Equal(["first", "second"], injection.Delivered);
    }

    [Fact]
    public void Dispose_during_slow_enqueue_does_not_throw()
    {
        var hotkey = new FakeHotkeyService();
        var capture = new FakeAudioCaptureService();
        capture.SetStopDelay(TimeSpan.FromMilliseconds(500));
        capture.EnqueueStopResult(Track(TestWavFactory.CreateSilentWav(TimeSpan.FromMilliseconds(500))));

        var stt = new FakeSpeechToTextService { TranscribeDelay = TimeSpan.FromSeconds(2) };
        var injection = new FakeTextInjectionService();

        var coordinator = CreateCoordinator(hotkey, capture, stt, injection, out _, out _);
        coordinator.Start();

        hotkey.SimulatePress();
        hotkey.SimulateRelease();

        var ex = Record.Exception(() =>
        {
            coordinator.Dispose();
            coordinator.Dispose();
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task Empty_capture_notifies_user_and_skips_transcription()
    {
        var hotkey = new FakeHotkeyService();
        var capture = new FakeAudioCaptureService();
        capture.EnqueueStopResult(null);

        var stt = new FakeSpeechToTextService();
        var injection = new FakeTextInjectionService();

        using var coordinator = CreateCoordinator(hotkey, capture, stt, injection, out var notify, out _);
        coordinator.Start();

        hotkey.SimulatePress();
        hotkey.SimulateRelease();

        await WaitUntilAsync(() => notify.Infos.Count > 0, TimeSpan.FromSeconds(2));

        Assert.Empty(injection.Delivered);
        Assert.Equal(0, stt.CallCount);
        Assert.Contains(notify.Infos, n => n.Title == "Nothing captured");
    }

    [Fact]
    public void Press_while_already_recording_is_ignored()
    {
        var hotkey = new FakeHotkeyService();
        var capture = new FakeAudioCaptureService();
        var stt = new FakeSpeechToTextService();
        var injection = new FakeTextInjectionService();

        using var coordinator = CreateCoordinator(hotkey, capture, stt, injection, out _, out _);
        coordinator.Start();

        hotkey.SimulatePress();
        hotkey.SimulatePress();

        Assert.Equal(1, capture.StartCount);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(25);
        }

        throw new TimeoutException("Condition was not met before timeout.");
    }
}
