using System.IO;
using VoiceType.Utils;

var output = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "VoiceType", "icon.ico");

output = Path.GetFullPath(output);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
AppIcon.SaveIcoFile(output);
Console.WriteLine($"Wrote {output}");
