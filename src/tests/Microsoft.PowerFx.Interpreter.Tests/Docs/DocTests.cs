// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests.Docs
{
    public class DocTests
    {
        [Fact]
        public void Test()
        {
            var config = new PowerFxConfig();
            config.EnableParseJSONFunction();
#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableRegExFunctions();
#pragma warning restore CS0618 // Type or member is obsolete

            config.SymbolTable.EnableMutationFunctions();

            var engine = new RecalcEngine(config);

            // File should have "Copy to output" set. 
            var path = Path.GetFullPath(@"Docs\InterpreterBase.json");

            EngineSchemaChecker.Check(engine, path);
        }
    }
}
