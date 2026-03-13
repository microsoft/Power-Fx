// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Docs
{
    public class DocTests
    {
        [Fact]
        public void Test()
        {
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();
#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableRegExFunctions();
            config.EnableOptionSetInfo();
            config.EnableJoinFunction();
#pragma warning restore CS0618 // Type or member is obsolete

            config.SymbolTable.EnableMutationFunctionsIterationSafe();

            var engine = new RecalcEngine(config);

            // File should have "Copy to output" set. 
            var path = Path.GetFullPath(@"Docs\InterpreterBase.json");

            EngineSchemaChecker.Check(engine, path);
        }
    }
}
