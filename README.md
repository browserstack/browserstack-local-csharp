# browserstack-local-csharp

[![Build Status](https://travis-ci.org/browserstack/browserstack-local-csharp.svg?branch=master)](https://travis-ci.org/browserstack/browserstack-local-csharp)

C# bindings for BrowserStack Local.

## Setup

Open the solution file `BrowserStack/BrowserStack.sln` in `Visual Studio`. The projects are `Visual Studio 2015` compatible.
You will need to resolve the references from the `Solution Explorer`. `Visual Studio` with automatically download the references from NuGet.

## Example

```
using BrowserStack;

# creates an instance of Local
Local local = new Local();

# replace <browserstack-accesskey> with your key. You can also set an environment variable - "BROWSERSTACK_ACCESS_KEY".
List<KeyValuePair<string, string>> bsLocalArgs = new List<KeyValuePair<string, string>>() {
  new KeyValuePair<string, string>("key", "<browserstack-accesskey>"),
}

# starts the Local instance with the required arguments
local.start(bsLocalArgs);

# check if BrowserStack local instance is running
Console.WriteLine(local.isRunning());

# stop the Local instance
local.stop();
```

## Arguments

Apart from the key, all other BrowserStack Local modifiers are optional. For the full list of modifiers, refer [BrowserStack Local modifiers](https://www.browserstack.com/local-testing#modifiers). For examples, refer below -

#### Verbose Logging
To enable verbose logging -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("v", "true"));
```

#### Folder Testing
To test local folder rather internal server, provide path to folder as value of this option -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("f", "/my/awesome/folder"));
```

#### Force Start
To kill other running Browserstack Local instances -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("force", "true"));
```

#### Only Automate
To disable local testing for Live and Screenshots, and enable only Automate -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("onlyAutomate", "true"));
```

#### Force Local
To route all traffic via local(your) machine -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("forcelocal", "true"));
```

#### Proxy
To use a proxy for local testing -

* proxyHost: Hostname/IP of proxy, remaining proxy options are ignored if this option is absent
* proxyPort: Port for the proxy, defaults to 3128 when -proxyHost is used
* proxyUser: Username for connecting to proxy (Basic Auth Only)
* proxyPass: Password for USERNAME, will be ignored if USERNAME is empty or not specified

```
bsLocalArgs.Add(new KeyValuePair<string, string>("proxyHost", "127.0.0.1"));
bsLocalArgs.Add(new KeyValuePair<string, string>("proxyPort", "8000"));
bsLocalArgs.Add(new KeyValuePair<string, string>("proxyUser", "user"));
bsLocalArgs.Add(new KeyValuePair<string, string>("proxyPass", "password"));
```

#### Local Identifier
If doing simultaneous multiple local testing connections, set this uniquely for different processes -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("localIdentifier", "randomstring"));
```

## Additional Arguments

#### Binary Path

By default, BrowserStack local wrappers try downloading and executing the latest version of BrowserStack binary in ~/.browserstack or the present working directory or the tmp folder by order. But you can override these by passing the -binarypath argument.
Path to specify local Binary path -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("binarypath", "/browserstack/BrowserStackLocal"));
```

#### Logfile
To save the logs to the file while running with the '-v' argument, you can specify the path of the file. By default the logs are saved in the local.log file in the present woring directory.
To specify the path to file where the logs will be saved -
```
bsLocalArgs.Add(new KeyValuePair<string, string>("v", "true"));
bsLocalArgs.Add(new KeyValuePair<string, string>("logfile", "/browserstack/logs.txt"));
```

## Contribute

### Build Instructions

To run the test suite run the nunit tests from Visual Studio.

### Reporting bugs

You can submit bug reports either in the Github issue tracker.

Before submitting an issue please check if there is already an existing issue. If there is, please add any additional information give it a "+1" in the comments.

When submitting an issue please describe the issue clearly, including how to reproduce the bug, which situations it appears in, what you expect to happen, what actually happens, and what platform (operating system and version) you are using.

### Pull Requests

We love pull requests! We are very happy to work with you to get your changes merged in, however, please keep the following in mind.

* Adhere to the coding conventions you see in the surrounding code.
* Include tests, and make sure all tests pass.
* Before submitting a pull-request, clean up the git history by going over your commits and squashing together minor changes and fixes into the corresponding commits. You can do this using the interactive rebase command.

## Example

To run the example,
- open the solution file `BrowserStackExample/BrowserStackExample.sln`
- resolve the references (Will need to resolve the reference to BrowserStack.dll [built from the main project])
- change the `BROWSERSTACK_USERNAME` and `BROWSERSTACK_ACCESS_KEY` string variables to your BrowserStack username and key mentioned [here](https://www.browserstack.com/accounts/settings)
