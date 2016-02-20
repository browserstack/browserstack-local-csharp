using System;
using BrowserStack;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace BrowserStackExample
{
  class Example
  {
    static void runOnConnected(LocalState state)
    {
      Console.WriteLine("State Changed - " + state);
    }
    static void Main(string[] args)
    {
      Local local = new Local(runOnConnected);
      Console.WriteLine("Is Running " + local.isRunning());
      local.verboseMode();
      Console.WriteLine("Is Running " + local.isRunning());

      List<KeyValuePair<string, string>> options = new List<KeyValuePair<string, string>>() {
        new KeyValuePair<string, string>("key", "sUiJatw6NhJZpsttcY35"),
        new KeyValuePair<string, string>("localIdentifier", "qwe"),
        new KeyValuePair<string, string>("qwe", "asd"),
        new KeyValuePair<string, string>("onlyAutomate", "true"),
        new KeyValuePair<string, string>("forceLocal", "true"),
        new KeyValuePair<string, string>("path", "C:\\Users\\Admin\\Desktop\\"),
      };
      Console.WriteLine("Is Running " + local.isRunning());
      local.start(options);
      Console.WriteLine("Is Running " + local.isRunning());
      //Console.ReadLine();
      IWebDriver driver;
      DesiredCapabilities capability = DesiredCapabilities.Firefox();
      capability.SetCapability("browserstack.user", "harishved2");
      capability.SetCapability("browserstack.key", "sUiJatw6NhJZpsttcY35");
      capability.SetCapability("browserstack.localIdentifier", "qwe");
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
      Console.WriteLine("Is Running " + local.isRunning());
      local.stop();
      Console.WriteLine("Is Running " + local.isRunning());
      Console.ReadLine();
      Console.WriteLine("Is Running " + local.isRunning());
    }
  }
}
