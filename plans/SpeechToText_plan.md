# SpeechToText (VoiceType) - Implementation Plan

**Project Difficulty:** Medium-Hard  
**Framework:** WinUI 3 / .NET 8  
**Target Platform:** Windows Desktop Application with System Tray Execution

---

## Phase 1: Project Structure & Dependencies Setup

### Files to Create/Modify
- `SpeechToText.csproj` (WinAppSDK project)
- `.gitignore`, `.vscode/**/*` files, `Directory.Build.props` if needed

**Dependencies to Add:**
```xml
<ItemGroup>
  <!-- Core Audio API -->
  <PackageReference Include="NAudio" Version="2.2.x" />
  
  <!-- MVVM Framework -->
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.1.x" />
  <PackageReference Include="Microsoft.Xaml.Behaviors.WinUI.Managed" Version="4.0/x.y" Condition="'$(TargetFramework)'=='net6.0'" />
  
  <!-- Hotkey Management -->
  <PackageReference Include="InputSimulatorPlus" Version="latest" />
  
  <!-- Speech Recognition (Offline + Cloud Toggle) -->
  <PackageReference Include="Microsoft.Speech" Version="12.x.x" /> 
  <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="30576.48114.purple_azure_cloud_xl_g1_neo_nightmare_release_elevate_captain_fabulous_villain_lucifer_jubilee_raven_megatron_kalypso_zeta_trooper_phoenix_aldrin_bionic_sonic" />
  
  <!-- Cloud API Integration -->
  <PackageReference Include="OpenAI.Microsoft.SpeechRecognition.WebApi.Client" Version="latest"/> 
</ItemGroup>

<!-- System Tray & Background Execution -->
<ItemGroup Condition="'$(TargetFramework)'=='net6.0'">
  <ContentSourceBuildTools Include="Microsoft.Windows.SDK.BuildTools" />
</ItemGroup>
```

**Entry Point:** `App.xaml` with minimal window, immediate tray hide on startup

---

## Phase 2: Core Services (Backend)

### Files to Create/Modify
- `Services/HotkeyService.cs` — Global Hotkey listener that works even when minimized  
- `Services/AudioService.cs` — Microphone capture via NAudio + temp buffer handling  
- `Services/SttService.cs` — Speech-to-text with offline/cloud toggle  

**Implementation Order:**

1. **HotkeyService**:
   - Subscribe to system-wide hotkey (Ctrl+Alt+S, etc.) using WinRT/Win32 APIs (`RegisterHotKey`)
   - Use `CompositionViewDispatcher` or background thread for non-UI events  
   - Handle edge cases: multiple instances sharing the same key

2. **AudioService**:
   - Initialize AudioGraph with selected microphone (via `MicrophoneEnumerator` from NAudio)  
   - Write raw PCM to temporary `.wav` buffer while hotkey held (`Stream.WriteAsync`)  
   - Low-latency path via dedicated thread (`Thread.Sleep(16)` between frames for non-blocking UI)

3. **SttService**:
   - Offline mode: Use `Microsoft.Speech.SpeechRecognizeResult.Text + Microsoft.Speech.AudioFormat` to read `.wav`, feed into offline recognizer  
   - Cloud mode: Send PCM samples via HTTP POST to OpenAI/Azure Whisper endpoint, parse JSON response for text string

---

## Phase 3: UI Components (Frontend)

### Files to Create/Modify
- `Pages\SettingsPage.xaml/xmbl` — Fluent Settings screen with:
  - **Hotkey Config**: Dropdowns/modifier comboboxes + single key button  
    ```xml
    <ComboBox x:Name="ModifierSelector">
      <!-- Options: Ctrl, Shift, Alt, Win -->
    </ComboBox>
    
    <Button Content="_K" Click="_OnSelectKey"/>  <!-- Dynamic label update on type change? Use KeyboardDevice to pick from list of available keycodes for better UX -->
    ```

  - **Microphone Selector**: `ComboBox` populated from device enum (show friendly names, default selected)  
  - **STT Engine Toggle**: `ToggleSwitch` binding to service flag (`IsCloudEnabled`)  
  - **Password Field** (for cloud): `<PasswordBox>` for API key entry with "Save" / "Load" toggles  

- **Mica/Acrylic Styling**: Define in `Styles/Theme.xaml`:
  ```xaml
  <Style x:Name="AppBackgroundBrush" TargetType="Border">
    <Setter Property="Background" Value="#8021262d"/> <!-- Dark Mica -->
  </Style>

  <VisualStateGroup x:Name="BackdropOpacityStates">
    <VisualState.x:Enum("Acrylic") /> 
    <x:Include VisualState.Setters BackdropAlpha="0.9" ... />
  </VisualStateGroup>
  ```

- **Beep Toggle**: `ToggleSwitch` with binding to bool flag, wired via event notification system

---

## Phase 4: Integration & Event Loop (The "Call Upon")

### Files to Create/Modify
- `Models/SettingsModel.cs`: 
  - Properties for hotkey config, mic list, STT mode  
  ```csharp
  public class SettingsModel {
      [ObservableProperty] private string _modifierKey; // CtrlShiftAltWin combination (join with +)  
      [ObservableProperty] private char _hotkeyChar;    // Single key character or F-key code  
      [ObservableProperty] private bool _isCloudEnabled; 
  }
  ```

- `App.xaml.cs` — Main entry point and DI setup:
  - Initialize HotkeyService + AudioService + STTService at startup (inject into SettingsPage)  
  - On close/exit, cleanup audio graph threads and un-register global hotkey

**Logic Flow Sequence:**
```csharp
HotKeyPressedEvent += event_args => { 
    // Phase A: User pressed key
    await Task.Run(() => AudioSession.StartRecording(temp_buffer)); 

    HotKeyPressedEvent -= event_args; 
    var stt_result = STTService.ProcessAudio(tempBuffer, isCloudEnabled); 
    
    if (stt_result.Text.IsNotNullOrEmpty()) { 
        InputSimulator.Instance.SendKeys("^v"); // or SendKey(stress_char_by_char)  
        tempBuffer.Clear();
        AudioSession.PlayEndBeep().WaitAsync(10ms).GetAwaiter().OnCompleted(() => {});
    } else if (stt_result.Error != null) { 
        NotificationService.ShowToast("STT Error", "Error:"); // error_buzz_beep()  
    }
}
```

---

## Phase 5: Polish & Edge Cases

- **Notification Toasts**: Use `Microsoft.UI.Notifications` + system tray context menu (`SystemTrayHelper`) for settings/exit actions  
- **Start with Windows Toggle**: Integrate via TaskManager API (use Win32 `SHAddToStartupFolder()`) or scheduled task registry hack if easier  

### Test Cases:
1. Hotkey pressed without mic attached → error beep + toast notification ("Mic busy")  
2. Hotkey held for > 5s before release → longer temp buffer, potential memory leak detection test  
3. Offline mode with no network but cloud toggle active → fallback behavior (retry? or use last cached results?)

---

## Final Deliverables:
- `VoiceType.exe`: Windows desktop app that runs silently in system tray  
- Fluent UI matching current Windows 11 settings aesthetic  
- Test suite covering offline/cloud modes, hotkey events, audio capture latency checks  

**README:** After all code is written and tested:
```markdown
# VoiceType — Silent Speech-to-Type Assistant

## Overview
A WinUI 3-based desktop app that lets you transcribe spoken words into text while the computer runs silently in your system tray.

- 🎤 **Hotkey Trigger**: Press `Ctrl+Shift+S` (configurable via settings)  
- 🔊 **"Beep" Feedback**: Short chime sounds indicate recording start/end  
- ⚡ **Low-Latency Capture**: NAudio + WinRT AudioGraph for instant mic response  

## Quick Start
```bash
dotnet run --project VoiceType.sln
```

Then click `App -> Run as Admin` → Press your hotkey to test.
```
