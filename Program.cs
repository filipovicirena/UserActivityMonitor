using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Management;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.Serialization.Formatters.Binary;

namespace UserActivityCore
{
    internal class Program
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        static string GetUsernameForProcess(int processId)
        {
            string query = "SELECT * FROM Win32_Process WHERE ProcessId = " + processId;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string[] ownerInfo = new string[2];
                    obj.InvokeMethod("GetOwner", ownerInfo);
                    return ownerInfo[0]; // username
                }
            }
            return "unknown";
        }
        static Dictionary<string, bool> lastKnownWebcamState = new Dictionary<string, bool>();
        static void CheckWebcamUsage()
        {
            string webcamKeyPath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";

            using (RegistryKey webcamKey = Registry.CurrentUser.OpenSubKey(webcamKeyPath))
            {
                if (webcamKey != null)
                {
                    string[] appNames = webcamKey.GetSubKeyNames();

                    foreach (string appName in appNames)
                    {
                        using (RegistryKey appKey = webcamKey.OpenSubKey(appName))
                        {
                            object startValue = appKey.GetValue("LastUsedTimeStart");
                            object stopValue = appKey.GetValue("LastUsedTimeStop");

                            if (startValue == null) continue;

                            long startFileTime = (long)startValue;
                            long stopFileTime = (stopValue != null) ? (long)stopValue : 0;

                            bool isActive = startFileTime > stopFileTime;
                            bool previouslyActive;
                            bool hasPreviousState = lastKnownWebcamState.TryGetValue(appName, out previouslyActive);

                            if (!hasPreviousState || previouslyActive != isActive)
                            {
                                // status changed (or this is the first time we've seen this app) - log it
                                Console.WriteLine(appName + " status changed -> Active: " + isActive);
                                
                                DateTime eventTimeUtc = DateTime.UtcNow;
                                string currentUser = Environment.UserName;

                                File.AppendAllText(@"C:\log-camera.txt", eventTimeUtc + " | User: "+ currentUser + " | App: " + appName + " | Status: " + (isActive ? "Active" : "Inactive") + Environment.NewLine);
                                File.AppendAllText(@"C:\log-events.txt", eventTimeUtc + " | User: " + currentUser + " | App: " + appName + " | Status: " + (isActive ? "Active" : "Inactive") + Environment.NewLine);
                                lastKnownWebcamState[appName] = isActive;
                            }

                            DateTime startTime = DateTime.FromFileTimeUtc(startFileTime);
                            DateTime stopTime = (stopFileTime != 0) ? DateTime.FromFileTimeUtc(stopFileTime) : DateTime.MinValue;

                        }
                    
                    }

                }
            }
        }
        static Dictionary<string, bool> lastKnownMicrophoneState = new Dictionary<string, bool>();
        static void CheckMicrophoneUsage()
        {
            string microphoneKeyPath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone";

            using (RegistryKey microphoneKey = Registry.CurrentUser.OpenSubKey(microphoneKeyPath))
            {
                if (microphoneKey != null)
                {
                    string[] appNames = microphoneKey.GetSubKeyNames();

                    foreach (string appName in appNames)
                    {
                        using (RegistryKey appKey = microphoneKey.OpenSubKey(appName))
                        {
                            object startValue = appKey.GetValue("LastUsedTimeStart");
                            object stopValue = appKey.GetValue("LastUsedTimeStop");

                            if (startValue == null) continue;

                            long startFileTime = (long)startValue;
                            long stopFileTime = (stopValue != null) ? (long)stopValue : 0;

                            bool isActive = startFileTime > stopFileTime;
                            bool previouslyActive;
                            bool hasPreviousState = lastKnownMicrophoneState.TryGetValue(appName, out previouslyActive);

                            if (!hasPreviousState || previouslyActive != isActive)
                            {
                                // status changed (or this is the first time we've seen this app) - log it
                                Console.WriteLine(appName + " status changed -> Active: " + isActive);

                                DateTime eventTimeUtc = DateTime.UtcNow;
                                string currentUser = Environment.UserName;

                                File.AppendAllText(@"C:\log-microphone.txt", eventTimeUtc + " | User: " + currentUser + " | App: " + appName + " | Status: " + (isActive ? "Active" : "Inactive") + Environment.NewLine);
                                File.AppendAllText(@"C:\log-events.txt", eventTimeUtc + " | User: " + currentUser + " | App: " + appName + " | Status: " + (isActive ? "Active" : "Inactive") + Environment.NewLine);

                                lastKnownMicrophoneState[appName] = isActive;
                            }

                            DateTime startTime = DateTime.FromFileTimeUtc(startFileTime);
                            DateTime stopTime = (stopFileTime != 0) ? DateTime.FromFileTimeUtc(stopFileTime) : DateTime.MinValue;

                        }

                    }

                }
            }
        }

        static string ExtractField(string message, string fieldLabel)
        {
            int labelIndex = message.IndexOf(fieldLabel);
            if (labelIndex == -1) return "unknown";

            int valueStart = labelIndex + fieldLabel.Length;
            int lineEnd = message.IndexOf('\n', valueStart);
            if (lineEnd == -1) lineEnd = message.Length;

            string value = message.Substring(valueStart, lineEnd - valueStart).Trim();
            return value;
        }

        static void SaveBookmark(EventBookmark bookmark)
        {
            using (FileStream fs = new FileStream(@"C:\log-open-log-file.bookmark", FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, bookmark);
            }
        }

        static EventBookmark LoadBookmark()
        {
            string path = @"C:\log-open-log-file.bookmark";

            if (!File.Exists(path))
            {
                return null;
            }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (EventBookmark)formatter.Deserialize(fs);
            }
        }
        static EventBookmark lastBookmark = LoadBookmark();

        static void CheckLogFileAccess()
        {
            string query = "*[System[(EventID=4663 or EventID=4656)] and EventData[Data[@Name='ObjectName']='C:\\log-events.txt']]";

            EventLogQuery eventsQuery = new EventLogQuery("Security", PathType.LogName, query);

            using (EventLogReader reader = new EventLogReader(eventsQuery, lastBookmark))
            {
                EventRecord record;
                while ((record = reader.ReadEvent()) != null)
                {
                    using (record)
                    {
                        string fullMessage = record.FormatDescription();
                        string accountName = ExtractField(fullMessage, "Account Name:");

                        string logLine = record.TimeCreated + " | User: " + accountName + " | Event: File opened";
                        File.AppendAllText(@"C:\log-open-log-file.txt", logLine + Environment.NewLine);
                        Console.WriteLine("Log entry added for file access: " + logLine);
                        lastBookmark = record.Bookmark;
                        SaveBookmark(lastBookmark);
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            
            Console.WriteLine("Monitoring focus changes, webcam and microphone usage. Press Ctrl+C to stop.");

            IntPtr lastHandle = IntPtr.Zero;

            while (true)
            {
                CheckLogFileAccess();
                IntPtr currentHandle = GetForegroundWindow();

                if (currentHandle != lastHandle)
                {
                    StringBuilder titleBuffer = new StringBuilder(256);
                    GetWindowText(currentHandle, titleBuffer, titleBuffer.Capacity);
                    string title = titleBuffer.ToString();
                    DateTime nowUtc = DateTime.UtcNow;
                    uint processId;
                    GetWindowThreadProcessId(currentHandle, out processId);
                    string username = GetUsernameForProcess((int)processId);

                    try
                    {
                        Process proc = Process.GetProcessById((int)processId);
                        string exeName = proc.ProcessName;
                        string exePath = proc.MainModule.FileName;
                        int sessionId = proc.SessionId;

                        string logLine = nowUtc + " | PID: " + processId +
                            " | User: " + username +
                            " | Exe: " + exeName +
                            " | Path: " + exePath +
                            " | Session ID: " + sessionId +
                            " | Title: " + title;

                        File.AppendAllText(@"C:\log-app.txt", logLine + Environment.NewLine);

                        Console.WriteLine("Focus changed -> " + title +
                            " | PID: " + processId +
                            " | User: " + username +
                            " | UTC: " + nowUtc +
                            " | Exe: " + exeName +
                            " | Path: " + exePath +
                            " | Session ID: " + sessionId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Focus changed -> " + title +
                            " | PID: " + processId +
                            " | UTC: " + nowUtc +
                            " | User: " + username +
                            " | (couldn't read process info: " + ex.Message + ")");
                    }
                }

                    lastHandle = currentHandle;

                CheckWebcamUsage();
                CheckMicrophoneUsage();

                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
