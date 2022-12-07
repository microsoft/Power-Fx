// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using BenchmarkDotNet.Running;

namespace Microsoft.PowerFx.Performance.Tests
{

    class Program
    {
        static void Main(string[] args)
        {
            /*
             * 
             *       This program needs to be run elevated
             *       Also set the following env. variable before starting VS
             *       SET MSBuildSDKsPath=C:\Program Files\dotnet\sdk\7.0.100\Sdks
             * 
             * 
             */
            
            var summary = BenchmarkRunner.Run<PerformanceTest1>();
        }
    }
}
