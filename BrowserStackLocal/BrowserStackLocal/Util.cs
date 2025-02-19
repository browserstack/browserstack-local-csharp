using System;
using System.Diagnostics;

namespace BrowserStack
{
    public class Util {

        // Only Unix Support
        public static string[] RunShellCommand(string command, string args = "")
        {
            ProcessStartInfo psi = new ProcessStartInfo {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = command,
                Arguments = args
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

        // Using for Linux Only
        public static bool Is64BitOS()
        {
            #if NET48_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                return Environment.Is64BitOperatingSystem;
            #endif
            // https://learn.microsoft.com/en-gb/dotnet/standard/choosing-core-framework-server?WT.mc_id=dotnet-35129-website
            // linux won't be supported in .NET Framework and fallback to 64 bit
            return true;
        }
    }
}

