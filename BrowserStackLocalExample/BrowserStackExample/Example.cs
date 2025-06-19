using System;
using BrowserStack;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace BrowserStackExample
{
  class Example
  {
    static void Main(string[] args)
    {
      // Start BrowserStack Local
      Local local = new Local();
      var bsLocalArgs = new List<KeyValuePair<string, string>>()
      {
        new KeyValuePair<string, string>("key", BROWSERSTACK_ACCESS_KEY),
        new KeyValuePair<string, string>("forcelocal", "true"),
        new KeyValuePair<string, string>("verbose", "true"),
        // new KeyValuePair<string, string>("binarypath", "C:\\Users\\Admin\\Desktop\\BrowserStackLocal.exe"),
        // new KeyValuePair<string, string>("logfile", "C:\\Users\\Admin\\Desktop\\local.log"),
      };
      local.start(bsLocalArgs);

      // Define BrowserStack capabilities
      var browserstackOptions = new Dictionary<string, object>
      {
          { "userName", BROWSERSTACK_USERNAME },
          { "accessKey", BROWSERSTACK_ACCESS_KEY },
          { "local", "true" },
          { "build", "build" }
      };

      // Set up ChromeOptions
      ChromeOptions chromeOptions = new ChromeOptions();
      chromeOptions.BrowserVersion = "latest";
      chromeOptions.PlatformName = "Windows 10";
      chromeOptions.AddAdditionalOption("bstack:options", browserstackOptions);

      // Launch Remote WebDriver
      IWebDriver driver = new RemoteWebDriver(
          new Uri("https://hub.browserstack.com/wd/hub"),
          chromeOptions
      );

      // Run your test
      driver.Navigate().GoToUrl("http://www.google.com");
      Console.WriteLine(driver.Title);

      var query = driver.FindElement(By.Name("q"));
      query.SendKeys("Browserstack");
      query.Submit();
      Console.WriteLine(driver.Title);

      driver.Quit();
      local.stop();
      Console.WriteLine("Test Completed.");
      Console.ReadLine();
    }
  }
}
