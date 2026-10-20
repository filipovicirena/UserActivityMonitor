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
        static void Main(string[] args)
        {
            Console.WriteLine("Monitoring focus changes. Press Ctrl+C to stop.");

            IntPtr lastHandle = IntPtr.Zero;

            while (true)
            {
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
                

                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
