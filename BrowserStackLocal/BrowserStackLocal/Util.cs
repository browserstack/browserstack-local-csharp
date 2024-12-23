using System.Diagnostics;

namespace BrowserStack
{
    public class Util {

        // Only Unix Support
        public static string[] RunShellCommand(string command) {
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
        
    }
}

