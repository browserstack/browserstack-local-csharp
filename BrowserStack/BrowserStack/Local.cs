using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BrowserStack
{
  public class Local
  {
    private string accessKey = "";
    private bool logVerbose = false;
    private string binaryPath = null;
    private string argumentString = "";
    private BrowserStackTunnel local = null;
    private static KeyValuePair<string, string> emptyStringPair = new KeyValuePair<string, string>();

    private Action<LocalState> stateChangeCallback = null;

    private static List<KeyValuePair<string, string>> valueCommands = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("localidentifier", "-localIdentifier"),
      new KeyValuePair<string, string>("hosts", ""),
      new KeyValuePair<string, string>("proxyhost", "-proxyHost"),
      new KeyValuePair<string, string>("proxyport", "-proxyPort"),
      new KeyValuePair<string, string>("proxyuser", "-proxyUser"),
      new KeyValuePair<string, string>("proxypass", "-proxyPass"),
    };

    private static List<KeyValuePair<string, string>> booleanCommands = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("verbose", "-v"),
      new KeyValuePair<string, string>("forcelocal", "-forcelocal"),
      new KeyValuePair<string, string>("onlyautomate", "-onlyAutomate"),
    };
    private void callOnStateChange(LocalState state)
    {
      Console.WriteLine("Current State " + state);
      if (stateChangeCallback != null)
      {
        stateChangeCallback(state);
      }
    }
    
    public bool isRunning()
    {
      if (this.local == null) return false;
      return (this.local.localState == LocalState.Connected);
    }

    public Local(Action<LocalState> stateChangeCallback)
    {
      this.stateChangeCallback = stateChangeCallback;
    }

    private void addArgs(string key, string value)
    {
      KeyValuePair<string, string> result;
      key = key.Trim().ToLower();

      if (key.Equals("key"))
      {
        this.accessKey = value;
      }
      if (key.Equals("path"))
      {
        this.binaryPath = value;
      }

      result = valueCommands.Find(pair => pair.Key == key);
      if (!result.Equals(emptyStringPair))
      {
        this.argumentString += result.Value + " " + value + " ";
      }

      result = booleanCommands.Find(pair => pair.Key == key);
      if (!result.Equals(emptyStringPair))
      {
        if (value.Trim().ToLower() == "true")
        {
          this.argumentString += result.Value + " ";
        }
      }
    }

    public void verboseMode()
    {
      this.logVerbose = true;
    }

    public void start(List<KeyValuePair<string, string>> options)
    {
      foreach (KeyValuePair<string, string> pair in options)
      {
        string key = pair.Key;
        string value = pair.Value;
        addArgs(key, value);
      }

      if (this.binaryPath == null || this.binaryPath.Trim().Length == 0)
      {
        this.binaryPath = Directory.GetCurrentDirectory();
      }

      if (this.accessKey == null || this.accessKey.Trim().Length == 0)
      {
        this.accessKey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");
        Regex.Replace(this.accessKey, @"\s+", "");
      }
      this.local = new BrowserStackTunnel(binaryPath, accessKey + " " + argumentString);
      if (this.logVerbose == true)
      {
        this.local.logVerbose();
      }
      this.local.Run(callOnStateChange);
    }

    public void stop()
    {
      if (this.local != null)
      {
        this.local.Kill();
      }
    }
  }
}
