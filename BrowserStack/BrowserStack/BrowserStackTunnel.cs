using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using log4net;

namespace BrowserStack
{
  public enum LocalState { Idle, Connecting, Connected, Error, Disconnected };

  internal class BrowserStackTunnel : IDisposable
  {
    static string binaryName = "BrowserStackLocal.exe";
    static string downloadURL = "https://s3.amazonaws.com/browserStack/browserstack-local/BrowserStackLocal.exe";

    bool verbose = false;
    string basePath = "";
    string binaryAbsolute = "";
    string binaryArguments = "";

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
      this.binaryAbsolute = Path.Combine(binaryPath, binaryName);

      this.localState = LocalState.Idle;
      this.output = new StringBuilder();
      this.binaryArguments = binaryArguments;
    }

    public void logVerbose()
    {
      this.verbose = true;
    }

    public void downloadBinary()
    {
      Directory.CreateDirectory(this.basePath);

      using (var client = new WebClient())
      {
        Local.logger.Info("Downloading BrowserStackLocal..");
        client.DownloadFile(downloadURL, this.binaryAbsolute);
        Local.logger.Info("Binary Downloaded.");
      }
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

      if (this.verbose == true)
      {
        Local.logger.Info("Local Started with Arguments: " + binaryArguments.Remove(0, 20));
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
          if (this.verbose == true)
          {
            Local.binaryLogger.Info(e.Data);
          }

          foreach (KeyValuePair<LocalState, Regex> kv in stateMatchers)
          {
            Match m = kv.Value.Match(e.Data);
            if (m.Success)
            {
              if (localState != kv.Key)
                TunnelStateChanged(localState, kv.Key);

              localState = kv.Key;
              output.Clear();
              if (this.verbose == true)
              {
                Local.logger.Info("TunnelState: " + localState.ToString());
              }
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
    }

    private void TunnelStateChanged(LocalState prevState, LocalState state)
    {
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
