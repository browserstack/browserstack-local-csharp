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
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-logFile \"" + logAbsolute + "\" " + "--source \"c-sharp:.*")), Times.Once());
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
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-logFile \"" + logAbsolute + "\" .*")), Times.Once());
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
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-logFile \"" + logAbsolute + "\" .*")), Times.Once());
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
      tunnelMock.Verify(mock => mock.addBinaryPath("dummyPath", "dummyKey", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-logFile \"" + logAbsolute + "\" .*")), Times.Once());
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
      tunnelMock.Verify(mock => mock.addBinaryPath("", "dummyKey", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-vvv.*-force.*-forcelocal.*-forceproxy.*-onlyAutomate.*")), Times.Once());
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
      tunnelMock.Verify(mock => mock.addBinaryPath("", "dummyKey", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(
        It.IsRegex("-localIdentifier.*dummyIdentifier.*dummyHost.*-proxyHost.*dummyHost.*-proxyPort.*dummyPort.*-proxyUser.*dummyUser.*-proxyPass.*dummyPass.*")
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
      tunnelMock.Verify(mock => mock.addBinaryPath("", "dummyKey", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(
        It.IsRegex("-customBoolKey1.*-customBoolKey2.*-customKey1.*customValue1.*-customKey2.*customValue2.*")
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
      tunnelMock.Verify(mock => mock.addBinaryPath("", "dummyKey", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-logFile \"" + logAbsolute + "\" .*")), Times.Once());
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
      tunnelMock.Verify(mock => mock.addBinaryPath("", "dummyKey", It.IsAny<bool>(), It.IsAny<Exception>()), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-logFile \"" + logAbsolute + "\" .*")), Times.Once());
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
