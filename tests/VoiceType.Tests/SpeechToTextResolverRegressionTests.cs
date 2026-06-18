using VoiceType.Services;
using VoiceType.Tests.Support;

namespace VoiceType.Tests;

public class SpeechToTextResolverRegressionTests
{
    [Fact]
    public void Resolve_returns_registered_local_service()
    {
        var stt = new FakeSpeechToTextService();
        var resolver = new SpeechToTextResolver(stt);

        Assert.Same(stt, resolver.Resolve());
    }
}
