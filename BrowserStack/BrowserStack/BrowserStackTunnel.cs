using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.IO.Compression;

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

    int lastPid = 0;
    StringBuilder output;
    public LocalState localState;

    string lastError;

    List<Process> processes = new List<Process>();
    List<Job> jobs = new List<Job>();
    private Action<LocalState> callbackFunction = null;

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
        Console.WriteLine("Downloading BrowserStackLocal..");
        client.DownloadFile(downloadURL, this.binaryAbsolute);
        Console.WriteLine("Binary Downloaded.");
      }
    }

    public void Run(Action<LocalState> callOnStateChange)
    {
      this.callbackFunction = callOnStateChange;
      if (!File.Exists(binaryAbsolute))
      {
        Console.WriteLine("BrowserStackLocal binary was not found.");
        downloadBinary();
      }

      if (lastPid > 0)
        KillByPid(lastPid);

      if(this.verbose == true)
      {
        Console.WriteLine("Local Started with Arguments: " + binaryArguments.Remove(0, 20));
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

      Process process = new Process();
      process.StartInfo = processStartInfo;
      process.EnableRaisingEvents = true;
      DataReceivedEventHandler o = new DataReceivedEventHandler((s, e) =>
      {
        if (e.Data != null)
        {
          output.Append(e.Data);
          if(this.verbose == true)
          {
            Console.WriteLine("BinaryOutput: " + e.Data);
          }

          foreach (KeyValuePair<LocalState, Regex> kv in stateMatchers)
          {
            Match m = kv.Value.Match(e.Data);
            if (m.Success)
            {
              if (kv.Key == LocalState.Error && m.Groups.Count > 1 && m.Groups[1].Captures.Count > 0)
                lastError = m.Groups[1].Captures[0].Value;

              if (localState != kv.Key)
                TunnelStateChanged(localState, kv.Key);

              localState = kv.Key;
              output.Clear();
              if(this.verbose == true)
              {
                Console.WriteLine("TunnelState: " + localState.ToString());
              }
              break;
            }
          }
        }
      });

      process.OutputDataReceived += o;
      process.ErrorDataReceived += o;
      process.Exited += ((s, e) =>
      {
        if (lastPid == process.Id)
        {
          lastPid = -1;
        }

        Kill();
        processes.Remove(process);
      });

      ThreadStart ths = new ThreadStart(() =>
      {
        process.Start();
        processes.Add(process);

        lastPid = process.Id;

        Job job = new Job();
        job.AddProcess(process.Handle);
        jobs.Add(job);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // process.WaitForExit();
        // process.CancelOutputRead();
        // process.CancelErrorRead();
      });

      lastError = null;
      TunnelStateChanged(LocalState.Idle, LocalState.Connecting);

      Thread th = new Thread(ths);
      th.Start();

      AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => Kill());
    }

    private void TunnelStateChanged(LocalState prevState, LocalState state)
    {
      if(this.callbackFunction != null)
      {
        this.callbackFunction(state);
      }
    }

    public bool IsConnected()
    {
      return (localState == LocalState.Connected);
    }

    public bool IsConnectedOrConnecting()
    {
      return (localState == LocalState.Connected || localState == LocalState.Connecting);
    }

    public string getLastError()
    {
      return lastError;
    }

    public void Destroy()
    {
      Kill();
    }

    public void Kill()
    {
      try
      {
        foreach (Process p in processes)
        {
          if (!p.HasExited)
            p.Kill();
        }

        processes.Clear();

        foreach (Job j in jobs)
        {
          j.Close();
        }

        jobs.Clear();
      }
      catch (Exception e)
      {
        Console.WriteLine("Error killing: " + e.Message);
      }
      finally
      {
        if (localState != LocalState.Disconnected)
          TunnelStateChanged(localState, LocalState.Disconnected);

        localState = LocalState.Disconnected;
        Console.WriteLine("TunnelState: " + localState.ToString());
      }
    }

    public static void KillByPid(int pid)
    {
      try
      {
        Process p = Process.GetProcessById(pid);
        if (p != null && !p.HasExited)
        {
          Console.WriteLine("Killing process with pid {0} {1}", p.Id, p.MainWindowTitle);
          p.Kill();
        }
      }
      catch (Exception)
      {
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
