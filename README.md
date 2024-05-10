# <img src="https://user-images.githubusercontent.com/70824102/194465962-481fa7ec-0d42-4037-bdd2-f84710272329.png" width="25"> Microsoft Power Fx

<img alt="PowerFX usage example, displaying a simple slider" src="https://learn.microsoft.com/pt-br/power-platform/power-fx/media/overview/always-live.gif" width="750">

> Microsoft Power Fx is a low-code general purpose programming language based on spreadsheet-like formulas.  It is a strongly typed, declarative, and functional language, with imperative logic and state management available as needed.  

> Power Fx started with Power Apps canvas apps and that is where [you can experience it now](https://powerapps.microsoft.com/en-us/).  We are in the process of extracting the language from that product so that we can use it in more Microsoft Power Platform products and make it available here for you to use.  That's going to take some time and we will report on our progress here and on the [Power Apps blog](https://powerapps.microsoft.com/en-us/blog/).  

> A start on the language documentation is [available in the docs folder](docs/overview.md).  As with the implementation, it is being extracted from the Power Apps documentation and generalized and that too is going to take some time.

## Summary

- [Vision](#vision)
- [Overview](#overview)
- [Packages](#packages)
- [Daily Builds](#daily-builds)
- [Samples](#samples)
- [Contributing](#contributing)
- [Trademarks](#trademarks)

## Vision

The easiest and most powerful ​system to express business logic for ​everyone, everywhere.

## Overview

| [<img width="350" alt="PowerFX overview video thumbnail" src="https://user-images.githubusercontent.com/70824102/194465349-0e78a62c-cebd-4d57-9f3b-df6a371127ee.png">](https://www.youtube-nocookie.com/embed/ik6k89WNjuk) | For those new to Power-Fx, this video should answer many of your questions: <br> [Power Fx: the Programming Language for Low Code and what it means for Developers](https://www.youtube-nocookie.com/embed/ik6k89WNjuk) |
| ---- | ---- |

## Packages

| Name                                     | Released Package |
|------------------------------------------|------------------|
| Microsoft.PowerFx.Core                   | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.PowerFx.Core?includePreReleases=true&dWidth=70)](https://www.nuget.org/packages/Microsoft.PowerFx.Core/) |
| Microsoft.PowerFx.Connectors             | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.PowerFx.Connectors?includePreReleases=true&dWidth=70)](https://www.nuget.org/packages/Microsoft.PowerFx.Core.Connectors/) |
| Microsoft.PowerFx.Json                   | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.PowerFx.Json?includePreReleases=true&dWidth=70)](https://www.nuget.org/packages/Microsoft.PowerFx.Core.Json/) |
| Microsoft.PowerFx.Core.Tests             | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.PowerFx.Core.Tests?includePreReleases=true&dWidth=70)](https://www.nuget.org/packages/Microsoft.PowerFx.Core.Tests/) |
| Microsoft.PowerFx.Interpreter            | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.PowerFx.Interpreter?includePreReleases=true&dWidth=70)](https://www.nuget.org/packages/Microsoft.PowerFx.Interpreter/) |
| Microsoft.PowerFx.LanguageServerProtocol | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.PowerFx.LanguageServerProtocol?includePreReleases=true&dWidth=70)](https://www.nuget.org/packages/Microsoft.PowerFx.LanguageServerProtocol/) |
| Microsoft.PowerFx.Transport.Attributes   | [![BotBuilder Badge](https://buildstats.info/nuget/Microsoft.PowerFx.Transport.Attributes?includePreReleases=true&dWidth=70)](https://www.nuget.org/packages/Microsoft.PowerFx.Transport.Attributes/) |

## Daily Builds
Daily builds of the Power Fx packages are published to Azure Artifacts. 
- The [Azure Artifacts daily feed](https://dev.azure.com/Power-Fx/Power%20Fx/_artifacts/feed/PowerFx) carries the most recent packages. To consume them, specify this package source: 
```
https://pkgs.dev.azure.com/Power-Fx/7dd30b4a-31be-4ac9-a649-e6addd4d5b0a/_packaging/PowerFx/nuget/v3/index.json
```

- For detailed instructions [visit this page](dailyBuilds.md).

## Samples
There are samples demonstrating how to consume Power Fx at: https://github.com/microsoft/power-fx-host-samples

You can also see usage examples in the [unit tests](https://github.com/microsoft/Power-Fx/tree/main/src/tests).

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
  
