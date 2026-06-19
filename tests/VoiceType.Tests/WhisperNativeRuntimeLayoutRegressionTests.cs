using VoiceType.Utils;
using Whisper.net.LibraryLoader;

namespace VoiceType.Tests;

public class WhisperNativeRuntimeLayoutRegressionTests
{
    [Fact]
    public void LibraryPath_must_be_sentinel_file_not_directory_root()
    {
        var runtimeRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VoiceType");

        var wrongLibraryPath = runtimeRoot;
        var correctLibraryPath = WhisperNativeRuntimeLayout.BuildLibraryPathSentinel(runtimeRoot);

        var wrongSearchRoot = WhisperNativeRuntimeLayout.ResolveSearchRoot(wrongLibraryPath);
        var correctSearchRoot = WhisperNativeRuntimeLayout.ResolveSearchRoot(correctLibraryPath);

        Assert.NotEqual(runtimeRoot, wrongSearchRoot, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(runtimeRoot, correctSearchRoot, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSearchRoot_finds_native_directory_from_sentinel_library_path()
    {
        var runtimeRoot = @"C:\Users\Example\AppData\Roaming\VoiceType";
        var libraryPath = WhisperNativeRuntimeLayout.BuildLibraryPathSentinel(runtimeRoot);

        var searchRoot = WhisperNativeRuntimeLayout.ResolveSearchRoot(libraryPath);
        var nativeDir = WhisperNativeRuntimeLayout.GetNativeDirectory(searchRoot!);

        Assert.Equal(runtimeRoot, searchRoot, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            Path.Combine(runtimeRoot, "runtimes", "win-x64"),
            nativeDir,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void HasValidRuntimes_returns_false_when_directory_missing()
    {
        var root = Path.Combine(Path.GetTempPath(), "voicetype-layout-" + Guid.NewGuid().ToString("N"));

        try
        {
            Assert.False(WhisperNativeRuntimeLayout.HasValidRuntimes(root));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void HasValidRuntimes_returns_false_when_dll_is_too_small()
    {
        var root = CreateTempRoot();
        try
        {
            WritePlaceholderDll(root, "whisper.dll", 1024);

            Assert.False(WhisperNativeRuntimeLayout.HasValidRuntimes(root));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void HasValidRuntimes_returns_true_when_all_required_dlls_exist()
    {
        var root = CreateTempRoot();
        try
        {
            foreach (var (fileName, minBytes) in WhisperNativeRuntimeLayout.NativeDlls)
                WritePlaceholderDll(root, fileName, minBytes);

            Assert.True(WhisperNativeRuntimeLayout.HasValidRuntimes(root));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void VoiceType_assembly_embeds_all_whisper_native_resources()
    {
        var assembly = typeof(WhisperNativeBootstrap).Assembly;

        foreach (var (fileName, _) in WhisperNativeRuntimeLayout.NativeDlls)
        {
            var resourceName = WhisperNativeRuntimeLayout.EmbeddedResourceNameFor(fileName);
            using var stream = assembly.GetManifestResourceStream(resourceName);

            Assert.NotNull(stream);
            Assert.True(stream!.Length >= 50_000, $"Embedded resource '{resourceName}' looks too small.");
        }
    }

    [Fact]
    public void EnsureRuntimes_extracts_embedded_dlls_to_target_root()
    {
        var root = CreateTempRoot();
        try
        {
            WhisperNativeBootstrap.EnsureRuntimes(root);

            Assert.True(WhisperNativeRuntimeLayout.HasValidRuntimes(root));

            foreach (var (fileName, minBytes) in WhisperNativeRuntimeLayout.NativeDlls)
            {
                var path = Path.Combine(WhisperNativeRuntimeLayout.GetNativeDirectory(root), fileName);
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length >= minBytes);
            }
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Initialize_sets_cpu_runtime_order_and_sentinel_library_path()
    {
        WhisperNativeBootstrap.Initialize();

        Assert.Equal([RuntimeLibrary.Cpu], RuntimeOptions.RuntimeLibraryOrder);
        Assert.EndsWith(WhisperNativeRuntimeLayout.LibraryPathSentinelFileName, RuntimeOptions.LibraryPath);

        var searchRoot = WhisperNativeRuntimeLayout.ResolveSearchRoot(RuntimeOptions.LibraryPath!);
        Assert.NotNull(searchRoot);
        Assert.True(WhisperNativeRuntimeLayout.HasValidRuntimes(searchRoot!));
    }

    private static string CreateTempRoot() =>
        Path.Combine(Path.GetTempPath(), "voicetype-layout-" + Guid.NewGuid().ToString("N"));

    private static void WritePlaceholderDll(string root, string fileName, long size)
    {
        var dir = WhisperNativeRuntimeLayout.GetNativeDirectory(root);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);

        using var stream = File.Create(path);
        stream.SetLength(size);
    }

    private static void DeleteTempRoot(string root)
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
