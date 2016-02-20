using System;
using BrowserStack;
using System.Collections.Generic;

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
      Console.ReadLine();
      Console.WriteLine("Is Running " + local.isRunning());
      local.stop();
      Console.WriteLine("Is Running " + local.isRunning());
      Console.ReadLine();
      Console.WriteLine("Is Running " + local.isRunning());
    }
  }
}
