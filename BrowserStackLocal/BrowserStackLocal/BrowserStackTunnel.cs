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
    static readonly string binaryName = isDarwin() ? "BrowserStackLocal-darwin-x64" : "BrowserStackLocal.exe";
    static readonly string downloadURL = isDarwin() ?
                                        "https://bstack-local-prod.s3.amazonaws.com/BrowserStackLocal-darwin-x64" :
                                        "https://bstack-local-prod.s3.amazonaws.com/BrowserStackLocal.exe";
    static readonly string homepath = isDarwin() ?
                                        Environment.GetFolderPath(Environment.SpecialFolder.Personal) :
                                        Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
    public static readonly string[] basePaths = new string[] {
      Path.Combine(homepath, ".browserstack"),
      Directory.GetCurrentDirectory(),
      Path.GetTempPath() };

    int basePathsIndex = -1;
    protected string binaryAbsolute = "";
    protected string binaryArguments = "";
    
    protected StringBuilder output;
    public LocalState localState;
    protected string logFilePath = "";
    protected FileSystemWatcher logfileWatcher;

    Process process = null;

    static Boolean isDarwin()
    {
      OperatingSystem os = Environment.OSVersion;
      return os.Platform.ToString() == "Unix";
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
      if (isDarwin())
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
          try {
            binaryOutput = JObject.Parse(e.Data);
          } catch (Exception) {
            throw new Exception($"Error while parsing JSON {e.Data}");
          }
          if(binaryOutput.GetValue("state") != null && !binaryOutput.GetValue("state").ToString().ToLower().Equals("connected"))
          {
            throw new Exception("Eror while executing BrowserStackLocal " + processType + " " + e.Data);
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

      TunnelStateChanged(LocalState.Idle, LocalState.Connecting);
      AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => Kill());

      process.WaitForExit();
    }

    private void TunnelStateChanged(LocalState prevState, LocalState state) { }

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
        localState = LocalState.Disconnected;
      }
    }

    public void Dispose()
    {
      if(process != null)
      {
        Kill();
      }
    }
  }
}
