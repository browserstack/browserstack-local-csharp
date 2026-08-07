using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;

using NUnit.Framework;
using BrowserStack;
using System.Collections.Generic;
using Moq;
using System.IO;

namespace BrowserStack_Unit_Tests
{
  [TestClass]
  public class LocalTests
  {
    static readonly string logAbsolute = Path.Combine(Directory.GetCurrentDirectory(), "local.log");
    private string user = "";
    private string accessKey = "";
    private LocalClass local;
    private List<KeyValuePair<String, String>> options;

    public class LocalClass : Local
    {
      public void setTunnel(BrowserStackTunnel tunnel)
      {
        this.tunnel = tunnel;
      }
    }

    [TestMethod]
    public void TestThrowsWithNoAccessKey()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", ""));
      local = new LocalClass();

      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);

      Assert.Throws(typeof(Exception),
        new TestDelegate(startWithOptions),
        "BROWSERSTACK_ACCESS_KEY cannot be empty. Specify one by adding key to options or adding to the environment variable BROWSERSTACK_ACCESS_KEY.");
      local.stop();
    }

    [TestMethod]
    public void TestWorksWithAccessKeyInOptions()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      Assert.DoesNotThrow(new TestDelegate(startWithOptions),
        "BROWSERSTACK_ACCESS_KEY cannot be empty. Specify one by adding key to options or adding to the environment variable BROWSERSTACK_ACCESS_KEY.");
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-logFile", logAbsolute, "--source") && StartsWithAny(a, "c-sharp:"))), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksWithAccessKeyNotInOptions()
    {
      Environment.SetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY", "envDummyKey");
      options = new List<KeyValuePair<string, string>>();
      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("envDummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      Assert.DoesNotThrow(new TestDelegate(startWithOptions),
        "BROWSERSTACK_ACCESS_KEY cannot be empty. Specify one by adding key to options or adding to the environment variable BROWSERSTACK_ACCESS_KEY.");
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-logFile", logAbsolute))), Times.Once());
      tunnelMock.Verify(mock => mock.Run("envDummyKey", "", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksForFolderTesting()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("f", "dummyFolderPath"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "dummyFolderPath", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-logFile", logAbsolute))), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "dummyFolderPath", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksForBinaryPath()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("binarypath", "dummyPath"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath("dummyPath", "", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-logFile", logAbsolute))), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksWithBooleanOptions()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("v", "true"));
      options.Add(new KeyValuePair<string, string>("force", "true"));
      options.Add(new KeyValuePair<string, string>("forcelocal", "true"));
      options.Add(new KeyValuePair<string, string>("forceproxy", "true"));
      options.Add(new KeyValuePair<string, string>("onlyAutomate", "true"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath("", "", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-vvv", "-force", "-forcelocal", "-forceproxy", "-onlyAutomate"))), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksWithValueOptions()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("localIdentifier", "dummyIdentifier"));
      options.Add(new KeyValuePair<string, string>("hosts", "dummyHost"));
      options.Add(new KeyValuePair<string, string>("proxyHost", "dummyHost"));
      options.Add(new KeyValuePair<string, string>("proxyPort", "dummyPort"));
      options.Add(new KeyValuePair<string, string>("proxyUser", "dummyUser"));
      options.Add(new KeyValuePair<string, string>("proxyPass", "dummyPass"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock =>mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath("", "", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-localIdentifier", "dummyIdentifier", "dummyHost", "-proxyHost", "dummyHost",
                   "-proxyPort", "dummyPort", "-proxyUser", "dummyUser", "-proxyPass", "dummyPass"))
        ), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksWithCustomOptions()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("customBoolKey1", "true"));
      options.Add(new KeyValuePair<string, string>("customBoolKey2", "false"));
      options.Add(new KeyValuePair<string, string>("customKey1", "customValue1"));
      options.Add(new KeyValuePair<string, string>("customKey2", "customValue2"));
      
      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath("", "", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-customBoolKey1", "-customBoolKey2", "-customKey1", "customValue1",
                   "-customKey2", "customValue2"))
        ), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestCallsFallbackOnFailure()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));

      local = new LocalClass();
      int count = 0;
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start")).Callback(() =>
      {
        count++;
        if (count == 1)
          throw new ApplicationException();
      });
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath("", "", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-logFile", logAbsolute))), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute, "start"), Times.Exactly(2));
      tunnelMock.Verify(mock => mock.fallbackPaths(), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestKillsTunnel()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      local.stop();
      tunnelMock.Verify(mock => mock.addBinaryPath("", "", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-logFile", logAbsolute))), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute, "start"), Times.Once());
    }

    [TestMethod]
    public void TestSetProxyCalledWithProxyOptions()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("proxyHost", "proxy.example.com"));
      options.Add(new KeyValuePair<string, string>("proxyPort", "8080"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.SetProxy("proxy.example.com", 8080), Times.Once);
      local.stop();
    }

    [TestMethod]
    public void TestSetProxyCalledWithDefaultsWhenAbsent()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.SetProxy(null, 0), Times.Once);
      local.stop();
    }

    [TestMethod]
    public void TestSetProxyIgnoresInvalidPort()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("proxyHost", "proxy.example.com"));
      options.Add(new KeyValuePair<string, string>("proxyPort", "not-a-number"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute, "start"));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.SetProxy("proxy.example.com", 0), Times.Once);
      local.stop();
    }

    // ---- argv helpers -------------------------------------------------------
    // Arguments are now discrete argv elements rather than one concatenated string,
    // so assertions match elements in order instead of matching a regex.
    private static bool InOrder(List<string> actual, params string[] expected)
    {
      int idx = 0;
      foreach (string e in expected)
      {
        idx = actual.IndexOf(e, idx);
        if (idx < 0) return false;
        idx++;
      }
      return true;
    }

    private static bool StartsWithAny(List<string> actual, string prefix)
    {
      return actual.Exists(a => a != null && a.StartsWith(prefix));
    }

    // ---- regression tests: CWE-88 argument injection ------------------------
    // Each of these fails on the pre-fix code, where every value was concatenated
    // into one string that Process.Start then re-tokenised on whitespace.

    [TestMethod]
    public void TestOptionValueWithSpacesStaysOneArgument()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("proxyPass", "p@ss --proxy evil.example.com"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      local.start(options);

      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-proxyPass", "p@ss --proxy evil.example.com")
        && !a.Contains("--proxy"))), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestUnknownOptionValueWithSpacesStaysOneArgument()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("customKey", "legit --config /tmp/attacker.cfg"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      local.start(options);

      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-customKey", "legit --config /tmp/attacker.cfg")
        && !a.Contains("--config"))), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestLogFilePathWithQuoteStaysOneArgument()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("logfile", "/tmp/x\" --proxy evil.example.com \""));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      local.start(options);

      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-logFile", "/tmp/x\" --proxy evil.example.com \"")
        && !a.Contains("--proxy"))), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestOptionKeyWithWhitespaceIsRejected()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("foo --proxy evil.example.com", "bar"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);

      Assert.Throws(typeof(ArgumentException), new TestDelegate(startWithOptions));
    }

    [TestMethod]
    public void TestAccessKeyWhitespaceIsStrippedFromOptions()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", " dummy Key --proxy evil.example.com "));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      local.start(options);

      // Whitespace removed, so no "--proxy" token can split out of the key.
      tunnelMock.Verify(mock => mock.Run("dummyKey--proxyevil.example.com", "", logAbsolute, "start"),
        Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestAccessKeyWhitespaceIsStrippedFromEnvironmentVariable()
    {
      Environment.SetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY", "env Dummy\tKey");
      options = new List<KeyValuePair<string, string>>();

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      local.start(options);

      tunnelMock.Verify(mock => mock.Run("envDummyKey", "", logAbsolute, "start"), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestFolderPathWithSpacesIsPreserved()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("f", "/my/awesome folder"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      local.start(options);

      tunnelMock.Verify(mock => mock.Run("dummyKey", "/my/awesome folder", logAbsolute, "start"),
        Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestDocumentedPassThroughOptionsStillWork()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("localProxyHost", "127.0.0.1"));
      options.Add(new KeyValuePair<string, string>("localProxyPort", "8000"));
      options.Add(new KeyValuePair<string, string>("-pac-file", "/tmp/my proxy.pac"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>("test-user-agent");
      local.setTunnel(tunnelMock.Object);
      local.start(options);

      tunnelMock.Verify(mock => mock.addBinaryArguments(It.Is<List<string>>(a =>
        InOrder(a, "-localProxyHost", "127.0.0.1", "-localProxyPort", "8000",
                   "--pac-file", "/tmp/my proxy.pac"))), Times.Once());
      local.stop();
    }

    public void startWithOptions()
    {
      local.start(options);
    }

    [TestInitialize]
    public void beforeEveryTest()
    {
      user = Environment.GetEnvironmentVariable("BROWSERSTACK_USERNAME");
      accessKey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");
      Environment.SetEnvironmentVariable("BROWSERSTACK_USERNAME", "");
      Environment.SetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY", "");
    }

    [TestCleanup]
    public void afterEveryTest()
    {
      Environment.SetEnvironmentVariable("BROWSERSTACK_USERNAME", user);
      Environment.SetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY", accessKey);
    }
  }
}
