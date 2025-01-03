using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Net;

using System.Security.AccessControl;
using System.Security.Principal;
using Newtonsoft.Json.Linq;

namespace BrowserStack
{
  public enum LocalState { Idle, Connecting, Connected, Error, Disconnected };

  public class BrowserStackTunnel : IDisposable
  {
    static readonly string uname = Util.GetUName();
    static readonly string binaryName = GetBinaryName();
    static readonly string downloadURL = "https://www.browserstack.com/local-testing/downloads/binaries/" + binaryName;

    static readonly string homepath = !IsWindows() ?
                                        Environment.GetFolderPath(Environment.SpecialFolder.Personal) :
                                        Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
    public static readonly string[] basePaths = new string[] {
      Path.Combine(homepath, ".browserstack"),
      Directory.GetCurrentDirectory(),
      Path.GetTempPath() };

    public int basePathsIndex = -1;
    protected string binaryAbsolute = "";
    protected string binaryArguments = "";

    protected StringBuilder output;
    public LocalState localState;
    protected string logFilePath = "";
    protected FileSystemWatcher logfileWatcher;

    Process process = null;

    static bool IsDarwin(string osName)
    {
      return osName.Contains("darwin");
    }
    

    static bool IsWindows()
    {
      return Environment.OSVersion.VersionString?.ToLower().Contains("windows") ?? false;
    }

    static bool IsLinux(string osName)
    {
      return osName.Contains("linux");
    }

    static bool IsAlpine()
    {
      try
      {
        string[] output = Util.RunShellCommand("grep", "-w 'NAME' /etc/os-release");
        return output[0]?.ToLower()?.Contains("alpine") ?? false;
      }
      catch (System.Exception ex)
      {
        Console.WriteLine("Exception while check isAlpine " + ex);
      }
      return false;
    }

    static string GetBinaryName()
    {
      if (IsWindows()) return "BrowserStackLocal.exe";
      if (IsDarwin(uname)) return "BrowserStackLocal-darwin-x64";

      if (IsLinux(uname))
      {
          if (Util.Is64BitOS())
          {
            return IsAlpine() ? "BrowserStackLocal-alpine" : "BrowserStackLocal-linux-x64";
          }
          return "BrowserStackLocal-linux-ia32";
      }

      return "BrowserStackLocal.exe";
    }

    public virtual void addBinaryPath(string binaryAbsolute)
    {
      if (binaryAbsolute == null || binaryAbsolute.Trim().Length == 0)
      {
        binaryAbsolute = Path.Combine(basePaths[++basePathsIndex], binaryName);
      }
      this.binaryAbsolute = binaryAbsolute;
    }

    public virtual void addBinaryArguments(string binaryArguments)
    {
      if (binaryArguments == null)
      {
        binaryArguments = "";
      }
      this.binaryArguments = binaryArguments;
    }

    public BrowserStackTunnel()
    {
      localState = LocalState.Idle;
      output = new StringBuilder();
    }

    public virtual void fallbackPaths()
    {
      if (basePathsIndex >= basePaths.Length - 1)
      {
        throw new Exception("Binary not found or failed to launch. Make sure that BrowserStackLocal is not already running.");
      }
      basePathsIndex++;
      binaryAbsolute = Path.Combine(basePaths[basePathsIndex], binaryName);
    }

    public void modifyBinaryPermission()
    {
      if (!IsWindows())
       {
        try
        {
          using (Process proc = Process.Start("/bin/bash", $"-c \"chmod 0755 {this.binaryAbsolute}\""))
          {
            proc.WaitForExit();
          }
        }
        catch
        {
          throw new Exception("Error in changing permission for file " + this.binaryAbsolute);
        }
      }
      else
      {
        DirectoryInfo dInfo = new DirectoryInfo(binaryAbsolute);
        DirectorySecurity dSecurity = dInfo.GetAccessControl();
        dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
        dInfo.SetAccessControl(dSecurity);
      }
    }

    public void downloadBinary()
    {
      string binaryDirectory = Path.Combine(this.binaryAbsolute, "..");
      //string binaryAbsolute = Path.Combine(binaryDirectory, binaryName);

      Directory.CreateDirectory(binaryDirectory);

      using (var client = new WebClient())
      {
        client.DownloadFile(downloadURL, this.binaryAbsolute);
      }

      if (!File.Exists(binaryAbsolute))
      {
        throw new Exception("Error accessing file " + binaryAbsolute);
      }

      modifyBinaryPermission();
    }

    public virtual void Run(string accessKey, string folder, string logFilePath, string processType)
    {
      string arguments = "-d " + processType + " ";
      if (folder != null && folder.Trim().Length != 0)
      {
        arguments += "-f " + accessKey + " " + folder + " " + binaryArguments;
      }
      else
      {
        arguments += accessKey + " " + binaryArguments;
      }
      if (!File.Exists(binaryAbsolute))
      {
        downloadBinary();
      }

      if (process != null)
      {
        process.Close();
      }

      if (processType.ToLower().Contains("start") && File.Exists(logFilePath))
      {
        File.WriteAllText(logFilePath, string.Empty);
      }
      RunProcess(arguments, processType);
    }

    private void RunProcess(string arguments, string processType)
    {
      ProcessStartInfo processStartInfo = new ProcessStartInfo()
      {
        FileName = binaryAbsolute,
        Arguments = arguments,
        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        RedirectStandardInput = true,
        UseShellExecute = false
      };

      process = new Process();
      process.StartInfo = processStartInfo;
      process.EnableRaisingEvents = true;
      DataReceivedEventHandler o = new DataReceivedEventHandler((s, e) =>
      {
        if (e.Data != null)
        {
          JObject binaryOutput = null;
          try
          {
            binaryOutput = JObject.Parse(e.Data);
          }
          catch (Exception)
          {
            SetTunnelState(LocalState.Error);
            throw new Exception($"Error while parsing JSON {e.Data}");
          }

          JToken connectionState = binaryOutput.GetValue("state");
          if (connectionState != null)
          {
            if (connectionState.ToString().ToLower().Equals("connected"))
            {
              SetTunnelState(LocalState.Connected);
            }
            else if (connectionState.ToString().ToLower().Equals("disconnected"))
            {
              SetTunnelState(LocalState.Disconnected);
              throw new Exception("Error while executing BrowserStackLocal " + processType + " " + e.Data);
            }
            else
            {
              SetTunnelState(LocalState.Error);
              throw new Exception("Error while executing BrowserStackLocal " + processType + " " + e.Data);
            }
          }
          else
          {
            JToken message = binaryOutput.GetValue("message");
            if (message != null && message.Type == JTokenType.String && message.ToString() == "BrowserStackLocal stopped successfully")
            {
              SetTunnelState(LocalState.Disconnected);
            }
          }
        }
      });

      process.OutputDataReceived += o;
      process.ErrorDataReceived += o;
      process.Exited += ((s, e) =>
      {
        process = null;
      });

      process.Start();

      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      SetTunnelState(LocalState.Connecting);
      AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) =>
      {
        Kill();
      });

      process.WaitForExit();
    }

    private void SetTunnelState(LocalState newState)
    {
      localState = newState;
    }

    public bool IsConnected()
    {
      return (localState == LocalState.Connected);
    }

    public void Kill()
    {
      if (process != null)
      {
        process.Close();
        process.Kill();
        process = null;
        SetTunnelState(LocalState.Disconnected);
      }
    }

    public void Dispose()
    {
      if (process != null)
      {
        Kill();
      }
    }
  }
}
