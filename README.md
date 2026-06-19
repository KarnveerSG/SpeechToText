# VoiceType

**Push-to-Talk Speech-to-Text for Windows**

VoiceType is a lightweight Windows desktop application that runs silently in the system tray and enables instant speech-to-text transcription through a configurable global hotkey. Simply hold your hotkey, speak, release, and the transcribed text is automatically pasted into your active application.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)

---

## ✨ Features

### 🎤 **Push-to-Talk Dictation**
- Hold your configured hotkey to record audio
- Release to automatically transcribe and paste
- Works across all applications (browsers, editors, chat apps, etc.)

### 🔊 **Audio Feedback**
- Subtle audio cues when recording starts/stops
- Error tones for microphone conflicts
- Non-intrusive, configurable beeps

### 🧠 **Local Whisper (On-Device)**
- **Whisper.cpp via Whisper.net** — accurate, private transcription with no API key
- Models download once to `%APPDATA%\VoiceType\models\` (Tiny through Small, English or multilingual)
- First run needs internet only to fetch the model; after that, fully offline

### ⚙️ **Fully Customizable**
- Configure any hotkey combination (Ctrl, Shift, Alt, Win + any key)
- Select your preferred microphone
- Choose between paste (Ctrl+V) or character-by-character typing
- Toggle audio cues on/off
- Start with Windows option

### 🎨 **Modern UI**
- Windows 11 Fluent Design with Mica backdrop
- Automatic dark/light theme matching
- Clean, minimal settings interface

---

## 🚀 Quick Start

### Prerequisites
- **Windows 10** (19041+) or **Windows 11**
- **Microphone** (any Windows-compatible audio input device)
- **Internet** on first run (to download the Whisper model)

### Installation

**Option A — Desktop shortcut (recommended)**

From a dev checkout, run `deploy-desktop.bat`. It builds a self-contained `VoiceType.exe` and copies it to your Desktop. No extra folders beside the exe — native libraries extract to AppData on first launch.

**Option B — Portable folder**

Run `publish.bat`, then copy the entire `dist\` folder anywhere. That folder includes `VoiceType.exe` plus `runtimes\win-x64\` (four Whisper DLLs).

**Option C — Release download**

1. Download the latest release from [Releases](../../releases)
2. Run `VoiceType.exe` (or extract the ZIP if distributed that way)
3. The app starts in the system tray (icon near the clock)

### First Use

1. **Right-click** the tray icon → **Settings**
2. Configure your hotkey (default: `Ctrl + Alt + Space`)
3. Select your microphone from the dropdown
4. Pick a **Whisper model** (Base English is the default; smaller = faster, larger = more accurate)
5. Click **Save** — the model downloads on first dictation if not cached yet

You're ready! Hold your hotkey, speak, release, and watch the magic happen.

---

## 📖 Usage

### Basic Workflow

1. **Position your cursor** in any text field (email, document, chat, etc.)
2. **Hold your hotkey** (e.g., `Ctrl + Alt + Space`)
3. **Speak clearly** into your microphone
4. **Release the hotkey** when done
5. Text appears instantly at your cursor position

### Audio Cues

| Sound | Meaning |
|-------|---------|
| Low tone (800Hz) | Recording started - speak now |
| Higher tone (1200Hz) | Recording stopped - processing |
| Harsh buzz | Error (microphone unavailable or API failure) |

You can disable audio cues in Settings if you prefer silent operation.

---

## ⚙️ Configuration

### Hotkey Setup

**Requirements:**
- Choose at least one modifier (Ctrl, Shift, Alt, or Win)
- Select any letter, number, or function key
- Avoid conflicts with system shortcuts (e.g., Win+L locks Windows)

**Recommended combinations:**
- `Ctrl + Alt + Space` (default)
- `Ctrl + Shift + V`
- `Win + Shift + D`

### Microphone Selection

The app lists all available audio input devices. Select:
- **Default** - Uses your Windows default recording device
- **Specific Device** - Pin to a particular microphone

**Troubleshooting:**
- If your mic isn't listed, check Windows Sound settings
- Ensure the device is enabled and not in use by another app
- Try running VoiceType as Administrator if permission issues arise

### Whisper Model Selection

| Model | Speed | Accuracy | Notes |
|-------|-------|----------|-------|
| Tiny English | Fastest | Good | ~75 MB download |
| Base English (default) | Fast | Better | ~142 MB |
| Small English | Slower | Best (English) | ~466 MB |
| Base Multilingual | Fast | Good | Many languages |
| Small Multilingual | Slower | Better | Many languages |

Models are stored under `%APPDATA%\VoiceType\models\`. Change the model in Settings anytime; the new one downloads on next use.

**Legacy settings:** Older `settings.json` files may still contain `Engine: "Cloud"` and `CloudApiKey`. Those fields are ignored — the app always uses local Whisper now.

### Text Delivery Modes

| Mode | How it works | Use when |
|------|-------------|----------|
| **Paste** (default) | Copies text to clipboard, sends Ctrl+V | Most applications (fast, preserves formatting) |
| **Type** | Types each character individually | Apps that block paste (e.g., some games, terminals) |

---

## 🏗️ Architecture

VoiceType is built with modern .NET practices:

```
┌─────────────────┐
│  System Tray    │  WPF NotifyIcon with context menu
└────────┬────────┘
         │
┌────────▼────────┐
│ DI Container    │  Microsoft.Extensions.DependencyInjection
└────────┬────────┘
         │
    ┌────┴─────────────────┬─────────────────┬──────────────┐
    │                      │                 │              │
┌───▼────────┐   ┌─────────▼────────┐   ┌───▼──────┐   ┌──▼───────┐
│  Hotkey    │   │ Audio Capture    │   │  Speech  │   │   Text   │
│  Service   │   │   Service        │   │ Resolver │   │ Injection│
│            │   │  (NAudio)        │   │          │   │ Service  │
│ Win32 Hook │   │  16kHz Mono WAV  │   │ Local    │   │          │
└────────────┘   └──────────────────┘   │ Whisper  │   │InputSim+ │
                                        └──────────┘   └──────────┘
```

### Key Components

- **HotkeyService** - Low-level keyboard hook (WH_KEYBOARD_LL) for system-wide push-to-talk
- **AudioCaptureService** - NAudio WaveInEvent recorder, outputs 16kHz mono 16-bit PCM
- **AudioCueService** - Beep synthesizer using NAudio SignalGenerator
- **LocalWhisperSpeechToTextService** - Whisper.net transcription pipeline
- **WhisperModelManager** - Downloads, caches, and loads ggml models from AppData
- **WhisperNativeBootstrap** - Extracts embedded native DLLs and configures Whisper.net library discovery (single-file exe support)
- **TextInjectionService** - InputSimulatorPlus for keyboard simulation
- **DictationCoordinator** - Orchestrates the full pipeline (hotkey → record → transcribe → inject)

### Design Patterns

- **MVVM** - Settings UI via CommunityToolkit.Mvvm
- **Dependency Injection** - All services are injected
- **Strategy** - SpeechToTextResolver picks engine at runtime
- **Observer** - EventArgs for hotkey press/release

---

## 🛠️ Building from Source

### Requirements
- Visual Studio 2022 (17.8+) or .NET 8 SDK
- Windows 10 SDK (10.0.19041.0)

### Steps

```bash
# Clone the repository
git clone https://github.com/KarnveerSG/SpeechToText.git
cd SpeechToText

# Restore and build
dotnet restore
dotnet build -c Release

# Run from source
dotnet run --project src/VoiceType/VoiceType.csproj

# Run unit tests
dotnet test tests/VoiceType.Tests/VoiceType.Tests.csproj -c Release

# Build self-contained portable exe (output in dist/)
publish.bat

# Build and copy lone exe to Desktop (natives go to %APPDATA%\VoiceType\runtimes)
deploy-desktop.bat
```

### Single-file deployment

`VoiceType.exe` can run as the only file on your Desktop:

1. Whisper native DLLs are **embedded** in the executable
2. On first run, they extract to `%APPDATA%\VoiceType\runtimes\win-x64\`
3. Whisper ggml models download to `%APPDATA%\VoiceType\models\`
4. Logs: `%APPDATA%\VoiceType\logs\voicetype-YYYYMMDD.log`

`deploy-desktop.bat` kills any running instance, removes stale Desktop copies, builds fresh, and installs one exe.

### Project Structure

```
SpeechToText/
├── src/VoiceType/
│   ├── Models/              # Data models (AppSettings, HotkeyConfig)
│   ├── Services/            # Business logic (DI-registered services)
│   │   ├── HotkeyService.cs
│   │   ├── AudioCaptureService.cs
│   │   ├── OfflineSpeechToTextService.cs
│   │   ├── CloudSpeechToTextService.cs
│   │   ├── DictationCoordinator.cs
│   │   └── ...
│   ├── ViewModels/          # MVVM view models
│   ├── Views/               # WPF XAML windows
│   │   └── SettingsWindow.xaml
│   ├── Utils/               # Helpers (WhisperNativeBootstrap, KeyCatalog, etc.)
│   ├── App.xaml             # WPF application entry point
│   └── VoiceType.csproj
├── tests/VoiceType.Tests/   # xUnit regression tests
├── publish.bat              # Self-contained dist\ build
├── deploy-desktop.bat       # Build + install lone exe to Desktop
├── Prompt.md                # Original specification
├── plan.md                  # Implementation plan
└── README.md                # This file
```

---

## 🐛 Troubleshooting

### "Microphone unavailable" error

**Cause:** Another app is using your microphone  
**Fix:**
1. Close apps that might be using the mic (Discord, Zoom, OBS, etc.)
2. Check Windows Sound settings → Recording tab
3. Run VoiceType as Administrator if permission denied

### "Native Library not found" (Whisper)

**Cause:** Whisper native DLLs missing or stale after moving only the exe  
**Fix:**
1. Run `deploy-desktop.bat` again (or delete `%APPDATA%\VoiceType\runtimes` and restart)
2. Check `%APPDATA%\VoiceType\logs\` for bootstrap errors
3. Ensure you are on 64-bit Windows (win-x64 build)

### Model download fails

**Cause:** No internet, firewall, or disk space  
**Fix:**
1. Confirm connectivity on first run
2. Ensure ~500 MB free under `%APPDATA%\VoiceType\models\`
3. Retry from Settings after changing model

### Hotkey doesn't work

**Possible causes:**
- Another app registered the same combination (try a different hotkey)
- The app isn't running (check system tray)
- UAC-elevated apps block low-level hooks (run VoiceType as Admin)

### Text doesn't paste

**In Paste mode:**
- Some apps block programmatic paste → Try "Type instead of paste" in Settings

**In Type mode:**
- Ensure the target window has focus when you release the hotkey
- Some fullscreen games block input simulation → Run game in windowed mode

---

## 🔒 Privacy & Security

### Data Collection
**VoiceType collects NO telemetry or analytics.** All data stays on your machine.

### Local processing
- Audio and transcription stay on your PC (after the Whisper model is downloaded)
- Settings, models, native runtimes, and logs live under `%APPDATA%\VoiceType\`
- Temporary WAV files are deleted immediately after transcription
- No telemetry or analytics

---

## 📝 Settings File

Settings are stored in JSON at:
```
%APPDATA%\VoiceType\settings.json
```

Example:
```json
{
  "Hotkey": {
    "Modifiers": 3,
    "VirtualKey": 32,
    "KeyName": "Space"
  },
  "MicrophoneDeviceNumber": -1,
  "MicrophoneName": "Default",
  "Engine": "Offline",
  "WhisperModel": "BaseEn",
  "CloudApiKey": "",
  "StartWithWindows": false,
  "PlayBeep": true,
  "Delivery": "Paste"
}
```

**Manual editing:** The app reloads settings when opened. Edit while the app is closed for best results.

---

## 🎯 Roadmap

Planned features:
- [ ] Multilingual UI (Spanish, French, German, Chinese)
- [ ] Automatic punctuation insertion (offline mode)
- [ ] Voice commands ("new line", "delete that", etc.)
- [ ] Per-app hotkey profiles
- [ ] Alternative cloud providers (Azure Cognitive, Google Cloud)
- [ ] System-wide spellcheck integration
- [ ] Noise reduction preprocessing
- [ ] Customizable audio cue sounds

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add some amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Code Style
- Follow existing patterns (MVVM, DI, etc.)
- Add XML doc comments to public APIs
- Keep services focused (single responsibility)
- Use `async`/`await` for I/O operations

### Testing
Before submitting:
```bash
dotnet test tests/VoiceType.Tests/VoiceType.Tests.csproj -c Release
```
- Build in Release mode without warnings
- Test dictation with at least two Whisper model sizes
- Verify Settings window layout at 100%, 125%, 150% DPI
- Run `WhisperNativeRuntimeLayoutRegressionTests` if touching native bootstrap / publish

---

## 📄 License

This project is licensed under the **MIT License** - see below for details:

```
MIT License

Copyright (c) 2024 VoiceType Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🙏 Acknowledgments

Built with these excellent libraries:
- [NAudio](https://github.com/naudio/NAudio) - Audio capture and playback
- [InputSimulatorPlus](https://github.com/TChatzigiannakis/InputSimulatorPlus) - Keyboard simulation
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM helpers
- [Whisper.net](https://github.com/sandrohanea/whisper.net) - Local Whisper.cpp bindings
- [OpenAI Whisper](https://openai.com/research/whisper) - Model architecture (runs locally via ggml)

---

## 📞 Support

- **Issues:** [GitHub Issues](../../issues)
- **Discussions:** [GitHub Discussions](../../discussions)
- **Email:** support@voicetype.app (if applicable)

---

## ⭐ Star History

If VoiceType saves you time, consider giving it a star! ⭐

---

**Made with ❤️ for the Windows community**
