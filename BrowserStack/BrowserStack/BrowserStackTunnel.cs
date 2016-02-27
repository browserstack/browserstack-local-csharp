using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

using ICSharpCode.SharpZipLib.Zip;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace BrowserStack
{
  public enum LocalState { Idle, Connecting, Connected, Error, Disconnected };

  internal class BrowserStackTunnel : IDisposable
  {
    static readonly string zipName = "BrowserStackLocal.zip";
    static readonly string binaryName = "BrowserStackLocal.exe";
    static readonly string downloadURL = "https://www.browserstack.com/browserstack-local/BrowserStackLocal-win32.zip";
    public static readonly string[] basePaths = new string[] {
      Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack"),
      Directory.GetCurrentDirectory(),
      Path.GetTempPath() };

    int basePathsIndex = -1;
    string binaryAbsolute = "";
    string binaryArguments = "";
    public AutoResetEvent connectingEvent = new AutoResetEvent(false);

    StringBuilder output;
    public LocalState localState;

    Job job = null;
    Process process = null;

    private static Dictionary<LocalState, Regex> stateMatchers = new Dictionary<LocalState, Regex>() {
      { LocalState.Connected, new Regex(@"Press Ctrl-C to exit.*", RegexOptions.Multiline) },
      { LocalState.Error, new Regex(@"\s*\*\*\* Error:\s+(.*).*", RegexOptions.Multiline) }
    };

    public BrowserStackTunnel(string binaryAbsolute, string binaryArguments)
    {
      if (binaryAbsolute == null || binaryAbsolute.Trim().Length == 0)
      {
        binaryAbsolute = Path.Combine(basePaths[++basePathsIndex], binaryName);
      }
      this.binaryAbsolute = binaryAbsolute;

      if (binaryArguments == null)
      {
        binaryArguments = "";
      }

      localState = LocalState.Idle;
      output = new StringBuilder();
      this.binaryArguments = binaryArguments;
    }

    public void fallbackPaths()
    {
      if (basePathsIndex >= basePaths.Length - 1)
      {
        throw new Exception("No More Paths to try. Please specify a binary path in options.");
      }
      basePathsIndex++;
      binaryAbsolute = Path.Combine(basePaths[basePathsIndex], binaryName);
    }
    public void downloadBinary()
    {
      string binaryDirectory = Path.Combine(binaryAbsolute, "..");
      string zipAbsolute = Path.Combine(binaryDirectory, zipName);

      Directory.CreateDirectory(binaryDirectory);

      using (var client = new WebClient())
      {
        Local.logger.Info("Downloading BrowserStackLocal..");
        client.DownloadFile(downloadURL, zipAbsolute);
        Local.logger.Info("Binary Downloaded.");
      }

      if (!File.Exists(zipAbsolute))
      {
        Local.logger.Error("Error accessing downloaded zip. Please check file permissions.");
        throw new Exception("Error accessing file " + zipAbsolute);
      }

      using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipAbsolute)))
      {
        ZipEntry theEntry;
        while ((theEntry = s.GetNextEntry()) != null)
        {
          string directoryName = Path.GetDirectoryName(theEntry.Name);
          if (directoryName.Length > 0)
          {
            Directory.CreateDirectory(directoryName);
          }
          string fileName = Path.GetFileName(theEntry.Name);
          if (fileName != String.Empty)
          {
            using (FileStream streamWriter = File.Create(binaryAbsolute))
            {
              int size = 2048;
              byte[] data = new byte[2048];
              while (true)
              {
                size = s.Read(data, 0, data.Length);
                if (size > 0)
                {
                  streamWriter.Write(data, 0, size);
                }
                else
                {
                  break;
                }
              }
            }
          }
        }
        s.Close();
      }

      DirectoryInfo dInfo = new DirectoryInfo(binaryAbsolute);
      DirectorySecurity dSecurity = dInfo.GetAccessControl();
      dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
      dInfo.SetAccessControl(dSecurity);

      File.Delete(zipAbsolute);
      Local.logger.Info("Binary Extracted");
    }

    public void Run(string accessKey, string folder)
    {
      string arguments = "";
      if (folder != null && folder.Trim().Length != 0)
      {
        arguments = "-f " + accessKey + " " + folder + " " + binaryArguments;
      }
      else
      {
        arguments = accessKey + " " + binaryArguments;
      }
      if (!File.Exists(binaryAbsolute))
      {
        Local.logger.Warn("BrowserStackLocal binary was not found at " + binaryAbsolute);
        downloadBinary();
      }

      if (process != null)
      {
        process.Close();
      }

      Local.logger.Info("BrowserStackLocal binary is located at " + binaryAbsolute);
      Local.logger.Info("Starting Binary with arguments " + arguments.Replace(accessKey, "<access_key>"));
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
          output.Append(e.Data);
          Local.binaryLogger.Info(e.Data);
          
          foreach (KeyValuePair<LocalState, Regex> kv in stateMatchers)
          {
            Match m = kv.Value.Match(e.Data);
            if (m.Success)
            {
              if (localState != kv.Key)
                TunnelStateChanged(localState, kv.Key);

              localState = kv.Key;
              output.Clear();
              Local.logger.Info("TunnelState: " + localState.ToString());
              break;
            }
          }
        }
      });

      process.OutputDataReceived += o;
      process.ErrorDataReceived += o;
      process.Exited += ((s, e) =>
      {
        Kill();
        process = null;
      });

      process.Start();
      
      job = new Job();
      job.AddProcess(process.Handle);
      
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      TunnelStateChanged(LocalState.Idle, LocalState.Connecting);

      AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => Kill());

      connectingEvent.WaitOne();
    }

    private void TunnelStateChanged(LocalState prevState, LocalState state)
    {
      if (state != LocalState.Connecting)
      {
        connectingEvent.Set();
      }
      Local.logger.Info("Current tunnel state " + state);
    }

    public bool IsConnected()
    {
      return (localState == LocalState.Connected);
    }

    public void Kill()
    {
      try
      {
        this.process.Kill();
        this.process = null;
        this.job.Close();
        this.job = null;
      }
      catch (Exception e)
      {
        Local.logger.Error("Error killing: " + e.Message);
      }
      finally
      {
        if (localState != LocalState.Disconnected)
          TunnelStateChanged(localState, LocalState.Disconnected);

        localState = LocalState.Disconnected;
        Local.logger.Info("TunnelState: " + localState.ToString());
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      Kill();
    }
  }
}
