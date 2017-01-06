using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

using NUnit.Framework;
using BrowserStack;
using System.Text;
using System.IO;

namespace BrowserStack_Unit_Tests
{
  [TestClass]
  public class BrowserStackTunnelTests
  {
    private TunnelClass tunnel;
    [TestMethod]
    public void TestInitialState()
    {
      tunnel = new TunnelClass();
      Assert.AreEqual(tunnel.localState, LocalState.Idle);
      Assert.NotNull(tunnel.getOutputBuilder());
    }
    [TestMethod]
    public void TestBinaryPathIsSet()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), "dummyPath");
    }
    [TestMethod]
    public void TestBinaryPathOnNull()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath(null);
      string expectedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack");
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnEmpty()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("");
      string expectedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack");
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnFallback()
    {
      string expectedPath = "dummyPath";
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack");
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Directory.GetCurrentDirectory();
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Path.GetTempPath();
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnNoMoreFallback()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath");
      tunnel.fallbackPaths();
      tunnel.fallbackPaths();
      tunnel.fallbackPaths();
      Assert.Throws(typeof(Exception),
        new TestDelegate(testFallbackException),
        "Binary not found or failed to launch. Make sure that BrowserStackLocal.exe is not already running."
        );
    }
    [TestMethod]
    public void TestBinaryArguments()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryArguments("dummyArguments");
      Assert.AreEqual(tunnel.getBinaryArguments(), "dummyArguments");
    }
    [TestMethod]
    public void TestBinaryArgumentsAreEmptyOnNull()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryArguments(null);
      Assert.AreEqual(tunnel.getBinaryArguments(), "");
    }


    public void testFallbackException()
    {
      tunnel.fallbackPaths();
    }
    public class TunnelClass : BrowserStackTunnel
    {
      public StringBuilder getOutputBuilder()
      {
        return output;
      }
      public string getBinaryAbsolute()
      {
        return binaryAbsolute;
      }
      public string getBinaryArguments()
      {
        return binaryArguments;
      }
    }
  }
}
