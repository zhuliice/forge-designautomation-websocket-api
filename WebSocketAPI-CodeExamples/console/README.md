# Console Sample

This is a simple C# console application that uses Forge Design Automation Websocket interface. Note that unlike in the [browser](..\browser) sample, we will use a 2-legged token to access the service. 
## Prerequisites

1. Register a Forge App following the steps [here](https://forge.autodesk.com/developer/start-now/getaccess). 
2. Visual Studio 2019.

## Running the sample

1. Open websocket.sln in Visual Studio 2019.
2. Create an `appsettings.user.json` file with the following content:
```JSON
{
  "Forge": {
    "ClientId": "<copy your forge client id>",
    "ClientSecret": "<copy your forge client secret>"
  }
}
```
3. Build and Run from the IDE.

The sample will create an appbundle for a simple AutoCAD application that exposes a TEST command. An activity is also created that calls this TEST command.
Finally, the sample submits a workitem that uses this activity via Websocket.
