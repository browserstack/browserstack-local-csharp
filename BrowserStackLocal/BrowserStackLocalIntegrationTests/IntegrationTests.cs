using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using BrowserStack;

namespace BrowserStackLocalIntegrationTests
{
  public class IntegrationTests
  {
    private List<KeyValuePair<string, string>> options = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("key", Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY")),
      //new KeyValuePair<string, string>("localIdentifier", "identifier"),
      //new KeyValuePair<string, string>("f", "C:\\Users\\Admin\\Desktop\\"),
      //new KeyValuePair<string, string>("onlyAutomate", "true"),
      new KeyValuePair<string, string>("verbose", "true"),
      new KeyValuePair<string, string>("forcelocal", "true"),
      new KeyValuePair<string, string>("binarypath", "C:\\Users\\Admin\\Desktop\\BrowserStackLocal.exe"),
      new KeyValuePair<string, string>("logfile", "C:\\Users\\Admin\\Desktop\\local.log"),
    };

    [Test]
    public void TestStartsAndStopsLocalBinary()
    {
      Local local = new Local();

      void startWithOptions()
      {
        local.start(options);
      }

      Assert.DoesNotThrow(new TestDelegate(startWithOptions));
      Process[] binaryInstances = Process.GetProcessesByName("BrowserStackLocal");
      Assert.AreNotEqual(binaryInstances.Length, 0);

      local.stop();
      binaryInstances = Process.GetProcessesByName("BrowserStackLocal");
      Assert.AreEqual(binaryInstances.Length, 0);
    }

    [Test]
    public void TestBinaryState()
    {
      Local local = new Local();

      Assert.AreEqual(local.isRunning(), false);

      local.start(options);
      Assert.AreEqual(local.isRunning(), true);

      local.stop();
      Assert.AreEqual(local.isRunning(), false);
    }

    [TearDown]
    public void TestCleanup()
    {
      foreach(Process p in Process.GetProcessesByName("BrowserStackLocal"))
      {
        p.Kill();
      }
    }
  }
}
