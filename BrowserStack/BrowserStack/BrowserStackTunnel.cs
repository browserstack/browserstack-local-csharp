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
    static string zipName = "BrowserStackLocal.zip";
    static string binaryName = "BrowserStackLocal.exe";
    static string downloadURL = "https://www.browserstack.com/browserstack-local/BrowserStackLocal-win32.zip";
    
    string basePath = "";
    string zipAbsolute = "";
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

    public BrowserStackTunnel(string binaryPath, string binaryArguments)
    {
      if (binaryPath == null && binaryPath.Trim() == "")
      {
        throw new Exception("The required binary path cannot be empty.");
      }
      if (binaryArguments == null)
      {
        binaryArguments = "";
      }

      this.basePath = binaryPath;
      this.zipAbsolute = Path.Combine(binaryPath, zipName);
      this.binaryAbsolute = Path.Combine(binaryPath, binaryName);

      this.localState = LocalState.Idle;
      this.output = new StringBuilder();
      this.binaryArguments = binaryArguments;
    }

    public void downloadBinary()
    {
      Directory.CreateDirectory(this.basePath);

      using (var client = new WebClient())
      {
        Local.logger.Info("Downloading BrowserStackLocal..");
        client.DownloadFile(downloadURL, this.zipAbsolute);
        Local.logger.Info("Binary Downloaded.");
      }

      if (!File.Exists(this.zipAbsolute))
      {
        Local.logger.Error("Error accessing downloaded zip. Please check file permissions.");
        throw new Exception("Error accessing file " + this.zipAbsolute);
      }

      using (ZipInputStream s = new ZipInputStream(File.OpenRead(this.zipAbsolute)))
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
            using (FileStream streamWriter = File.Create(this.binaryAbsolute))
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

      DirectoryInfo dInfo = new DirectoryInfo(this.zipAbsolute);
      DirectorySecurity dSecurity = dInfo.GetAccessControl();
      dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
      dInfo.SetAccessControl(dSecurity);

      File.Delete(this.zipAbsolute);
      Local.logger.Info("Binary Extracted");
    }

    public void Run()
    {
      if (!File.Exists(binaryAbsolute))
      {
        Local.logger.Warn("BrowserStackLocal binary was not found.");
        downloadBinary();
      }

      if (this.process != null)
      {
        this.process.Refresh();
        if (!this.process.HasExited)
        {
          try
          {
            this.process.Kill();
          } catch (Exception) { }
        }
      }

      ProcessStartInfo processStartInfo = new ProcessStartInfo()
      {
        FileName = binaryAbsolute,
        Arguments = binaryArguments,
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

      this.process.OutputDataReceived += o;
      this.process.ErrorDataReceived += o;
      this.process.Exited += ((s, e) =>
      {
        Kill();
        this.process = null;
      });

      this.process.Start();
      
      this.job = new Job();
      this.job.AddProcess(process.Handle);
      
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

    public bool IsConnectedOrConnecting()
    {
      return (localState == LocalState.Connected || localState == LocalState.Connecting);
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
