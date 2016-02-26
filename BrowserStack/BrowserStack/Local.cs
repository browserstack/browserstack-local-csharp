using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BrowserStack
{
  public class Local
  {
    private Hierarchy hierarchy;
    private string folder = "";
    private string accessKey = "";
    private string customLogPath = "";
    private string argumentString = "";
    private string customBinaryPath = "";
    private PatternLayout patternLayout;
    private BrowserStackTunnel tunnel = null;
    public static ILog logger = LogManager.GetLogger("Local");
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
      new KeyValuePair<string, string>("force", "-force"),
      new KeyValuePair<string, string>("forcelocal", "-forcelocal"),
      new KeyValuePair<string, string>("onlyautomate", "-onlyAutomate"),
    };
    private readonly string LOG4NET_CONFIG_FILE_PATH = Path.Combine(Directory.GetCurrentDirectory(), "log_config.xml");

    public bool isRunning()
    {
      if (tunnel == null) return false;
      return tunnel.IsConnected();
    }

    private void addArgs(string key, string value)
    {
      KeyValuePair<string, string> result;
      key = key.Trim().ToLower();

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
      else
      {
        result = valueCommands.Find(pair => pair.Key == key);
        if (!result.Equals(emptyStringPair))
        {
          argumentString += result.Value + " " + value + " ";
        }

        result = booleanCommands.Find(pair => pair.Key == key);
        if (!result.Equals(emptyStringPair))
        {
          if (value.Trim().ToLower() == "true")
          {
            argumentString += result.Value + " ";
          }
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

      LoggerMatchFilter loggerMatchFilter = new LoggerMatchFilter();
      loggerMatchFilter.LoggerToMatch = "Local";
      loggerMatchFilter.AcceptOnMatch = true;
      consoleAppender.AddFilter(loggerMatchFilter);
      consoleAppender.AddFilter(new DenyAllFilter());

      hierarchy.Root.AddAppender(consoleAppender);

      hierarchy.Root.Level = Level.All;
      hierarchy.Configured = true;
    }
    private void setupFileLogger(string filePath)
    {
      logger.Info("Logging Binary Output to - " + filePath);
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
            "Specify one by adding key to options or adding to the environment variable BROWSERSTACK_KEY.");
        }
        Regex.Replace(this.accessKey, @"\s+", "");
      }

      if (customLogPath == null || customLogPath.Trim().Length == 0)
      {
        customLogPath = Path.Combine(BrowserStackTunnel.basePaths[1], "local.log");
      }

      setupFileLogger(customLogPath);
      argumentString += "-logFile " + customLogPath;
      tunnel = new BrowserStackTunnel(customBinaryPath, argumentString);
      while (true) {
        bool except = false;
        try {
          tunnel.Run(accessKey, folder);
        } catch (Exception)
        {
          logger.Warn("Running Local failed. Falling back to backup path.");
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
      if (tunnel != null)
      {
        tunnel.Kill();
      }
    }
  }
}
