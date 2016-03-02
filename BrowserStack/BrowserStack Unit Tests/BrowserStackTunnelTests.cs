using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NUnit.Framework;
using BrowserStack;
using System.Collections.Generic;
using Moq;
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
      NUnit.Framework.Assert.AreEqual(tunnel.localState, LocalState.Idle);
      NUnit.Framework.Assert.NotNull(tunnel.getOutputBuilder());
    }
    [TestMethod]
    public void TestBinaryPathIsSet()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryAbsolute(), "dummyPath");
    }
    [TestMethod]
    public void TestBinaryPathOnNull()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath(null);
      string expectedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack");
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnEmpty()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("");
      string expectedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack");
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnFallback()
    {
      string expectedPath = "dummyPath";
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), ".browserstack");
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Directory.GetCurrentDirectory();
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Path.GetTempPath();
      expectedPath = Path.Combine(expectedPath, "BrowserStackLocal.exe");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnNoMoreFallback()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath");
      tunnel.fallbackPaths();
      tunnel.fallbackPaths();
      tunnel.fallbackPaths();
      NUnit.Framework.Assert.Throws(typeof(Exception),
        new TestDelegate(testFallbackException),
        "No More Paths to try. Please specify a binary path in options."
        );
    }
    [TestMethod]
    public void TestBinaryArguments()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryArguments("dummyArguments");
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryArguments(), "dummyArguments");
    }
    [TestMethod]
    public void TestBinaryArgumentsAreEmptyOnNull()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryArguments(null);
      NUnit.Framework.Assert.AreEqual(tunnel.getBinaryArguments(), "");
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
    [SetUpFixture]
    public class SetupClass
    {
     [SetUp]
      public void beforeEveryTest()
      {
       
      }

      [TearDown]
      public void afterEveryTest()
      {
        
      }
    }
  }
}
