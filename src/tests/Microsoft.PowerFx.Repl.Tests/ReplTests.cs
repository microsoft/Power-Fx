// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.PowerFx.Repl.Tests
{
    public class ReplTests
    {
        private readonly PowerFxREPL _repl;
        private readonly TestReplOutput _output = new TestReplOutput();

        public ReplTests()
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
        public void TestHelp()
        {
            _repl.HandleLine("Help()");

            // Ensure Test ran and wrote something.
            var log = _output.Get(OutputKind.Notify);
            Assert.NotEmpty(log);
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

        [Fact]
        public void NamedFormulas()
        {
            _repl.HandleLine(
"Set(x,1)",
"NamedFormula1 = x*10",
"Notify(NamedFormula1)",
"Set(x,2);Notify(NamedFormula1)");

            var log = _output.Get(OutputKind.Notify);
            Assert.Equal(
@"10
20", log);
        }

        // test that we get back an error for an illegal named formula, as opposed to throwing an exception
        [Fact]
        public void BadNamedFormula()
        {
            _repl.HandleLine(
"Result = If( Mid(\"asdf\",Value,1) = \"a\", \"X\", \"Y\" )");

            var log = _output.Get(OutputKind.Error);
            Assert.True(log.Length > 0);
        }

        // test that we get back an error for redefining a named formula, as opposed to throwing an exception
        [Fact]
        public void BadRedefinedNamedFormula()
        {
            _repl.HandleLine("a = 3");
            _repl.HandleLine("a = \"hello\"");

            var log = _output.Get(OutputKind.Error);
            Assert.True(log.Length > 0);
        }

        // test that Exit() informs the host that an exit has been requested
        [Fact]
        public void Exit()
        {
            _repl.HandleLine("Exit()");
            Assert.True(_repl.ExitRequested);
        }

        // test that comments do not return a result
        [Fact]
        public void Comments()
        {
            _repl.HandleLine("// hello world");
            var log1 = _output.Get(OutputKind.Repl);
            Assert.True(log1.Length == 0);

            _repl.HandleLine("/* hello world */");
            var log2 = _output.Get(OutputKind.Repl);
            Assert.True(log2.Length == 0);

            _repl.HandleLine("/* hello */  /* world */");
            var log3 = _output.Get(OutputKind.Repl);
            Assert.True(log3.Length == 0);
        }

        // test suggestion for "help" with differnet case and without parens
        [Fact]
        public void SuggestHelp()
        {
            // should suggest
            _repl.HandleLine("help");
            Assert.Contains("Help()", _output.Get(OutputKind.Error));

            _repl.HandleLine("HELP");
            Assert.Contains("Help()", _output.Get(OutputKind.Error));

            _repl.HandleLine("help()");
            Assert.Contains("Help()", _output.Get(OutputKind.Error));

            _repl.HandleLine("HELP()");
            Assert.Contains("Help()", _output.Get(OutputKind.Error));

            // should not suggest
            _repl.HandleLine("HELF()");
            Assert.DoesNotContain("Help()", _output.Get(OutputKind.Error));

            _repl.HandleLine("helq");
            Assert.DoesNotContain("Help()", _output.Get(OutputKind.Error));
        }

        // test suggestion for "exit" with differnet case and without parens
        [Fact]
        public void SuggestExit()
        {
            // should suggest
            _repl.HandleLine("exit");
            Assert.Contains("Exit()", _output.Get(OutputKind.Error));

            _repl.HandleLine("EXIT");
            Assert.Contains("Exit()", _output.Get(OutputKind.Error));

            _repl.HandleLine("exit()");
            Assert.Contains("Exit()", _output.Get(OutputKind.Error));

            _repl.HandleLine("EXIT()");
            Assert.Contains("Exit()", _output.Get(OutputKind.Error));

            // should not suggest
            _repl.HandleLine("EXIP()");
            Assert.DoesNotContain("Exit()", _output.Get(OutputKind.Error));

            _repl.HandleLine("exif");
            Assert.DoesNotContain("Exit()", _output.Get(OutputKind.Error));
        }

        // test that newlines are properly placed, especailly with FormatTable
        [Fact]
        public void NewLinesBasicPrompt()
        {
            _repl.WritePromptAsync().Wait();

            var log1 = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log1 == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
        }

        [Fact]
        public void NewLinesContinuationPrompt()
        {
            _repl.WritePromptAsync().Wait();
            var log1p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log1p == @">> ");

            _repl.HandleLineAsync("Sqrt(4").Wait();     // intentionally left unclosed

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);

            _repl.WritePromptAsync().Wait();
            var log2p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log2p == @".. ");

            _repl.HandleLineAsync(")").Wait();          // and now closed

            _repl.WritePromptAsync().Wait();
            var log3p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log3p == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl) == "2");
        }

        [Fact]
        public void NewlinesValueTable()
        {
            _repl.WritePromptAsync().Wait();

            var log1p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log1p == @">> ");

            _repl.HandleCommandAsync(
"[1,2,3]").Wait();
            var log2 = _output.Get(OutputKind.Repl, trim: true);
            var expected2 = @"[1, 2, 3]";
            Assert.True(log2 == expected2);

            _repl.WritePromptAsync().Wait();
            var log2p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log2p == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
        }

        [Fact]
        public void EmptyValueTable()
        {
            _repl.WritePromptAsync().Wait();

            var log1p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log1p == @">> ");

            _repl.HandleCommandAsync(
"[1,2,3]").Wait();
            var log2 = _output.Get(OutputKind.Repl, trim: true);
            var expected2 = @"[1, 2, 3]";
            Assert.True(log2 == expected2);

            _repl.HandleCommandAsync(
"Filter([1,2,3],Value>4)").Wait();
            var log3 = _output.Get(OutputKind.Repl, trim: false);
            var expected3 = @"
<empty table>
";
            Assert.True(Regex.Replace(log3, @"\r?\n", @"\n") == Regex.Replace(expected3, @"\r?\n", @"\n"));

            _repl.WritePromptAsync().Wait();
            var log2p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log2p == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
        }

        [Fact]
        public void NewlinesFormatTable()
        {
            _repl.WritePromptAsync().Wait();

            var log1p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log1p == @">> ");

            // compare but ignore trailing whitespace at the end of each line
            _repl.HandleCommandAsync(
"Table({a:1},{b:2})").Wait();
            var log2 = _output.Get(OutputKind.Repl, trim: false);
            var expected2 = @"
  a   b  
 === === 
  1      
      2 
";
            Assert.True(Regex.Replace(log2, @"[ ]*\r?\n", @"\n") == Regex.Replace(expected2, @"[ ]*\r?\n", @"\n"));

            _repl.WritePromptAsync().Wait();
            var log2p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log2p == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
        }

        [Fact]
        public void NewlinesNamedFormulaFormatTable()
        {
            _repl.WritePromptAsync().Wait();
            var log1p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log1p == @">> ");

            // compare but ignore trailing whitespace at the end of each line
            _repl.HandleCommandAsync(
"MyTable = Table({a:1},{b:2})").Wait();
            var log2 = _output.Get(OutputKind.Repl, trim: false);
            var expected2 = @"MyTable:
  a   b  
 === === 
  1      
      2
";
            Assert.True(Regex.Replace(log2, @"[ ]*\r?\n", @"\n") == Regex.Replace(expected2, @"[ ]*\r?\n", @"\n"));

            _repl.WritePromptAsync().Wait();
            var log2p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log2p == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
        }

        [Fact]
        public void EmptyFormatTable()
        {
            _repl.WritePromptAsync().Wait();
            var log1p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log1p == @">> ");

            _repl.HandleCommandAsync(
"MyTable = Table({a:1},{b:2})").Wait();
            var log2 = _output.Get(OutputKind.Repl, trim: false);
            var expected2 = @"MyTable:
  a   b  
 === === 
  1      
      2
";
            Assert.True(Regex.Replace(log2, @"[ ]*\r?\n", @"\n") == Regex.Replace(expected2, @"[ ]*\r?\n", @"\n"));

            _repl.HandleCommandAsync(
"Filter( MyTable, a = b )").Wait();
            var log3 = _output.Get(OutputKind.Repl, trim: false);
            var expected3 = @"
<empty table>
";
            Assert.True(Regex.Replace(log3, @"\r?\n", @"\n") == Regex.Replace(expected3, @"\r?\n", @"\n"));

            _repl.WritePromptAsync().Wait();
            var log2p = _output.Get(OutputKind.Control, trim: false);
            Assert.True(log2p == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
        }

        [Fact]
        public void EchoAndPrintResult()
        {
            _repl.Echo = false;
            _repl.PrintResult = false;
            _repl.HandleCommandAsync(@"Notify( 1234 )").Wait();
            Assert.True(_output.Get(OutputKind.Notify, trim: false) == @"1234
");
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Control, trim: false) == string.Empty);

            _repl.Echo = true;
            _repl.PrintResult = false;
            _repl.HandleCommandAsync(@"Notify( 2345 )").Wait();
            Assert.True(_output.Get(OutputKind.Notify, trim: false) == @"2345
");
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == @"Notify( 2345 )
");
            Assert.True(_output.Get(OutputKind.Control, trim: false) == @">> ");

            _repl.Echo = false;
            _repl.PrintResult = true;
            _repl.HandleCommandAsync(@"Notify( 3456 )").Wait();
            Assert.True(_output.Get(OutputKind.Notify, trim: false) == @"3456
");
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == @"true
");
            Assert.True(_output.Get(OutputKind.Control, trim: false) == string.Empty);

            _repl.Echo = true;
            _repl.PrintResult = true;
            _repl.HandleCommandAsync(@"Notify( 4567 )").Wait();
            Assert.True(_output.Get(OutputKind.Notify, trim: false) == @"4567
");
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == @"Notify( 4567 )
true
");
            Assert.True(_output.Get(OutputKind.Control, trim: false) == @">> ");

            Assert.True(_output.Get(OutputKind.Error, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
        }

        [Fact]
        public void LineNumbersInErrors()
        {
            _repl.HandleCommandAsync(@"2 +-* s", lineNumber: 7891).Wait();

            var errors = _output.Get(OutputKind.Error, trim: false);

            // make sure there is at least one error
            Assert.StartsWith("Line ", errors);

            using (StringReader reader = new StringReader(errors))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Assert.StartsWith("Line 7891: ", errors);
                }
            }

            Assert.True(_output.Get(OutputKind.Notify, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Repl, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Control, trim: false) == string.Empty);
            Assert.True(_output.Get(OutputKind.Warning, trim: false) == string.Empty);
        }
    }

    internal static class ReplExtensions
    {
        public static void HandleLine(this PowerFxREPL repl, params string[] inputs)
        {
            foreach (var input in inputs)
            {
                repl.HandleLine(input);
            }
        }

        public static void HandleLine(this PowerFxREPL repl, string input)
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
