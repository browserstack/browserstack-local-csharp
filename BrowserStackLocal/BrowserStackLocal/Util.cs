using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BrowserStack
{
    public class Util {

        // Ref: https://stackoverflow.com/a/336729
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        // Only Unix Support
        public static string[] RunShellCommand(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo("bash", $"-c \"{command}\"") {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = psi };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new string[]{output, error};
        }

        public static string GetUName()
        {
            string osName = "";
            try
            {
                string[] output = RunShellCommand("uname");
                osName = output[0]?.ToLower();
            }
            catch (System.Exception) {}
            return osName;
        }

        public static bool Is64BitOS()
        {
            #if NET48_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                return Environment.Is64BitOperatingSystem;
            #endif
            bool is64BitProcess = IntPtr.Size == 8;
            return is64BitProcess || InternalCheckIsWow64();
        }

        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }
    }
}

