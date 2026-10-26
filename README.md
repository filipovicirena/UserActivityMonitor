# UserActivityMonitor

A lightweight Windows desktop monitoring utility written in C# that logs user activity, active application focus changes, webcam and microphone usage, and access to a monitored log file.

This project is intended for local activity tracking and auditing on Windows systems. It can record:

- Active window changes
- Process information for the foreground app
- Webcam usage state changes by app
- Microphone usage state changes by app
- Windows Security log access events for a specific file

## Features

- Tracks the currently focused window and logs:
  - timestamp (UTC)
  - PID
  - user name
  - executable name
  - executable path
  - session ID
  - window title
- Detects when focused apps change by comparing the previous foreground window handle.
- Reads webcam and microphone usage state from the Windows Consent Store registry:
  - `Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam`
  - `Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone`
- Writes status changes to log files such as:
  - `C:\log-app.txt`
  - `C:\log-camera.txt`
  - `C:\log-microphone.txt`
  - `C:\log-open-log-file.txt`
  - `C:\log-events.txt`
- Monitors Security event log entries for file access events involving `C:\log-events.txt`.
- Persists the Event Log bookmark so monitoring can continue from the last read event.

## Requirements

- Windows 10 or Windows 11
- .NET Framework or compatible C# build environment
- Visual Studio or another C# compiler
- Administrative privileges are recommended for reading Windows Security events and writing to system-protected paths

## Important Notes

This application writes logs directly to the system drive (`C:\...`). In a real deployment, you should:

- confirm the target folder is appropriate for your environment
- ensure permissions are correct
- review privacy and compliance requirements before monitoring user activity
- consider using a safer/less invasive logging approach depending on your scenario

## Build and Run

1. Open the project in Visual Studio.
2. Restore NuGet dependencies if necessary.
3. Build the solution.
4. Run the executable.
5. The app will begin monitoring immediately and continue in a loop until terminated.

Example launch flow:

```bash
# After building in Visual Studio:
UserActivityMonitor.exe
```

## Runtime Behavior

The application runs an infinite loop and checks:

- event log access events
- foreground window changes
- webcam state
- microphone state

Every 500 ms, it sleeps between checks.

## Log Files

The app writes to the following files on the local machine:

- `C:\log-app.txt` — foreground window / process activity
- `C:\log-camera.txt` — webcam status changes
- `C:\log-microphone.txt` — microphone status changes
- `C:\log-open-log-file.txt` — log file access events
- `C:\log-events.txt` — main event log file
- `C:\log-open-log-file.bookmark` — saved Event Log bookmark

## Example Output

A foreground window change may be logged like this:

```text
2026-10-26T12:34:56.789Z | PID: 1234 | User: DOMAIN\username | Exe: chrome | Path: C:\Program Files\Google\Chrome\Application\chrome.exe | Session ID: 1 | Title: Example - Google Chrome
```

A webcam state change may be logged like this:

```text
2026-10-26T12:35:00.123Z | User: username | App: MicrosoftTeams | Status: Active
```

## Security and Privacy Considerations

This code captures user activity and process metadata, which may be sensitive. Use responsibly and only in environments where it is authorized and compliant with relevant policies.

## License

This project does not include a license file in the repository snapshot provided here. If you are using it in a production environment, add an appropriate license before distribution.

## Notes for Developers

The application relies on Windows-specific APIs and features, including:

- `user32.dll` Win32 functions
- WMI queries via `System.Management`
- Windows Registry reads
- Windows Security Event Log query access
- `BinaryFormatter` for bookmark persistence

If you plan to extend or harden this project, consider:

- validating registry keys before reading them
- handling exceptions more gracefully
- avoiding hardcoded paths in production code
- supporting configurable log locations
- securing or encrypting output logs if they contain sensitive data
- adding a proper shutdown mechanism instead of running indefinitely

## Contact

This project appears to be a personal or experimental monitoring utility. For repository-specific questions, use the project owner and repository details in the source host.
