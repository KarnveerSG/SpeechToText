# VoiceType Project Plan

## Overview
Build "VoiceType" - a WinUI 3/.NET 8 desktop app that runs in system tray and performs Push-to-Talk Speech-to-Text with global hotkey.

---

## Phase 1: Core Infrastructure (Foundation)

### 1.1 Create .csproj Solution Files
**Purpose:** Set up project structure, dependencies, and solution files.

**Files to create/add:**
- `VoiceType.sln` - Main solution file including `.NET` SDK config and NuGet packages (`NAudio`, `Microsoft.Speech`, `CommunityToolkit.MVVM`)

**Key Packages Required:**
- `NAudio` (audio capture) | `Microsoft.Windows.SDK.BuildTools` (WinUI3)  
  → CommunityToolkit.Mvvm, InputSimulatorPlus, Microsoft.SpeechRecognition


### 1.2 Project Structure Skeleton
Organize folders:
```
SpeechToText/
├── src/VoiceType/               # Main WinUI3 project
│   ├── Models/                  # Data models (HotkeyConfig, AudioSettings)
│   ├── Services/                # DI-friendly services (ISttService, IAudioSvc)  
│   │   ├── HotkeyService.cs     # Global hotkey registration & handling
│   │   └── AudioCaptureService.cs# WAV buffer recording to %TEMP%
│   ├── ViewModels/VoiceTypeViewModels/  # MVVM view models for Settings UI  
│   ├── Views/MainViews/         # Fluent-style XAML windows (HotKeyConfig)
│   └── Utils/                   # Beep synthesizer, temp path helpers
├── Prompt.md                    # Original prompt spec
├── plan.md                      # This file
└── tests/VoiceType.Tests.csproj # Unit test project with DI container & mock audio capture  
```

---

## Phase 2: Audio System (Low Latency)

### 2.1 Create Beep Synthesizer Utility
**Purpose:** Non-intrusive startup/shutdown cues.

**Files:** `src/VoiceType/Utils/AudioCues.cs` with helper methods for playing system sounds.

**Logic Flow:**
- Start event: Play low pitch (800Hz, 100ms) on hotkey press
- End event: Higher tone (1200Hz) when stopped & processing begins  
→ Error state: Short error buzz if mic unavailable


### 2.2 AudioCaptureService Implementation
**Purpose:** Record audio to temporary buffer without blocking UI.

**Key Components:**
- Capture device selection via NAudio's `WaveInEvent` | `MMDeviceEnumerator`
- Async recording thread using .NET threads, buffering to `.wav` in `%TEMP%`.


---

## Phase 3: Global Hotkey System (Triggers)

### 3.1 Create HotkeyService Implementation  
**Purpose:** Register system-wide hotkeys that work when minimized/hidden.

**Key Components:**
- `GlobalHotKeySpecifier` struct | Custom message router to catch window messages on process level.


---

## Phase 4: Speech-to-Text Engine (Transcription)

### 4.1 Implement Dual-STT Architecture  
**Purpose:** Toggle between offline (fast, Windows SDK-based) vs cloud API variants.

**Cloud Options:**
- `Microsoft.SpeechRecognition` (Windows native STT | Offline)
- OpenAI Whisper REST endpoint OR Azure Cognitive Services


---

## Phase 5: Input Simulation (Delivery)  

### 5.1 Create VirtualInputService Implementation  
**Purpose:** Deliver text to foreground app using simulated keystrokes.

**Key Components:**
- `SendKeysWrapper` class wrapping Microsoft.PowerShell.Core.SDKeys or UIAutomation patterns | Character-by-character typing via Win32 APIs,


---

## Phase 6: MVVM Settings UI (Configuration)  

### 6.1 Create Modern Fluent-style XAML Controls  
**Purpose:** System tray context menu with dropdowns for modifiers/key selection.

**Key Components:**
- `MainWindow.xaml` | Navigation view layout (`MicaWindow`, Dark mode, rounded corners),


---

## Phase 7: Error Handling & UX Feedback (Edge Cases)  

### 7.1 Create ToastNotificationService Implementation  
**Purpose:** Non-intrusive system toasts for mic conflicts/API failures with proper error codes.

**Files:** `src/VoiceType/Services/ToastNotifictionService.cs` using Win32 notification APIs


---

## Phase 8: System Tray & Background Execution (Presence)  

### 8.1 Create WindowsNotifyIcon Service Implementation  
**Purpose:** Persistent system tray presence with right-click context menu for Settings + Exit actions,


---

## Phase 9: Unit Tests (Stability Verification)  

### 9.1 Create Comprehensive Test Suite
- `tests/VoiceType.Tests/` - .NET 8 test project targeting both STT modes  
→ Edge cases like mic conflicts | API rate limits with mocking frameworks


---

## Implementation Order Summary

**Phase 1 → Phase 2:** Foundation + Audio Cues (Lowest Risk)  
→ **Phase 3: Hotkeys, Phases 4-5: Core Engine Pipeline (Mid Complexity)**  
→ **Phase 6 & 8: UI Polish/Tray Presence (User-Facing Features)**

