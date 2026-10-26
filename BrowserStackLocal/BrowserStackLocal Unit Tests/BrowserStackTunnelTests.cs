using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

using NUnit.Framework;
using BrowserStack;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BrowserStack_Unit_Tests
{
  [TestClass]
  public class BrowserStackTunnelTests
  {
    static readonly OperatingSystem os = Environment.OSVersion;
    static readonly string homepath = os.Platform.ToString() == "Unix" ?
                                        Environment.GetFolderPath(Environment.SpecialFolder.Personal) :
                                        Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
    static readonly string binaryName = BrowserStackTunnel.GetBinaryName();
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
      tunnel.addBinaryPath("dummyPath", "");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), "dummyPath");
    }
    [TestMethod]
    public void TestBinaryPathOnNull()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath(null, "");
      string expectedPath = Path.Combine(homepath, ".browserstack");
      expectedPath = Path.Combine(expectedPath, binaryName);
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnEmpty()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("", "");
      string expectedPath = Path.Combine(homepath, ".browserstack");
      expectedPath = Path.Combine(expectedPath, binaryName);
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnFallback()
    {
      string expectedPath = "dummyPath";
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath", "");
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Path.Combine(homepath, ".browserstack");
      expectedPath = Path.Combine(expectedPath, binaryName);
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Directory.GetCurrentDirectory();
      expectedPath = Path.Combine(expectedPath, binaryName);
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);

      tunnel.fallbackPaths();
      expectedPath = Path.GetTempPath();
      expectedPath = Path.Combine(expectedPath, binaryName);
      Assert.AreEqual(tunnel.getBinaryAbsolute(), expectedPath);
    }
    [TestMethod]
    public void TestBinaryPathOnNoMoreFallback()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryPath("dummyPath", "");
      tunnel.fallbackPaths();
      tunnel.fallbackPaths();
      tunnel.fallbackPaths();
      Assert.Throws(typeof(Exception),
        new TestDelegate(testFallbackException),
        "Binary not found or failed to launch. Make sure that BrowserStackLocal is not already running."
        );
    }
    [TestMethod]
    public void TestBinaryArguments()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryArguments(new List<string> { "-dummyFlag", "dummyValue" });
      CollectionAssert.AreEqual(new List<string> { "-dummyFlag", "dummyValue" }, tunnel.getBinaryArguments());
    }
    [TestMethod]
    public void TestBinaryArgumentsAreEmptyOnNull()
    {
      tunnel = new TunnelClass();
      tunnel.addBinaryArguments(null);
      Assert.IsEmpty(tunnel.getBinaryArguments());
    }


    [TestMethod]
    public void TestGetBinaryNameReturnsKnownPlatformBinary()
    {
      string result = BrowserStackTunnel.GetBinaryName();
      string[] knownBinaries = new[] {
        "BrowserStackLocal.exe",
        "BrowserStackLocal-darwin-x64",
        "BrowserStackLocal-linux-x64",
        "BrowserStackLocal-linux-ia32",
        "BrowserStackLocal-linux-arm64",
        "BrowserStackLocal-alpine"
      };
      Assert.Contains(result, knownBinaries);
    }

    [TestMethod]
    public void TestSetProxyAcceptsHostAndPort()
    {
      tunnel = new TunnelClass();
      Assert.DoesNotThrow(() => tunnel.SetProxy("proxy.example.com", 8080));
      Assert.DoesNotThrow(() => tunnel.SetProxy(null, 0));
    }

    public void testFallbackException()
    {
      tunnel.fallbackPaths();
    }

    // LOC-7549: the endpoint can hand out a path whose objects are stored with
    // Content-Encoding: gzip. The saved binary must be the decoded bytes, not the gzip stream.
    [TestMethod]
    public void TestDownloadBinaryDecodesGzipContentEncoding()
    {
      byte[] binary = SampleBinary();
      byte[] saved = DownloadFrom(binary, gzip: true);
      CollectionAssert.AreEqual(binary, saved, "gzip-encoded download was saved without decoding");
    }

    [TestMethod]
    public void TestDownloadBinarySavesUncompressedResponseAsIs()
    {
      byte[] binary = SampleBinary();
      CollectionAssert.AreEqual(binary, DownloadFrom(binary, gzip: false));
    }

    [TestMethod]
    public void TestDownloadBinaryFailureLeavesNoPartialFile()
    {
      string work = Path.Combine(Path.GetTempPath(), "bsloc" + Guid.NewGuid().ToString("N"));
      try
      {
        using (var server = new BinaryServer(null, gzip: false))
        {
          tunnel = new TunnelClass();
          tunnel.setSourceUrl(server.BaseUrl);
          tunnel.setBinaryAbsolute(Path.Combine(work, binaryName));
          Assert.That(() => tunnel.downloadBinary(), Throws.Exception);
          Assert.IsFalse(File.Exists(Path.Combine(work, binaryName)), "failed download left a file behind");
        }
      }
      finally
      {
        try { Directory.Delete(work, true); } catch { }
      }
    }

    private static byte[] SampleBinary()
    {
      // Compressible, like a real binary, and large enough to span several reads.
      var bytes = new byte[2 * 1024 * 1024];
      for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)((i * 31) % 251);
      return bytes;
    }

    private byte[] DownloadFrom(byte[] binary, bool gzip)
    {
      string work = Path.Combine(Path.GetTempPath(), "bsloc" + Guid.NewGuid().ToString("N"));
      try
      {
        using (var server = new BinaryServer(binary, gzip))
        {
          tunnel = new TunnelClass();
          tunnel.setSourceUrl(server.BaseUrl);
          tunnel.setBinaryAbsolute(Path.Combine(work, binaryName));
          tunnel.downloadBinary();
          Assert.AreEqual("/" + binaryName, server.RequestedPath);
          return File.ReadAllBytes(Path.Combine(work, binaryName));
        }
      }
      finally
      {
        try { Directory.Delete(work, true); } catch { }
      }
    }

    // Serves `binary` at /<binaryName> for one request, gzip-encoded when asked to,
    // the way S3/CloudFront serve an object stored with Content-Encoding: gzip.
    // A null binary answers 404.
    private sealed class BinaryServer : IDisposable
    {
      private readonly System.Net.HttpListener listener = new System.Net.HttpListener();
      private readonly System.Threading.Tasks.Task serving;
      public string BaseUrl { get; }
      public string RequestedPath { get; private set; }

      public BinaryServer(byte[] binary, bool gzip)
      {
        int port = FreePort();
        BaseUrl = "http://127.0.0.1:" + port;
        listener.Prefixes.Add(BaseUrl + "/");
        listener.Start();
        serving = System.Threading.Tasks.Task.Run(() =>
        {
          var context = listener.GetContext();
          RequestedPath = context.Request.Url.AbsolutePath;
          var response = context.Response;
          if (binary == null)
          {
            response.StatusCode = 404;
            response.Close();
            return;
          }
          byte[] body = binary;
          if (gzip)
          {
            using (var buffer = new MemoryStream())
            {
              using (var gz = new System.IO.Compression.GZipStream(buffer, System.IO.Compression.CompressionLevel.Optimal))
              {
                gz.Write(binary, 0, binary.Length);
              }
              body = buffer.ToArray();
            }
            response.AddHeader("Content-Encoding", "gzip");
          }
          response.ContentLength64 = body.Length;
          response.OutputStream.Write(body, 0, body.Length);
          response.Close();
        });
      }

      private static int FreePort()
      {
        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        int port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
      }

      public void Dispose()
      {
        serving.Wait(TimeSpan.FromSeconds(10));
        listener.Close();
      }
    }

    // Regression for the chmod shell-metacharacter injection (F-001): binaryAbsolute must
    // reach chmod as a single argument, never interpolated into a shell command line. On
    // pre-fix code (`bash -c "chmod 0755 <path>"`) the payload below runs `touch <marker>`
    // and never chmods the real file, so BOTH asserts fail; the fix (`/bin/chmod` +
    // ArgumentList) creates no marker and chmods the real path. Unix-only: on Windows
    // modifyBinaryPermission takes the ACL branch, not chmod.
    [TestMethod]
    public void TestModifyBinaryPermissionDoesNotInterpretShellMetacharacters()
    {
      if (os.Platform.ToString() != "Unix")
      {
        Assert.Ignore("Unix-only: Windows takes the ACL branch in modifyBinaryPermission, not chmod");
        return;
      }

      string prevCwd = Directory.GetCurrentDirectory();
      // Space-free working dir so the injected `touch pwned` (if it runs) lands here deterministically.
      string work = Path.Combine(Path.GetTempPath(), "bsloc" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(work);
      Directory.SetCurrentDirectory(work);
      try
      {
        // Filename carries a space AND a shell-injection payload. A filename cannot contain '/',
        // so the injected command targets the (deterministic) CWD, not an absolute path.
        string binaryPath = Path.Combine(work, "bs local; touch pwned; #");
        File.WriteAllText(binaryPath, "#!/bin/sh\n"); // default perms ~0644 (not executable)

        tunnel = new TunnelClass();
        ((TunnelClass)tunnel).setBinaryAbsolute(binaryPath);
        tunnel.modifyBinaryPermission();

        Assert.IsFalse(File.Exists(Path.Combine(work, "pwned")),
          "shell metacharacters in binaryAbsolute were interpreted - OS command injection");
        Assert.IsTrue(IsExecutable(binaryPath),
          "chmod 0755 was not applied to the real binary path (the path was mangled by the shell)");
      }
      finally
      {
        Directory.SetCurrentDirectory(prevCwd);
        try { Directory.Delete(work, true); } catch { }
      }
    }

    // Returns true iff `path` has the execute bit set. Uses sh's `$0` positional so the
    // path (which contains a space + metacharacters) is passed safely, not re-parsed.
    private static bool IsExecutable(string path)
    {
      var psi = new System.Diagnostics.ProcessStartInfo("/bin/sh") { UseShellExecute = false };
      psi.ArgumentList.Add("-c");
      psi.ArgumentList.Add("test -x \"$0\"");
      psi.ArgumentList.Add(path);
      using (var p = System.Diagnostics.Process.Start(psi))
      {
        p.WaitForExit();
        return p.ExitCode == 0;
      }
    }
    public class TunnelClass : BrowserStackTunnel
    {
      public TunnelClass() : base("test-user-agent") {}
      // Stub the network boundary so these binary-path/fallback unit tests exercise
      // the real path-resolution logic without making a live HTTP call to the
      // endpoint API (which addBinaryPath triggers on first invocation).
      protected override string fetchSourceUrl(string accessKey)
      {
        return null;
      }
      public StringBuilder getOutputBuilder()
      {
        return output;
      }
      public string getBinaryAbsolute()
      {
        return binaryAbsolute;
      }
      public List<string> getBinaryArguments()
      {
        return binaryArguments;
      }
      public void setBinaryAbsolute(string path)
      {
        binaryAbsolute = path;
      }
      public void setSourceUrl(string url)
      {
        typeof(BrowserStackTunnel)
          .GetField("sourceUrl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
          .SetValue(this, url);
      }
    }
  }
}
