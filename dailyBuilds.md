# Consuming daily builds of Power Fx

## Connect to the feed

Daily packages for Power Fx for Dotnet are published to [`Azure Artifacts`](https://dev.azure.com/ConversationalAI/BotFramework/_packaging?_a=feed&feed=SDK) (filter by "PowerFx"). 

Follow the configuration steps below, depending on your case:

### Using dotnet command line

Add a nuget.config file to your project, in the same folder as your .csproj or .sln file

```<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="SDK" value="https://pkgs.dev.azure.com/ConversationalAI/BotFramework/_packaging/SDK/nuget/v3/index.json" />
  </packageSources>
</configuration>
```
Restore packages (using the interactive flag, which allows dotnet to prompt you for credentials)
```
dotnet restore --interactive
```
Note: You don't need --interactive every time. dotnet will prompt you to add --interactive if it needs updated credentials.

### Using Visual Studio
On the Tools menu, select Options > NuGet Package Manager > Package Sources. Select the green plus in the upper-right corner and enter the name and source URL below.

Name
```
SDK
```
Source
```
https://pkgs.dev.azure.com/ConversationalAI/BotFramework/_packaging/SDK/nuget/v3/index.json
```
Note: You need to do this on every machine that needs access to your packages. Use the NuGet.exe instructions if you want to complete setup once and check it into your repository.`

On the Tools menu, select Options > NuGet Package Manager > Package Manager Console. Find a package you want to use, copy the Package Manager command, and paste it in the Package Manager Console.

For example:
```
Install-Package Microsoft.Bot.Builder
```