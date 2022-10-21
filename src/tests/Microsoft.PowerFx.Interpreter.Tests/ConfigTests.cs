// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Tests.IntellisenseTests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ConfigTests
    {
        // Drection evaluation with values. 
        [Fact]
        public async Task BasicEvaluation()
        {
            // Per expression.
            var engine = new RecalcEngine();
            engine.UpdateVariable("global", 2.0);

            var locals = new SymbolValues()
                .Add("local", FormulaValue.New(3));

            var result = await engine.EvalAsync(
                "local+global",
                CancellationToken.None,
                runtimeConfig: locals);

            Assert.Equal(5.0, result.ToObject());
        }

        [Fact]
        public async Task BasicDirectEvalFunc()
        {
            // ReflectionFunctions are impls an interface for direct dispatch
            // hence we don't need a runtime config for it. 

            // Share a config
            var s1 = new SymbolTable();
            s1.AddFunction(new Func1Function());

            // Per expression.
            var engine = new RecalcEngine();
            var result = await engine.EvalAsync(
                "Func1(3)",
                CancellationToken.None,
                symbolTable: s1);
            Assert.Equal(6.0, result.ToObject());
        }

        [Fact]
        public void DisplayNameBinding()
        {
            var r1 = RecordType.Empty()
             .Add(new NamedFormulaType("Num", FormulaType.Number, "DisplayNum"));

            var s = ReadOnlySymbolTable.NewFromRecord(r1);
            var s2 = new SymbolTable() { Parent = s };

            var config = new PowerFxConfig
            {
                SymbolTable = s2
            };

            var engine = new RecalcEngine(config);
            var check = engine.Check("DisplayNum + 2");

            Assert.True(check.IsSuccess);
        }

        // Bind a function, eval it separately.
        // But still share symbol values.
        [Fact]
        public async Task BasicBindAndEvalFunc()
        {
            // Run separately 
            var r1 = new SymbolTable();
            r1.AddFunction(new Func1Function());

            var expr = "Func1(3)";
            var engine = new RecalcEngine();
            var check = engine.Check(expr, symbolTable: r1);
            Assert.True(check.IsSuccess);

            // Run separately 
            var run = check.GetEvaluator();

            // Functions are already captured from the symbols 
            // don't need to be re-added.
            var result = await run.EvalAsync(CancellationToken.None);
            Assert.Equal(6.0, result.ToObject());
        }

        // Trivial test function, just multiples by 2. 
        private class Func1Function : ReflectionFunction
        {
            // Must have "Execute" method. 
            public NumberValue Execute(NumberValue x)
            {
                var val = x.Value;
                return FormulaValue.New(val * 2);
            }
        }

        // Another Test function. 
        private class MultiplyFunction : ReflectionFunction
        {
            private readonly int _factor;

            public MultiplyFunction(int factor)
            {
                _factor = factor;
            }

            public MultiplyFunction()
                : this(2)
            {
            }

            // Must have "Execute" method. 
            public NumberValue Execute(NumberValue x)
            {
                var val = x.Value;
                return FormulaValue.New(val * _factor);
            }
        }

        // Mutate the config after binding to show that execution doesn't rely on it. 
        [Fact]
        public async Task ExecutionDoesntUseConfig()
        {
            // Share a config
            var s1 = new SymbolTable();
            s1.AddFunction(new MultiplyFunction(3));

            // Per expression.
            var engine = new RecalcEngine();

            var check = engine.Check("Multiply(2)", symbolTable: s1);

            // Even removing it and mofiying the config afterwards doesn't matter
            // since execution doesn't use config. 
            s1.RemoveFunction("Multiply");
            var expr = check.GetEvaluator();

            var result = await expr.EvalAsync(CancellationToken.None);
            Assert.Equal(6.0, result.ToObject());

            // Rebinding  fails because we removed the func. 
            check = engine.Check("Multiply(2)", symbolTable: s1);
            Assert.False(check.IsSuccess);

            // Re-add with same name, but configured differently. 
            s1.AddFunction(new MultiplyFunction(4));

            // Executing old expression is *still* unaffected (does not pickup new function with same name)
            result = await expr.EvalAsync(CancellationToken.None);
            Assert.Equal(6.0, result.ToObject());

            // But rebinding will pickup new ... 
            result = await engine.EvalAsync("Multiply(2)", CancellationToken.None, symbolTable: s1);
            Assert.Equal(8.0, result.ToObject());
        }

        [Fact]
        public async Task Shadow()
        {
            var s1 = new SymbolTable();
            s1.AddFunction(new MultiplyFunction(3));
            s1.AddFunction(new Func1Function());

            var s2 = new SymbolTable
            {
                Parent = s1
            };
            s2.AddFunction(new MultiplyFunction(4)); // Shadows s1

            var engine = new RecalcEngine();

            // Multiply was shadowed
            var result = await engine.EvalAsync("Multiply(2)", CancellationToken.None, symbolTable: s2);
            Assert.Equal(2 * 4.0, result.ToObject());

            // Func1 was not shadowed, so inherit. 
            result = await engine.EvalAsync("Func1(2)", CancellationToken.None, symbolTable: s2);
            Assert.Equal(2 * 2.0, result.ToObject());
        }

        // Show symbol tables can be shared
        [Fact]
        public async Task Shared()
        {
            // Share a config
            var sCommon = new SymbolTable();
            sCommon.AddFunction(new Func1Function());

            var s3 = new SymbolTable
            {
                Parent = sCommon
            };
            s3.AddFunction(new MultiplyFunction(3));

            var s4 = new SymbolTable
            {
                Parent = sCommon
            };
            s4.AddFunction(new MultiplyFunction(4));

            // Per expression.
            // Same engine *instance*, same expression, but different configs. 
            // Calls something from both shared config and individual config. 
            var engine = new RecalcEngine();
            var expr = "Func1(1) & Multiply(2)";

            var result3 = await engine.EvalAsync(
                expr,
                CancellationToken.None,
                symbolTable: s3); // 1*2 & 2*3  = "26"
            Assert.Equal("26", result3.ToObject());

            var result4 = await engine.EvalAsync(
                expr,
                CancellationToken.None,
                symbolTable: s4); // 1*2 & 2*4  = "28"            
            Assert.Equal("28", result4.ToObject());
        }

        [Fact]
        public void RecalcEngine_Symbol_CultureInfo()
        {
            var us_Symbols = new SymbolValues();
            var us_Culture = new CultureInfo("en-US");
            us_Symbols.AddService(us_Culture);
            var us_ParserOptions = new ParserOptions() { Culture = us_Culture };

            var fr_Symbols = new SymbolValues();
            var fr_Culture = new CultureInfo("fr-FR");
            fr_Symbols.AddService(fr_Culture);
            var fr_ParserOptions = new ParserOptions() { Culture = fr_Culture };

            var fa_Symbols = new SymbolValues();
            var fa_Culture = new CultureInfo("fa-IR");
            fa_Symbols.AddService(fa_Culture);

            var engine = new RecalcEngine();

            Assert.Equal(1.0, engine.EvalAsync("1.0", CancellationToken.None, options: us_ParserOptions, runtimeConfig: us_Symbols).Result.ToObject());
            Assert.ThrowsAsync<InvalidOperationException>(() => engine.EvalAsync("1.0", CancellationToken.None, options: fr_ParserOptions, runtimeConfig: fr_Symbols));
            Assert.ThrowsAsync<InvalidOperationException>(() => engine.EvalAsync("2,0", CancellationToken.None, options: us_ParserOptions, runtimeConfig: us_Symbols));
            Assert.Equal(2.0, engine.EvalAsync("2,0", CancellationToken.None, options: fr_ParserOptions, runtimeConfig: fr_Symbols).Result.ToObject());

            Assert.Equal("2/01", engine.EvalAsync("Text(2,01)", CancellationToken.None, options: fr_ParserOptions, runtimeConfig: fa_Symbols).Result.ToObject());
        }

        // Verify that a single IR tree can be re-executed across multiple cultures.
        [Fact]
        public void RecalcEngine_Symbol_CultureInfo2()
        {
            var us_Symbols = new SymbolValues();
            var us_Culture = new CultureInfo("en-US");
            us_Symbols.AddService(us_Culture);
            var us_ParserOptions = new ParserOptions() { Culture = us_Culture };

            var fr_Symbols = new SymbolValues();
            var fr_Culture = new CultureInfo("fr-FR");
            fr_Symbols.AddService(fr_Culture);
            var fr_ParserOptions = new ParserOptions() { Culture = fr_Culture };

            var engine = new RecalcEngine();

            var expr1 = engine.Check("Text(1.01)", options: us_ParserOptions).GetEvaluator();
            var expr2 = engine.Check("Text(2,01)", options: fr_ParserOptions).GetEvaluator();

            Assert.Equal("1.01", (expr1.Eval(us_Symbols) as StringValue).Value);
            Assert.Equal("1,01", (expr1.Eval(fr_Symbols) as StringValue).Value);

            Assert.Equal("2.01", (expr2.Eval(us_Symbols) as StringValue).Value);
            Assert.Equal("2,01", (expr2.Eval(fr_Symbols) as StringValue).Value);
        }

        // Verify if text is transformed using the correct culture info (PowerFxConfig and Symbols)
        [Fact]
        public void RecalcEngine_Symbol_CultureInfo3()
        {
            var config = new PowerFxConfig(CultureInfo.InvariantCulture);
            var engine = new RecalcEngine(config);

            var tr_symbols = new SymbolValues();

            tr_symbols.AddService(new CultureInfo("tr-TR"));

            var textExpression = "Upper(\"indigo\")";
            var datetimeExpression = "Text(DateTimeValue(\"Perşembe 6 Ekim 2022 14:19:06\", \"tr-TR\"))";

            var check = engine.Check(textExpression).GetEvaluator();

            Assert.Equal("INDIGO", (check.Eval() as StringValue).Value);
            Assert.Equal("İNDİGO", (check.Eval(runtimeConfig: tr_symbols) as StringValue).Value);

            check = engine.Check(datetimeExpression).GetEvaluator();

            Assert.Equal("10/06/2022 14:19", (check.Eval() as StringValue).Value);
            Assert.Equal("6.10.2022 14:19", (check.Eval(runtimeConfig: tr_symbols) as StringValue).Value);
        }

        // Verify if text is transformed using the correct culture info (PowerFxConfig and Symbols)
        [Fact]
        public void RecalcEngine_Symbol_CultureInfo4()
        {
            var config = new PowerFxConfig(CultureInfo.InvariantCulture);
            var engine = new RecalcEngine(config);

            var us_symbols = new SymbolValues();

            us_symbols.AddService(new CultureInfo("en-US"));

            var textExpression = "Upper(\"indigo\")";
            var datetimeExpression = "Text(DateTimeValue(\"Perşembe 06 Ekim 2022 14:19:06\", \"tr-TR\"))";

            var check = engine.Check(textExpression).GetEvaluator();

            Assert.Equal("INDIGO", (check.Eval() as StringValue).Value);
            Assert.Equal("INDIGO", (check.Eval(runtimeConfig: us_symbols) as StringValue).Value);

            check = engine.Check(datetimeExpression).GetEvaluator();

            Assert.Equal("10/06/2022 14:19", (check.Eval() as StringValue).Value);
            Assert.Equal("10/6/2022 2:19 PM", (check.Eval(runtimeConfig: us_symbols) as StringValue).Value);
        }

        // Verify if text is transformed using the correct culture info (PowerFxConfig and global settings)
        [Fact]
        public void RecalcEngine_Symbol_CultureInfo5()
        {
            Exception exception = null;

            var t = new Thread(() =>
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

                var config = new PowerFxConfig(CultureInfo.InvariantCulture);
                var engine = new RecalcEngine(config);

                var symbols = new SymbolValues();

                var upperExpression = "Upper(\"inDigo\")";
                var lowerExpression = "Lower(\"inDigo\")";
                var properExpression = "Proper(\"inDigo\")";

                var datetimeExpression = "Text(DateTimeValue(\"Perşembe 06 Ekim 2022 14:19:06\", \"tr-TR\"))";

                try
                {
                    var result = engine.Eval(upperExpression);
                    Assert.Equal("INDIGO", (result as StringValue).Value);

                    result = engine.Eval(lowerExpression);
                    Assert.Equal("indigo", (result as StringValue).Value);

                    result = engine.Eval(properExpression);
                    Assert.Equal("Indigo", (result as StringValue).Value);

                    result = engine.Eval(datetimeExpression);
                    Assert.Equal("10/06/2022 14:19", (result as StringValue).Value);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }                
            });

            t.Start();
            t.Join();

            if (exception != null)
            {
                throw exception;
            }
        }

        // Verify that an engine with a specific culture can evaluate an invariant formula
        [Fact]
        public void RecalcEngine_CultureInfo()
        {
            var fr_culture = new CultureInfo("fr-FR");
            var invariant_culture = CultureInfo.InvariantCulture;

            var engine = new RecalcEngine(new PowerFxConfig(fr_culture));

            var fr_formula = "Concatenate(\"My \";\"french \";\"formula\")";
            var invariant_formula = engine.GetInvariantExpression(fr_formula, RecordType.Empty());

            var fr_ParserOptions = new ParserOptions() { Culture = fr_culture };
            var invariant_ParserOptions = new ParserOptions() { Culture = invariant_culture };

            Assert.Throws<AggregateException>(() => engine.Eval(invariant_formula, options: fr_ParserOptions));
            Assert.Equal("My french formula", engine.Eval(fr_formula, options: fr_ParserOptions).ToObject());
            Assert.Equal("My french formula", engine.Eval(invariant_formula, options: invariant_ParserOptions).ToObject());
        }

        private static void AssertUnique(HashSet<VersionHash> set, VersionHash hash)
        {
            Assert.True(set.Add(hash), "Hash value should be unique");
        }

        private static void AssertUnique(HashSet<VersionHash> set, SymbolTable symbolTable)
        {
            AssertUnique(set, symbolTable.VersionHash);
        }

        // Changing the config changes its hash.
        // Verify with function mutations. 
        [Fact]
        public void ConfigHashWithFunctions()
        {
            var set = new HashSet<VersionHash>();

            var s1 = new SymbolTable();
            AssertUnique(set, s1);

            var func = new Func1Function();
            s1.AddFunction(func);
            AssertUnique(set, s1);

            s1.RemoveFunction("Func1");
            AssertUnique(set, s1);

            // Same as before, but should still be unique VersionHash!
            s1.AddFunction(func);
            AssertUnique(set, s1);
        }

        // bump version number in a virtual callback that's called during binding. 
        // This lets us deterministically simulate a "mutation" in the middle of binding. 
        private class HookTable : SymbolTable
        {
            internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
            {
                // Nop to just bump the version number.
                RemoveVariable("missing");

                return base.TryLookup(name, out nameInfo);
            }
        }

        // If we mutate symbol table during binding, it fails. 
        // Host orchestrates both binding and symbol table, so they are responsible to ensure this doesn't happen. 
        [Fact]
        public void MutationProtection()
        {
            var s1 = new HookTable();

            var engine = new Engine(new PowerFxConfig());

            Assert.Throws<InvalidOperationException>(
                () => engine.Check("x", symbolTable: s1));
        }

        // Add a function that gets different runtime state per expression invoke
        [Fact]
        public async Task LocalFunction()
        {
            // Share a config
            var s1 = new SymbolTable();
            s1.AddFunction(new UserFunction());

            var engine = new RecalcEngine();

            var check = engine.Check(
                "User(3)",
                symbolTable: s1);

            // Bind expression once, and then can pass in per-eval state. 
            var expr = check.GetEvaluator();

            foreach (var name in new string[] { "Bill", "Steve", "Satya" })
            {
                var runtime = new SymbolValues()
                    .AddService(new UserFunction.Runtime { _name = name });
                var result = await expr.EvalAsync(CancellationToken.None, runtime);

                var expected = name + "3";
                var actual = result.ToObject();
                Assert.Equal(expected, actual);
            }
        }

        // A test function that has per-eval state (rather than global)
        private class UserFunction : ReflectionFunction
        {
            public UserFunction()
            {
                // Specify the type used for config. 
                // At runtime, this is pulled from the RuntimeConfig config dictionary. 
                ConfigType = typeof(Runtime);
            }

            public class Runtime
            {
                public string _name;
            }

            // Must have "Execute" method. 
            // Arg0 is from RuntimeConfig state. 
            // Arg1 is a regular parameter. 
            public StringValue Execute(Runtime config, NumberValue x)
            {
                var val = x.Value;
                return FormulaValue.New(config._name + val);
            }
        }

        [Fact]
        public async Task LocalFunctionDirect()
        {
            var s1 = new SymbolTable();
            s1.AddFunction(new UserFunction());

            var runtime = new SymbolValues()
                .AddService(new UserFunction.Runtime { _name = "Bill" });

            var engine = new RecalcEngine();

            // Pass RuntimeConfig directly through eval.
            var result = await engine.EvalAsync(
                "User(3)",
                CancellationToken.None,
                symbolTable: s1,
                runtimeConfig: runtime);

            Assert.Equal("Bill3", result.ToObject());
        }

        // USe Services and parameters together, excercise chaining. 
        [Fact]
        public async Task LocalFunctionWithParameters()
        {
            var s1 = new SymbolTable();
            s1.AddFunction(new UserFunction());

            var r1 = new SymbolValues
            {
                DebugName = "Runtime-AddUserService"
            }.AddService(new UserFunction.Runtime { _name = "Bill" });

            var r2 = new SymbolValues
            {
                DebugName = "Runtime-X",
                Parent = r1
            }.Add("x", FormulaValue.New(3));

            var engine = new RecalcEngine();

            // Pass RuntimeConfig directly through eval.
            var result = await engine.EvalAsync(
                "User(x)",
                CancellationToken.None,
                symbolTable: s1,
                runtimeConfig: r2);

            Assert.Equal("Bill3", result.ToObject());
        }

        [Fact]
        public async Task SimpleParameters()
        {
            var s1 = new SymbolTable();
            s1.AddVariable("p1", FormulaValue.New(12));

            var engine = new Engine(new PowerFxConfig());
            var check = engine.Check("p1", symbolTable: s1);

            var eval = check.GetEvaluator();
            var result = await eval.EvalAsync(CancellationToken.None);

            Assert.Equal(12.0, result.ToObject());
        }

        // Bind global parameters without running them
        [Fact]
        public async Task ParametersBindOnly()
        {
            var s1 = new SymbolTable();
            s1.AddConstant("p1", FormulaValue.New(3));

            var engine = new Engine(new PowerFxConfig());
            var check = engine.Check("p1", symbolTable: s1);
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.Number, check.ReturnType);
        }

        // Bind global parameters and ensure lookup works by display name 
        [Fact]
        public async Task ParametersDisplayNameBindOnly()
        {
            var optionSet = new OptionSet("foo", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() { { "key1", "value1" } }));

            var s1 = new SymbolTable();
            s1.AddEntity(optionSet, displayName: new DName("DisplayFoo"));

            var engine = new Engine(new PowerFxConfig());
            var check = engine.Check("DisplayFoo.key1", symbolTable: s1);
            Assert.True(check.IsSuccess);
        }

        // Can change default builtin functions.
        // Different engines may have different supported sets. 
        [Fact]
        public async Task DefaultFunctions()
        {
            var engine = new Engine2();

            var expr1 = "Sum(1,2)"; // Built in
            var expr2 = "Func1(1)"; // Custom

            var result = engine.Check(expr1);
            Assert.True(result.IsSuccess);

            result = engine.Check(expr2);
            Assert.False(result.IsSuccess);

            // Switch default function set.
            // This removes Sum() and adds Func1(). 
            var defaultFunctions = new SymbolTable();
            defaultFunctions.AddFunction(new Func1Function());
            engine.UpdateSupportedFunctions(defaultFunctions);

            result = engine.Check(expr1);
            Assert.False(result.IsSuccess);

            result = engine.Check(expr2);
            Assert.True(result.IsSuccess);
        }

        // Helper to set Engine.SupportedFunctions.
        private class Engine2 : Engine
        {
            public Engine2()
                : base(new PowerFxConfig())
            {
            }

            public void UpdateSupportedFunctions(SymbolTable s)
            {
                SupportedFunctions = s;
            }
        }

        // Simulate adding a new resolver into the chain. 
        [Fact]
        public async Task Dataverse()
        {
            var c1 = new PowerFxConfig();

            // these will injecting a resolver
            c1.AddDataverse("Value1", FormulaValue.New(11));
            c1.AddDataverse("Value2", FormulaValue.New(22));

            var engine = new RecalcEngine(c1);
            var check = engine.Check("Value1 + Value2");

            Assert.True(check.IsSuccess);

            var eval = check.GetEvaluator();
            var result = await eval.EvalAsync(CancellationToken.None);

            Assert.Equal(33.0, result.ToObject());
        }

        [Theory]
        [InlineData("Abs", "Abs(-1)")]
        [InlineData("Abs", "If(true,Abs(-1))")]
        [InlineData("Abs", "If(false,Abs(-1))")]
        public void MutableSupportedFunctionsTest(string functionName, string expression)
        {
            var engine = new Engine(new PowerFxConfig());
            var symbolTable = engine.SupportedFunctions.GetMutableCopyOfFunctions();

            symbolTable.RemoveFunction(functionName);

            var engine2 = new Engine2();
            engine2.UpdateSupportedFunctions(symbolTable);

            var checkFalse = engine2.Check(expression);
            var checkTrue = engine2.Check("Value(\"1\")");

            Assert.True(checkTrue.IsSuccess);
            Assert.False(checkFalse.IsSuccess);
            Assert.Contains(checkFalse.Errors, e => e.MessageKey == "ErrUnknownFunction" && e.Message.Contains($"'{functionName}' is an unknown or unsupported function."));
        }
    } // end test class

    // Extension methods, need to be in a top-level class. 
    internal static class MyTestExt
    {
        public static void AddVariable(this SymbolTable symbolTable, string name, FormulaValue value)
        {
            symbolTable.AddConstant(name, value);
        }

        public static void AddDataverse(this PowerFxConfig config, string valueName, FormulaValue value)
        {
            var symbolTable = new DataverseSymbolTable
            {
                _valueName = valueName,
                _value = new DataverseSymbolTable.Wrapper { Value = value },
                Parent = config.SymbolTable
            };

            // Add to chain. 
            config.SymbolTable = symbolTable;
        }
    }

    // Injects a custom resolver
    internal class DataverseSymbolTable : SymbolTable
    {
        public string _valueName;
        public Wrapper _value;

        // IR will fetch via a ICanGetValue
        public class Wrapper : ICanGetValue
        {
            public FormulaValue Value { get; set; }
        }

        internal override bool TryLookup(DName name, out NameLookupInfo nameInfo)
        {
            if (name.Value == _valueName)
            {
                ICanGetValue irValue = _value;

                nameInfo = new NameLookupInfo(
                    BindKind.PowerFxResolvedObject,
                    _value.Value.Type._type,
                    DPath.Root,
                    0,
                    data: irValue);
                return true;
            }

            // Lookup in custom metadata
            return base.TryLookup(name, out nameInfo);
        }
    }
}
