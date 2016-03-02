# browserstack-local-csharp

[![Build Status](https://travis-ci.org/browserstack/browserstack-local-csharp.svg?branch=master)](https://travis-ci.org/browserstack/browserstack-local-csharp)

## Setup

Open the solution file `BrowserStack/BrowserStack.sln` in `Visual Studio`. The projects are `Visual Studio 2015` compatible.
You will need to resolve the references from the `Solution Explorer`. `Visual Studio` with automatically download the references from NuGet.

## API

### Constructor

* `new Local()`: creates an instance of Local

### Methods

* `start(options)`: starts Local instance with options. The options available are detailed below.
* `stop()`: stops the Local instance
* `isRunning()`: checks if Local instance is running and returns a corresponding boolean value

### Options

* `key`: BrowserStack Access Key
* `v`: Provides verbose logging
* `f`: If you want to test local folder rather internal server, provide path to folder as value of this option
* `force`: Kill other running Browserstack Local
* `only`: Restricts Local Testing access to specified local servers and/or folders
* `forcelocal`: Route all traffic via local machine
* `onlyAutomate`: Disable Live Testing and Screenshots, just test Automate
* `proxyHost`: Hostname/IP of proxy, remaining proxy options are ignored if this option is absent
* `proxyPort`: Port for the proxy, defaults to 3128 when -proxyHost is used
* `proxyUser`: Username for connecting to proxy (Basic Auth Only)
* `proxyPass`: Password for USERNAME, will be ignored if USERNAME is empty or not specified
* `localIdentifier`: If doing simultaneous multiple local testing connections, set this uniquely for different processes
* `hosts`: List of hosts and ports where Local must be enabled for eg. localhost,3000,1,localhost,3001,0
* `logfile`: Path to file where Local logs be saved to
* `binarypath`: Optional path to Local binary

## Example

To run the example,
- open the solution file `BrowserStackExample/BrowserStackExample.sln`
- resolve the references (Will need to resolve the reference to BrowserStack.dll [built from the main project])
- change the `BROWSERSTACK_USERNAME` and `BROWSERSTACK_ACCESS_KEY` string variables to your BrowserStack username and key mentioned [here](https://www.browserstack.com/accounts/settings)
