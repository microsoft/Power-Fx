// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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

        private static readonly BasicUserInfo _userInfo = new BasicUserInfo
        {
            FullName = "Susan Burk",
            Email = "susan@contoso.com",
            DataverseUserId = new Guid("aa1d4f65-044f-4928-a95f-30d4c8ebf118"),
            TeamsMemberId = "29:1DUjC5z4ttsBQa0fX2O7B0IDu30R",
            EntraObjectId = new Guid("99999999-044f-4928-a95f-30d4c8ebf118"),
        };

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
        public void TestUser()
        {
            _repl.UserInfo = _userInfo.UserInfo;
            _repl.HandleLine("User.Email");

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal("\"susan@contoso.com\"", log);
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
