// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using BenchmarkDotNet.Running;

namespace Microsoft.PowerFx.Performance.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /* Run benchmark suite from the repository root with Windows PowerShell:
             * 1. Build:
             *    dotnet build src\tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release
             * 2. List benchmarks:
             *    Set-Location src
             *    dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --list flat
             *    Set-Location ..
             * 3. Dry validation:
             *    Set-Location src
             *    dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --filter "*ParsingAndBindingPerformance*" --job Dry
             *    Set-Location ..
             * 4. Normal suite run:
             *    Set-Location src
             *    dotnet run --project tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj -c Release --no-build -- --filter "*ParsingAndBindingPerformance*"
             *    Set-Location ..
             * 5. Logs are written to:
             *    src\BenchmarkDotNet.Artifacts
             *    Summary reports are written to:
             *    src\BenchmarkDotNet.Artifacts\results
             */

            _ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
