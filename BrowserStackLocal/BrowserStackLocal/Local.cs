using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace BrowserStack
{
  public class Local
  {
    private string folder = "";
    private string accessKey = "";
    private string customLogPath = "";
    private string argumentString = "";
    private string customBinaryPath = "";
    private string bindingVersion = "";
        
    protected BrowserStackTunnel tunnel = null;
    private static KeyValuePair<string, string> emptyStringPair = new KeyValuePair<string, string>();

    private static List<KeyValuePair<string, string>> valueCommands = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("localIdentifier", "-localIdentifier"),
      new KeyValuePair<string, string>("hosts", ""),
      new KeyValuePair<string, string>("proxyHost", "-proxyHost"),
      new KeyValuePair<string, string>("proxyPort", "-proxyPort"),
      new KeyValuePair<string, string>("proxyUser", "-proxyUser"),
      new KeyValuePair<string, string>("proxyPass", "-proxyPass"),
    };

    private static List<KeyValuePair<string, string>> booleanCommands = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("v", "-vvv"),
      new KeyValuePair<string, string>("force", "-force"),
      new KeyValuePair<string, string>("forcelocal", "-forcelocal"),
      new KeyValuePair<string, string>("forceproxy", "-forceproxy"),
      new KeyValuePair<string, string>("onlyAutomate", "-onlyAutomate"),
    };

    public bool isRunning()
    {
      if (tunnel == null) return false;
      return tunnel.IsConnected();
    }

    private void addArgs(string key, string value)
    {
      KeyValuePair<string, string> result;
      key = key.Trim();

      if (key.Equals("key"))
      {
        accessKey = value;
      }
      else if (key.Equals("f"))
      {
        folder = value;
      }
      else if (key.Equals("binarypath"))
      {
        customBinaryPath = value;
      }
      else if (key.Equals("logfile"))
      {
        customLogPath = value;
      }
      else if (key.Equals("verbose"))
      {

      }
      else if (key.Equals("source"))
      {

      }
    else
      {
        result = valueCommands.Find(pair => pair.Key == key);
        if (!result.Equals(emptyStringPair))
        {
          argumentString += result.Value + " " + value + " ";
          return;
        }

        result = booleanCommands.Find(pair => pair.Key == key);
        if (!result.Equals(emptyStringPair))
        {
          if (value.Trim().ToLower() == "true")
          {
            argumentString += result.Value + " ";
            return;
          }
        }

        if (value.Trim().ToLower() == "true")
        {
          argumentString += "-" + key + " ";
        }
        else
        {
          argumentString += "-" + key + " " + value + " ";
        }
      }
    }
    public static string GetVersionString(string pVersionString)
    {
      string tVersion = "Unknown";
      string[] aVersion;

      if (string.IsNullOrEmpty(pVersionString)) { return tVersion; }
      aVersion = pVersionString.Split('.');
      if (aVersion.Length > 0) { tVersion = aVersion[0]; }
      if (aVersion.Length > 1) { tVersion += "." + aVersion[1]; }
      if (aVersion.Length > 2) { tVersion += "." + aVersion[2].PadLeft(4, '0'); }
      if (aVersion.Length > 3) { tVersion += "." + aVersion[3].PadLeft(4, '0'); }

      return tVersion;
    }
    public static Assembly GetAssemblyEmbedded(string pAssemblyDisplayName)
    {
       Assembly tMyAssembly = null;

       if (string.IsNullOrEmpty(pAssemblyDisplayName)) { return tMyAssembly; }
       try 
       {
          tMyAssembly = Assembly.Load(pAssemblyDisplayName);
       }
       catch (Exception ex)
       {
          string m = ex.Message;
       }
       return tMyAssembly;
    }
    public static string GetVersionStringFromAssemblyEmbedded(string pAssemblyDisplayName)
    {
       string tVersion = "Unknown";
       Assembly tMyAssembly = null;

       tMyAssembly = GetAssemblyEmbedded(pAssemblyDisplayName);
       if (tMyAssembly == null) { return tVersion; }
       tVersion = GetVersionString(tMyAssembly.GetName().Version.ToString());
       return tVersion;
    }

    public Local()
    {
       bindingVersion = GetVersionStringFromAssemblyEmbedded("BrowserStackLocal");
       tunnel = new BrowserStackTunnel();
    }
    public void start(List<KeyValuePair<string, string>> options)
    {
      foreach (KeyValuePair<string, string> pair in options)
      {
        string key = pair.Key;
        string value = pair.Value;
        addArgs(key, value);
      }

      if (accessKey == null || accessKey.Trim().Length == 0)
      {
        accessKey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");
        if (accessKey == null || accessKey.Trim().Length == 0)
        {
          throw new Exception("BROWSERSTACK_ACCESS_KEY cannot be empty. "+
            "Specify one by adding key to options or adding to the environment variable BROWSERSTACK_ACCESS_KEY.");
        }
        Regex.Replace(this.accessKey, @"\s+", "");
      }

      if (customLogPath == null || customLogPath.Trim().Length == 0)
      {
        customLogPath = Path.Combine(BrowserStackTunnel.basePaths[1], "local.log");
      }

      argumentString += "-logFile \"" + customLogPath + "\" ";
      argumentString += "--source \"c-sharp:" + bindingVersion + "\" ";
      tunnel.addBinaryPath(customBinaryPath);
      tunnel.addBinaryArguments(argumentString);
      while (true) {
        bool except = false;
        try {
          tunnel.Run(accessKey, folder, customLogPath, "start");
        } catch (Exception)
        {
          except = true;
        }
        if (except)
        {
          tunnel.fallbackPaths();
        } else
        {
          break;
        }
      }
    }

    public void stop()
    {
      tunnel.Run(accessKey, folder, customLogPath, "stop");
      tunnel.Kill();
    }
  }
}
