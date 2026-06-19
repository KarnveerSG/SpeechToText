The Prompt

Role: Act as a Senior Windows Desktop Developer specializing in WinUI 3, .NET 8, and low-level Windows Audio APIs.

Project Goal: Build a lightweight Windows application called "VoiceType". It must run silently in the system tray (background), intercept a configurable global keyboard shortcut, and perform "Push-to-Talk" (PTT) Speech-to-Text (STT) transcription, pasting the resulting text into the currently active application.

Core Functional Requirements:

    Background Execution (System Tray):

        The application must start with Windows (optional toggle in settings).

        On launch, it should minimize to the System Tray (NotifyIcon). No main window should open by default.

        A right-click context menu on the tray icon must provide: "Settings" and "Exit".

    Global Hotkey (Push-to-Talk):

        The app must register a global system-wide hotkey that works even when the app is minimized or out of focus.

        Configuration: The user must be able to configure the hotkey via the Settings UI. They should be able to set:

            Modifier Keys: Ctrl, Shift, Alt, Win (or any combination).

            Key: Any letter/number/F-key.

        Behavior: When the user holds the configured button down, recording starts immediately. When they release it, recording stops and transcription begins.

    Audio Input & Feedback (The "Beep"):

        Low Latency: Use NAudio or WinRT AudioGraph to capture microphone audio with minimal delay.

        Start Beep: The moment the button is pressed, the system must play a short, pleasant, low-frequency "chime" (e.g., 800Hz for 100ms) to indicate the mic is live.

        End Beep/Error: When the button is released, play a slightly higher beep (e.g., 1200Hz) to indicate processing has started. If the microphone fails to initialize, play a harsh "error" buzz.

        Note: The beep must be non-intrusive (low volume) and should not be recorded into the audio stream.

    Speech-to-Text Engine:

        Use the Windows Speech Recognition API (Microsoft.Speech or Windows.Media.SpeechRecognition) for offline use, OR integrate the OpenAI Whisper API (or Azure Cognitive Services) for higher accuracy.

        Decision: Provide a toggle in settings for "Offline (Fast)" vs "Cloud (Accurate)".

        Output: Once the text is transcribed, the app must simulate keyboard input (e.g., SendKeys or InputSimulator) to paste the text directly into the active window (Word, Browser, Notepad, etc.) instantly.

    User Interface & Settings (Modern Aesthetic):

        The settings window must be Modern, Fluent, and Minimalist (think Windows 11 style).

        Use Mica or Acrylic backgrounds.

        Dark/Light Mode: The UI must respect the system theme or have a manual toggle.

        Settings to include:

            Hotkey Configuration (Drop-downs for Modifier + Key).

            Microphone Selection (Drop-down list of available input devices).

            STT Engine Toggle (Offline vs. Cloud).

            Cloud API Key Input (Password field for OpenAI/Azure key).

            "Start with Windows" Toggle.

            Play Beep Toggle (Allow users to turn the beep off).

Technical Constraints & Architecture:

    Framework: WinUI 3 (Desktop) or WPF with .NET 8.

    Dependency Injection: Use a DI container to manage services (HotkeyService, AudioService, STTService).

    MVVM: Use the Model-View-ViewModel pattern (e.g., CommunityToolkit.MVVM).

    Error Handling: If the mic is in use by another app, show a system toast notification (Windows Notification) and play the error beep.

    Performance: The audio capture must run on a separate thread to avoid blocking the UI when the button is held down.

Logic Flow (The "Call Upon" sequence):

    User presses Hotkey (e.g., Ctrl + Space).

    System plays "Start Beep".

    App captures audio from the selected mic and writes to a temporary buffer (.wav or .mp3 in %TEMP%).

    User releases Hotkey.

    System plays "End Beep".

    App stops recording and sends the audio buffer to the selected STT engine.

    App receives the text string.

    App simulates Ctrl + V (Paste) or types the text character-by-character into the active foreground window.

    App clears the temporary buffer.

UI/UX Design Description for the Prompter (Image Generation):

If you are using a tool like Midjourney to design the UI first:

    "Create a Windows 11 Settings-style interface. It features a navigation header at the top with the app name 'VoiceType'. The left pane contains a vertical list of settings categories: 'General', 'Hotkey', 'Microphone', and 'Speech Engine'. The main content pane shows the Hotkey configuration: two modern dropdown boxes side-by-side (Modifier: 'Ctrl + Alt' and Key: 'Space') with a 'Record' button next to it. The theme is Dark Mode with Mica transparency, rounded corners, and a subtle glassmorphism effect. In the bottom right corner, a small toggle switch labeled 'Play Audio Cues' is turned ON."*

Initial Code Skeleton Request:
Please start by generating the project structure (.csproj) with the required NuGet packages (NAudio, Microsoft.Windows.SDK.BuildTools, CommunityToolkit.Mvvm, and InputSimulatorPlus). Then, implement the System Tray icon and the Global Hotkey listener class.


    and finally create a accurate README.mb once you are done