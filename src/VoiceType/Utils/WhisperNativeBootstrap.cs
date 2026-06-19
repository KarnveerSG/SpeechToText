using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Whisper.net.LibraryLoader;

namespace VoiceType.Utils;

/// <summary>
/// Ensures Whisper.net native DLLs exist before any <see cref="Whisper.net.WhisperFactory"/> is created.
/// Lone executables extract natives into %APPDATA%\VoiceType\runtimes\win-x64.
/// </summary>
internal static class WhisperNativeBootstrap
{
    private static string? _runtimeRoot;
    private static readonly object Gate = new();

    private static string AppDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VoiceType");

    [ModuleInitializer]
    internal static void Run() => Initialize();

    public static void Initialize()
    {
        lock (Gate)
        {
            if (_runtimeRoot is not null)
            {
                ApplyLibraryPath(_runtimeRoot);
                return;
            }

            Log($"Whisper bootstrap starting. exe={Environment.ProcessPath}");
            RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Cpu];

            foreach (var root in GetCandidateRoots())
            {
                if (!TryUseRuntimeRoot(root, createIfMissing: false, out var runtimeRoot))
                    continue;

                Activate(runtimeRoot);
                return;
            }

            if (TryUseRuntimeRoot(AppDataRoot, createIfMissing: true, out var appDataRuntimeRoot))
            {
                Activate(appDataRuntimeRoot);
                return;
            }

            throw new InvalidOperationException(
                "VoiceType could not prepare Whisper native libraries in AppData. Rebuild VoiceType.exe.");
        }
    }

    internal static void EnsureRuntimes(string runtimeRoot) =>
        ExtractEmbeddedRuntimes(runtimeRoot);

    private static void Activate(string runtimeRoot)
    {
        PreloadNativeLibraries(runtimeRoot);
        ApplyLibraryPath(runtimeRoot);
        _runtimeRoot = runtimeRoot;
        Log($"Whisper runtimes ready at {runtimeRoot}");
    }

    private static void ApplyLibraryPath(string runtimeRoot) =>
        RuntimeOptions.LibraryPath = WhisperNativeRuntimeLayout.BuildLibraryPathSentinel(runtimeRoot);

    private static void PreloadNativeLibraries(string runtimeRoot)
    {
        var dir = WhisperNativeRuntimeLayout.GetNativeDirectory(runtimeRoot);
        foreach (var dll in WhisperNativeRuntimeLayout.LoadOrder)
        {
            var path = Path.Combine(dir, dll);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing Whisper native library: {path}");

            NativeLibrary.Load(path);
        }
    }

    private static IEnumerable<string> GetCandidateRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in new[]
                 {
                     AppDataRoot,
                     Path.GetDirectoryName(Environment.ProcessPath),
                     AppContext.BaseDirectory,
                 })
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var normalized = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (seen.Add(normalized))
                yield return normalized;
        }
    }

    private static bool TryUseRuntimeRoot(string root, bool createIfMissing, out string runtimeRoot)
    {
        runtimeRoot = root;

        if (WhisperNativeRuntimeLayout.HasValidRuntimes(root))
            return true;

        if (!createIfMissing)
            return false;

        try
        {
            ExtractEmbeddedRuntimes(root);
            return WhisperNativeRuntimeLayout.HasValidRuntimes(root);
        }
        catch (Exception ex)
        {
            Log($"Failed to deploy Whisper runtimes to {root}: {ex.Message}");
            return false;
        }
    }

    private static void ExtractEmbeddedRuntimes(string root)
    {
        var destDir = WhisperNativeRuntimeLayout.GetNativeDirectory(root);
        Directory.CreateDirectory(destDir);

        var assembly = typeof(WhisperNativeBootstrap).Assembly;
        foreach (var (fileName, minBytes) in WhisperNativeRuntimeLayout.NativeDlls)
        {
            var destPath = Path.Combine(destDir, fileName);
            if (File.Exists(destPath) && new FileInfo(destPath).Length >= minBytes)
                continue;

            if (File.Exists(destPath))
            {
                try { File.Delete(destPath); }
                catch { /* replace on next write attempt */ }
            }

            var resourceName = WhisperNativeRuntimeLayout.EmbeddedResourceNameFor(fileName);
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException(
                    $"Embedded Whisper runtime '{fileName}' was not found in {assembly.FullName}.");

            using var output = File.Create(destPath);
            stream.CopyTo(output);

            if (new FileInfo(destPath).Length < minBytes)
            {
                throw new InvalidDataException(
                    $"Extracted '{fileName}' looks corrupt ({new FileInfo(destPath).Length} bytes).");
            }
        }
    }

    private static void Log(string message)
    {
        try
        {
            var logDir = Path.Combine(AppDataRoot, "logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"voicetype-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] {message}\r\n");
        }
        catch
        {
            // best effort
        }
    }
}
