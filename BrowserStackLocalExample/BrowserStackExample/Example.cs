using System;
using BrowserStack;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace BrowserStackExample
{
  class Example
  {
    static void Main(string[] args)
    {
      Local local = new Local();

      List<KeyValuePair<string, string>> options = new List<KeyValuePair<string, string>>() {
        new KeyValuePair<string, string>("key", BROWSERSTACK_ACCESS_KEY),
        //new KeyValuePair<string, string>("localIdentifier", "identifier"),
        //new KeyValuePair<string, string>("f", "C:\\Users\\Admin\\Desktop\\"),
        new KeyValuePair<string, string>("onlyAutomate", "true"),
        new KeyValuePair<string, string>("verbose", "true"),
        new KeyValuePair<string, string>("forcelocal", "true"),
        new KeyValuePair<string, string>("binarypath", "C:\\Users\\Admin\\Desktop\\BrowserStackLocal.exe"),
        new KeyValuePair<string, string>("logfile", "C:\\Users\\Admin\\Desktop\\local.log"),
      };
      local.start(options);
      
      // Run WebDriver Tests
      IWebDriver driver;
      DesiredCapabilities capability = DesiredCapabilities.Chrome();
      capability.SetCapability("browserstack.user", BROWSERSTACK_USERNAME);
      capability.SetCapability("browserstack.key", BROWSERSTACK_ACCESS_KEY);
      //capability.SetCapability("browserstack.localIdentifier", "identifier");
      capability.SetCapability("browserstack.local", true);
      capability.SetCapability("build", "build");

      driver = new RemoteWebDriver(
        new Uri("http://hub.browserstack.com/wd/hub/"), capability
      );
      driver.Navigate().GoToUrl("http://www.google.com");
      Console.WriteLine(driver.Title);

      IWebElement query = driver.FindElement(By.Name("q"));
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
