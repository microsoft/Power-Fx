// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Types;
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
        public void WholeFile()
        {
            var file = @"
Set(x, 
   1) // multiline!
Set(y, x+1)
x+y
Set(z, x * 5 + y)
Notify(z)
";
            var lines = file.Split("\n");
            
            foreach (var line in lines)
            {
                _repl.HandleLine(line);
            }

            Assert.Empty(_output.Get(OutputKind.Error));

            var log = _output.Get(OutputKind.Notify);
            Assert.Equal("7", log);
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

        // When AllowSetDefinitions is false, we can update existing vars,
        // but we can't create new ones. 
        [Fact]
        public void AllowSetDefinitions()
        {
            _repl.AllowSetDefinitions = false;
            ((RecalcEngine)_repl.Engine).UpdateVariable("x", 10);

            // succeeds, pre-existing
            var replResult = _repl.HandleCommandAsync("Set(x, 20); x").Result;
            Assert.Equal("20", replResult.EvalResult.ToObject().ToString());

            // Fails, can't decalre new one
            var replResult2 = _repl.HandleCommandAsync("Set(y, 20); y").Result;
            Assert.False(replResult2.IsSuccess);
            Assert.Null(replResult2.EvalResult);
        }

        [Fact]
        public void SetConflict()
        {
            // Attempting to declare a variable with different types.
            // First one wins, 2nd one becomes an error. 
            _repl.HandleLine("Set(x, 1+2); Set(x, true);");

            var log = _output.Get(OutputKind.Error);
            Assert.True(log.Length > 0);

            // First set declared it. But starts as blank. 
            // Since whole expression got errors during binding (due to 2nd set),
            // the expression doesn't run and we stay blank. 
            var value = _repl.Engine.GetValue("x");
            Assert.IsType<BlankValue>(value);

            var ok = _repl.Engine.TryGetVariableType("x", out var type);
            Assert.True(ok);
            Assert.IsType<DecimalType>(type);
        }

        [Fact]
        public void SetErrors()
        {
            // First Set() has an error in RHS. 
            _repl.HandleLine("Set(x, some_error+1); Set(x, true);");

            var log = _output.Get(OutputKind.Error);
            Assert.True(log.Length > 0);

            // Failed to define at all. 
            var ok = _repl.Engine.TryGetVariableType("x", out var type);
            Assert.False(ok);            
        }

        [Fact]
        public void TestUser()
        {
            _repl.EnableUserObject(UserInfo.AllKeys);
            _repl.UserInfo = _userInfo.UserInfo;
            _repl.HandleLine("User.Email");

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal("\"susan@contoso.com\"", log);
        }

        [Fact]
        public void TestUserFails()
        {
            // Fails when EnableUserObject is not called 
            _repl.UserInfo = _userInfo.UserInfo;

            Assert.Throws<InvalidOperationException>(
                () => _repl.HandleLine("User.Email"));

            var log = _output.Get(OutputKind.Repl);
            Assert.Empty(log);
        }

        [Fact]
        public void TestIR()
        {
            _repl.AddPseudoFunction(new IRPseudoFunction());

            // IR() function is a meta-function that
            // circumvents eval and dumps the IR. 
            _repl.HandleLine("IR(1+2)");

            Assert.Empty(_output.Get(OutputKind.Error));

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal("AddDecimals:w(1:w, 2:w)", log);
        }

        [Fact]
        public void ExtraSymols()
        {
            SymbolValues extraValues = new SymbolValues("ExtraValues");
            extraValues.Add("X1", FormulaValue.New(10));

            _repl.ExtraSymbolValues = extraValues;

            _repl.HandleLine("X1+2");

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal("12", log);
        }

        [Fact]
        public void ExtraSymolsCantSet()
        {
            SymbolTable st = new SymbolTable() { DebugName = "ExtraValues" };
            var slot = st.AddVariable("Const1", FormulaType.Decimal, new SymbolProperties
            {
                 CanMutate = false,
                 CanSet = false
            });
            var extraValues = st.CreateValues();
            extraValues.Set(slot, FormulaValue.New(10));

            _repl.ExtraSymbolValues = extraValues;

            // Read ok 
            _repl.HandleLine("Const1+2");

            var log = _output.Get(OutputKind.Repl);
            Assert.Equal("12", log);

            // But can't set (doesn't declare a shadow copy).
            var replResult = _repl.HandleCommandAsync("Set(Const1, 99)").Result;
            Assert.False(replResult.IsSuccess); 
        }
    }

    internal static class ReplExtensions
    {
        public static void HandleLine(this PowerFxRepl repl, string input)
        {
            try
            {
                repl.HandleLineAsync(input).Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
