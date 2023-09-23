// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PowerFx.Repl.Tests
{
    public class ReplTests
    {
        private readonly PowerFxRepl _repl;
        private readonly TestReplOutput _output = new TestReplOutput();

        public ReplTests()
        {            
            _repl = new PowerFxRepl
            {
                Output = _output,
                AllowSetDefinitions = true,
            };
        }

        [Fact]
        public void Test1()
        {
            _repl.HandleLine("1+2");

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal("3", log);
        } 

        [Fact]
        public void Set()
        {
            _repl.HandleLine("Set(x, 1+2); x * 10");

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal(
@"x: 3
30", log);
        }

        [Fact]
        public void TestIR()
        {
            // IR() function is a meta-function that
            // circumvents eval and dumps the IR. 
            _repl.HandleLine("IR(1+2)");

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal("AddDecimals:w(1:w, 2:w)", log);
        }
    }

    internal static class ReplExtensions
    {
        public static void HandleLine(this PowerFxRepl repl, string input)
        {
            repl.HandleLineAsync(input).Wait();
        }
    }
}
