# UserActivityMonitor

A small Windows utility that monitors user activity and logs app usage information.

## What the app does
- Tracks the foreground window and records the active app title
- Logs process details such as PID, user, executable path, and session ID
- Detects webcam usage changes for installed apps
- Detects microphone usage changes for installed apps
- Watches the Windows Security log for access to the monitored log file
- Writes activity data to files under `C:\`

## Requirements
- Windows 10/11
- Visual Studio or .NET build tools
- Run as Administrator

## How to run
1. Open the project in Visual Studio.
2. Build the solution.
3. Run the app as Administrator.
4. The app will begin monitoring automatically.

Press `Ctrl+C` in the console to stop it.

## Important
This app writes log files to `C:\` and reads Windows event logs, so it must be run with elevated privileges.
