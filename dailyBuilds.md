# Consuming Power Fx daily builds

## Connect to the feed

Daily NuGet packages for Power Fx are published to [`Azure Artifacts`](https://dev.azure.com/Power-Fx/Power%20Fx/_artifacts/feed/PowerFx).

Follow the configuration steps below, depending on your case:

### Using dotnet command line

Add a nuget.config file to your project in the same folder as your .csproj or .sln file

```<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="SDK" value="https://pkgs.dev.azure.com/Power-Fx/7dd30b4a-31be-4ac9-a649-e6addd4d5b0a/_packaging/PowerFx/nuget/v3/index.json" />
  </packageSources>
</configuration>
```
Restore packages (the interactive flag allows dotnet to prompt you for credentials)
```
dotnet restore --interactive
```
`Note: You don't need --interactive every time. dotnet will prompt you to add --interactive if it needs updated credentials.`

### Using Visual Studio
On the Tools menu, select Options > NuGet Package Manager > Package Sources. Select the green plus in the upper-right corner and enter the name and source URL below.

Name
```
SDK
```
Source
```
https://pkgs.dev.azure.com/Power-Fx/7dd30b4a-31be-4ac9-a649-e6addd4d5b0a/_packaging/PowerFx/nuget/v3/index.json
```
`Note: You need to do this on every machine that needs access to your packages. Use the command line instructions above if you want to complete the setup once and check it in to your repository.`

On the Tools menu, select Options > NuGet Package Manager > Package Manager Console. Find a package you want to use, copy the Package Manager command, and paste it in the Package Manager Console.

For example:
```
Install-Package Microsoft.PowerFx.Core
```