## What is it
A set of plugins that can be used inside the Bhp core library. 

## Using plugins
Plugins can be used to increase functionality in bhp. One common example is to add the ApplicationLogs plugin in order to enable your node to create log files.

To configure a plugin, do the following:
 - Download the desired plugin from the [Releases page](https://github.com/BhpAlpha/bhp-plugins/releases)
  - Alternative: Compile from source
    - Clone this repository;
    - Open it in Visual Studio, select the plugin you want to enable and select `publish` \(compile it using Release configuration\)
    - Create the Plugins folder in bhp-cli / bhp-gui (where the binary is run from, like `/bhp-cli/bin/debug/netcoreapp2.1/Plugins`)
 - Copy the .dll and the folder with the configuration files into this Plugin folder.
 - Start bhp using additional parameters, if required;
 	- In order to start logging, start bhp with the `--log` option.

The resulting folder structure is going to be like this:

```BASH
./bhp-cli.dll
./Plugins/ApplicationLogs.dll
./Plugins/ApplicationsLogs/config.json
```

## Existing plugins
### Application Logs
Add this plugin to your application if need to access the log files. This can be useful to handle notifications, but remember that this also largely increases the space used by the application.

### Import Blocks
Synchronizes the client using offline packages.  

### RPC Security
Improves security in RPC nodes.

### Simple Policy
Enables policies for consensus.

### StatesDumper
Exports BHP-CLI status data \(useful for debugging\).
