using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BrowserStack
{
  public class Local
  {
    private Hierarchy hierarchy;
    private string accessKey = "";
    private bool logVerbose = false;
    private string defaultDirectoryPath = null;
    private string argumentString = "";
    private PatternLayout patternLayout;
    private BrowserStackTunnel local = null;
    public static ILog logger = LogManager.GetLogger("log4net");
    public static ILog binaryLogger = LogManager.GetLogger("Binary Output");
    private static KeyValuePair<string, string> emptyStringPair = new KeyValuePair<string, string>();

    private static List<KeyValuePair<string, string>> valueCommands = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("localidentifier", "-localIdentifier"),
      new KeyValuePair<string, string>("hosts", ""),
      new KeyValuePair<string, string>("proxyhost", "-proxyHost"),
      new KeyValuePair<string, string>("proxyport", "-proxyPort"),
      new KeyValuePair<string, string>("proxyuser", "-proxyUser"),
      new KeyValuePair<string, string>("proxypass", "-proxyPass"),
    };

    private static List<KeyValuePair<string, string>> booleanCommands = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("verbose", "-vvv"),
      new KeyValuePair<string, string>("forcelocal", "-forcelocal"),
      new KeyValuePair<string, string>("onlyautomate", "-onlyAutomate"),
    };
    private readonly string LOG4NET_CONFIG_FILE_PATH = Path.Combine(Directory.GetCurrentDirectory(), "log_config.xml");

    public bool isRunning()
    {
      if (this.local == null) return false;
      return this.local.IsConnectedOrConnecting();
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
        this.defaultDirectoryPath = value;
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
    private void setupLogging()
    {
      hierarchy = (Hierarchy)LogManager.GetRepository();

      patternLayout = new PatternLayout();
      patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
      patternLayout.ActivateOptions();

      ConsoleAppender consoleAppender = new ConsoleAppender();
      consoleAppender.Threshold = Level.Info;
      consoleAppender.Layout = patternLayout;
      consoleAppender.ActivateOptions();
      hierarchy.Root.AddAppender(consoleAppender);

      hierarchy.Root.Level = Level.All;
      hierarchy.Configured = true;
    }
    private void setupFileLogger(string filePath)
    {
      RollingFileAppender roller = new RollingFileAppender();
      roller.AppendToFile = true;
      roller.File = filePath;
      roller.Layout = patternLayout;
      roller.Threshold = Level.All;

      LoggerMatchFilter loggerMatchFilter = new LoggerMatchFilter();
      loggerMatchFilter.LoggerToMatch = "Binary Output";
      loggerMatchFilter.AcceptOnMatch = true;
      roller.AddFilter(loggerMatchFilter);
      roller.AddFilter(new DenyAllFilter());

      roller.MaxSizeRollBackups = 5;
      roller.MaximumFileSize = "1GB";
      roller.RollingStyle = RollingFileAppender.RollingMode.Size;
      roller.StaticLogFileName = true;
      roller.ActivateOptions();
      hierarchy.Root.AddAppender(roller);
    }

    public Local()
    {
      setupLogging();
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

      if (this.defaultDirectoryPath == null || this.defaultDirectoryPath.Trim().Length == 0)
      {
        this.defaultDirectoryPath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack");
      }
      (new FileInfo(this.defaultDirectoryPath)).Directory.Create();

      if (this.accessKey == null || this.accessKey.Trim().Length == 0)
      {
        this.accessKey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");
        Regex.Replace(this.accessKey, @"\s+", "");
      }

      setupFileLogger(Path.Combine(this.defaultDirectoryPath, "local.log"));
      this.local = new BrowserStackTunnel(defaultDirectoryPath, accessKey + " " + argumentString);
      if (this.logVerbose == true)
      {
        this.local.logVerbose();
      }
      this.local.Run();
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
