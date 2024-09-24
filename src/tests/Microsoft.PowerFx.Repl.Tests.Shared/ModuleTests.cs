// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable SA1118 // Parameter should not span multiple lines

namespace Microsoft.PowerFx.Repl.Tests
{
    // Thesde tests should target public Module Import() function 
    // (and not internal services)
    public class ModuleTests
    {
        private PowerFxREPL _repl;
        private readonly TestReplOutput _output = new TestReplOutput();

        public ModuleTests()
        {
            var config = new PowerFxConfig();
            config.SymbolTable.EnableMutationFunctions();

            // config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            _repl = new PowerFxREPL
            {
                Engine = engine,
                Output = _output,
                AllowSetDefinitions = true,
                AllowUserDefinedFunctions = true,
                AllowImport = true,
                ParserOptions = new ParserOptions() { AllowsSideEffects = true }
            };
        }
        
        // Run line, return the normal output. 
        private string HandleLine(string fx, bool expectErrors = false)
        {
            Debugger.Log(0, string.Empty, ">> " + fx);
            _repl.HandleLine(fx);

            if (!expectErrors)
            {
                AssertNoErrors();
            }

            var log = _output.Get(OutputKind.Repl);

            Debugger.Log(0, string.Empty, log);
            return log;
        }

        private void AssertNoErrors()
        {
            Assert.Empty(_output.Get(OutputKind.Error));
        }

        private void Import(string path, bool expectErrors = false)
        {
            string fullpath = Path.Combine(
                Environment.CurrentDirectory,
                "Modules",
                path);

            var expr = $"Import(\"{fullpath}\")";
            HandleLine(expr, expectErrors);

            if (expectErrors)
            {
                var modules = _repl.Modules.ToArray();
                Assert.Empty(modules); // nothing actually loaded. 
            }
        }

        // Ensure that we only can access modules if they're enabled. 
        [Fact]
        public void MustEnable()
        {
            var engine = new RecalcEngine();

            _repl = new PowerFxREPL
            {
                Engine = engine,
                Output = _output            
            };

            // Defaults to false
            Assert.False(_repl.AllowUserDefinedFunctions);

            Import("basic1.fx.yml", expectErrors: true);
            
            // Failed to import. 
            var msg = _output.Get(OutputKind.Error);
            Assert.Contains("'Import' is an unknown or unsupported function.", msg);
        }

        [Fact]
        public void Test1()
        {
            Import("basic1.fx.yml");

            // Call method defined in moduel.
            var log = HandleLine("DoubleIt(3)");

            Assert.Equal("6", log);
        }

        // Load multiple modules
        [Fact]
        public void Test2()
        {
            Import("basic1.fx.yml");
            Import("basic2.fx.yml");

            // Call method defined in moduel.
            var log = HandleLine("DoubleIt(Add1(3))");

            Assert.Equal("8", log);
        }

        // Load with imports
        [Fact]
        public void TestImport()
        {
            Import("Depend2.fx.yml");

            // Call method defined in moduel.
            var log = HandleLine("Func2(3)");

            Assert.Equal("8", log);
        }

        // Load with imports
        [Fact]
        public void TestImport2()
        {
            Import("Depend2.fx.yml");

            // Call method defined in moduel.
            var log = HandleLine("Func2(3)");
            Assert.Equal("8", log);

            // Inner dependencies are not exported 
            log = HandleLine("Add1(3)", expectErrors: true);
            var msg = _output.Get(OutputKind.Error);
            Assert.Contains("Add1' is an unknown or unsupported function.", msg);

            // But we can import the inner module and call.
            Import("basic2.fx.yml");
            log = HandleLine("Add1(3)");
            Assert.Equal("4", log);
        }

        // When we import a file twice, we get the updated contents.
        [Fact]
        public void GetsLatest()
        {
            using var temp = new TempFileHolder();
            string fullpath = temp.FullPath;

            // First version
            File.WriteAllText(fullpath, @"
Formulas: |
  Func(x: Number) : Number = x * 10;
");
            HandleLine($"Import(\"{fullpath}\")");

            // Call method defined in moduel.
            var before = HandleLine("Func(3)");
            Assert.Equal("30", before);

            // Update the file, 2nd version
            File.WriteAllText(fullpath, @"
Formulas: |
  Func(x: Number) : Number = x * 20;
");

            HandleLine($"Import(\"{fullpath}\")");

            var after = HandleLine("Func(3)");
            Assert.Equal("60", after);
        }

        // Error gets spans. 
        [Fact]
        public void ErrorShowsFileRange()
        {
            Import("Error1.fx.yml", expectErrors: true);

            var errorMsg = _output.Get(OutputKind.Error);
                                    
            // Key elements here are that:
            // - the message has the filename
            // - the location (5,9) is relative into the file, not just the expression.
            Assert.Equal("Error: Error1.fx.yml (5,9): Name isn't valid. 'missing' isn't recognized.", errorMsg);
        }

        // Circular references are detected 
        [Fact]
        public void Cycles()
        {
            Import("cycle1.fx.yml", expectErrors: true);

            var errorMsg = _output.Get(OutputKind.Error);
            Assert.Contains("Circular reference", errorMsg);
        }

        // Loading a missing file 
        [Fact]
        public void Missing()
        {
            Import("missing_file.fx.yml", expectErrors: true);

            var errorMsg = _output.Get(OutputKind.Error);
            Assert.Contains("missing_file.fx.yml", errorMsg);
        }

        // Loading a file with yaml parse errors.
        [Fact]
        public void YamlParseErrors()
        {
            Import("yaml_parse_errors.fx.yml", expectErrors: true);

            var errorMsg = _output.Get(OutputKind.Error);
        }
    }
}
