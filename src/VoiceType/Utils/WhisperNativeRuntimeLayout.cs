using System.IO;

namespace VoiceType.Utils;

/// <summary>
/// Path conventions for Whisper.net native runtime discovery on Windows.
/// </summary>
internal static class WhisperNativeRuntimeLayout
{
    public const string LibraryPathSentinelFileName = ".whisper-root";
    public const string NativeSubDirectory = "runtimes/win-x64";

    public static readonly IReadOnlyList<(string FileName, long MinBytes)> NativeDlls =
    [
        ("ggml-base-whisper.dll", 500_000),
        ("ggml-cpu-whisper.dll", 600_000),
        ("ggml-whisper.dll", 50_000),
        ("whisper.dll", 400_000),
    ];

    public static readonly IReadOnlyList<string> LoadOrder =
    [
        "ggml-base-whisper.dll",
        "ggml-cpu-whisper.dll",
        "ggml-whisper.dll",
        "whisper.dll",
    ];

    public static string GetNativeDirectory(string runtimeRoot) =>
        Path.Combine(runtimeRoot, "runtimes", "win-x64");

    public static string BuildLibraryPathSentinel(string runtimeRoot) =>
        Path.Combine(runtimeRoot, LibraryPathSentinelFileName);

    /// <summary>
    /// Whisper.net calls <see cref="Path.GetDirectoryName(string)"/> on <c>RuntimeOptions.LibraryPath</c>.
    /// </summary>
    public static string? ResolveSearchRoot(string libraryPath)
    {
        if (string.IsNullOrWhiteSpace(libraryPath))
            return null;

        var normalized = libraryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.GetDirectoryName(normalized);
    }

    public static bool HasValidRuntimes(string runtimeRoot)
    {
        var runtimeDir = GetNativeDirectory(runtimeRoot);
        if (!Directory.Exists(runtimeDir))
            return false;

        foreach (var (fileName, minBytes) in NativeDlls)
        {
            var path = Path.Combine(runtimeDir, fileName);
            if (!File.Exists(path))
                return false;

            if (new FileInfo(path).Length < minBytes)
                return false;
        }

        return true;
    }

    public static string EmbeddedResourceNameFor(string fileName) =>
        $"whisper.win-x64.{fileName}";
}
