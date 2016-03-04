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

      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("", "", logAbsolute));
      local.setTunnel(tunnelMock.Object);

      Assert.Throws(typeof(Exception),
        new TestDelegate(startWithOptions),
        "BROWSERSTACK_ACCESS_KEY cannot be empty. Specify one by adding key to options or adding to the environment variable BROWSERSTACK_KEY.");
      local.stop();
    }

    [TestMethod]
    public void TestWorksWithAccessKeyInOptions()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      local.setTunnel(tunnelMock.Object);
      Assert.DoesNotThrow(new TestDelegate(startWithOptions),
        "BROWSERSTACK_ACCESS_KEY cannot be empty. Specify one by adding key to options or adding to the environment variable BROWSERSTACK_KEY.");
      tunnelMock.Verify(mock => mock.addBinaryArguments("-logFile " + logAbsolute), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksWithAccessKeyNotInOptions()
    {
      Environment.SetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY", "envDummyKey");
      options = new List<KeyValuePair<string, string>>();
      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("envDummyKey", "", logAbsolute));
      local.setTunnel(tunnelMock.Object);
      Assert.DoesNotThrow(new TestDelegate(startWithOptions),
        "BROWSERSTACK_ACCESS_KEY cannot be empty. Specify one by adding key to options or adding to the environment variable BROWSERSTACK_KEY.");
      tunnelMock.Verify(mock => mock.addBinaryArguments("-logFile " + logAbsolute), Times.Once());
      tunnelMock.Verify(mock => mock.Run("envDummyKey", "", logAbsolute), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksForFolderTesting()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("f", "dummyFolderPath"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("dummyKey", "dummyFolderPath", logAbsolute));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryArguments("-logFile " + logAbsolute), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "dummyFolderPath", logAbsolute), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestWorksForBinaryPath()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));
      options.Add(new KeyValuePair<string, string>("binarypath", "dummyPath"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath("dummyPath"), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments("-logFile " + logAbsolute), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute), Times.Once());
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
      options.Add(new KeyValuePair<string, string>("onlyAutomate", "true"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath(""), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(It.IsRegex("-vvv.*-force.*-forcelocal.*-onlyAutomate")), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute), Times.Once());
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
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath(""), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments(
        It.IsRegex("-localIdentifier.*dummyIdentifier.*dummyHost.*-proxyHost.*dummyHost.*-proxyPort.*dummyPort.*-proxyUser.*dummyUser.*-proxyPass.*dummyPass")
        ), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestCallsFallbackOnFailure()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));

      local = new LocalClass();
      int count = 0;
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute)).Callback(() =>
      {
        count++;
        if (count == 1)
          throw new ApplicationException();
      });
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      tunnelMock.Verify(mock => mock.addBinaryPath(""), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments("-logFile " + logAbsolute), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute), Times.Exactly(2));
      tunnelMock.Verify(mock => mock.fallbackPaths(), Times.Once());
      local.stop();
    }

    [TestMethod]
    public void TestKillsTunnel()
    {
      options = new List<KeyValuePair<string, string>>();
      options.Add(new KeyValuePair<string, string>("key", "dummyKey"));

      local = new LocalClass();
      Mock<BrowserStackTunnel> tunnelMock = new Mock<BrowserStackTunnel>();
      tunnelMock.Setup(mock => mock.Run("dummyKey", "", logAbsolute));
      local.setTunnel(tunnelMock.Object);
      local.start(options);
      local.stop();
      tunnelMock.Verify(mock => mock.addBinaryPath(""), Times.Once);
      tunnelMock.Verify(mock => mock.addBinaryArguments("-logFile " + logAbsolute), Times.Once());
      tunnelMock.Verify(mock => mock.Run("dummyKey", "", logAbsolute), Times.Once());
      tunnelMock.Verify(mock => mock.Kill(), Times.Once());
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
