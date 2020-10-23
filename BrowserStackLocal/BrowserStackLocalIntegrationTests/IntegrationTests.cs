using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Edge;
using BrowserStack;

namespace BrowserStackLocalIntegrationTests
{
  public class IntegrationTests
  {
    private static string username = Environment.GetEnvironmentVariable("BROWSERSTACK_USERNAME");
    private static string accesskey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");

    private List<KeyValuePair<string, string>> options = new List<KeyValuePair<string, string>>() {
      new KeyValuePair<string, string>("key", accesskey),
      new KeyValuePair<string, string>("onlyAutomate", "true"),
      new KeyValuePair<string, string>("verbose", "true"),
      new KeyValuePair<string, string>("forcelocal", "true"),
      new KeyValuePair<string, string>("binarypath", "C:\\Users\\Admin\\Desktop\\BrowserStackLocal.exe"),
      new KeyValuePair<string, string>("logfile", "C:\\Users\\Admin\\Desktop\\local.log"),
    };

    [Test]
    public void TestStartsAndStopsLocalSession()
    {
      Local local = new Local();

      void startWithOptions()
      {
        local.start(options);
      }

      Assert.DoesNotThrow(new TestDelegate(startWithOptions));
      Process[] binaryInstances = Process.GetProcessesByName("BrowserStackLocal");
      Assert.AreNotEqual(binaryInstances.Length, 0);

      IWebDriver driver;
      EdgeOptions capability = new EdgeOptions();
      capability.AddAdditionalCapability("browserstack.user", username);
      capability.AddAdditionalCapability("browserstack.key", accesskey);
      capability.AddAdditionalCapability("browserstack.local", true);
      capability.AddAdditionalCapability("build", "C Sharp binding Integration Test");

      driver = new RemoteWebDriver(
        new Uri("http://hub.browserstack.com/wd/hub/"), capability
      );
      driver.Navigate().GoToUrl("http://bs-local.com:45691/check");
      String status = driver.FindElement(By.TagName("body")).Text;

      Assert.AreEqual(status, "Up and running");

      driver.Quit();
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
