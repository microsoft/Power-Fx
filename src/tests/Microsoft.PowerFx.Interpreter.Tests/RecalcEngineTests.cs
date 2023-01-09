// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Interpreter.UDF;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Tests
{
    public class RecalcEngineTests : PowerFxTest
    {
        [Fact]
        public void PublicSurfaceTests()
        {
            var asm = typeof(RecalcEngine).Assembly;

            var ns = "Microsoft.PowerFx";
            var nsType = "Microsoft.PowerFx.Types";
            var allowed = new HashSet<string>()
            {
                $"{ns}.{nameof(CheckResultExtensions)}",
                $"{ns}.{nameof(ReadOnlySymbolValues)}",
                $"{ns}.{nameof(RecalcEngine)}",
                $"{ns}.{nameof(ReflectionFunction)}",
#pragma warning disable CS0618 // Type or member is obsolete
                $"{ns}.{nameof(RecalcEngineScope)}",
#pragma warning restore CS0618 // Type or member is obsolete
                $"{ns}.{nameof(PowerFxConfigExtensions)}",
                $"{ns}.{nameof(IExpressionEvaluator)}",
                $"{ns}.{nameof(ITypeMarshallerProvider)}",
                $"{ns}.{nameof(ITypeMarshaller)}",
                $"{ns}.{nameof(IDynamicTypeMarshaller)}",
                $"{ns}.{nameof(ObjectMarshallerProvider)}",
                $"{ns}.{nameof(ObjectMarshaller)}",
                $"{ns}.{nameof(BasicServiceProvider)}",
                $"{ns}.{nameof(IRuntimeConfig)}",
                $"{ns}.{nameof(RuntimeConfig)}",
                $"{ns}.{nameof(PrimitiveMarshallerProvider)}",
                $"{ns}.{nameof(PrimitiveTypeMarshaller)}",
                $"{ns}.{nameof(SymbolValues)}",
                $"{ns}.{nameof(TableMarshallerProvider)}",
                $"{ns}.{nameof(TypeMarshallerCache)}",
                $"{ns}.{nameof(TypeMarshallerCacheExtensions)}",
                $"{ns}.{nameof(SymbolExtensions)}",
                $"{nsType}.{nameof(ObjectRecordValue)}",
                $"{nsType}.{nameof(QueryableTableValue)}",
                $"{ns}.InterpreterConfigException",
                $"{ns}.Interpreter.{nameof(NotDelegableException)}",
                $"{ns}.Interpreter.UDF.{nameof(DefineFunctionsResult)}",                               

                // Services for functions. 
                $"{ns}.Functions.IRandomService"
            };

            var sb = new StringBuilder();
            foreach (var type in asm.GetTypes().Where(t => t.IsPublic))
            {
                var name = type.FullName;
                if (!allowed.Contains(name))
                {
                    sb.Append(name);
                    sb.Append("; ");
                }

                allowed.Remove(name);
            }

            Assert.True(sb.Length == 0, $"Unexpected public types: {sb}");

            // Types we expect to be in the assembly aren't there. 
            if (allowed.Count > 0)
            {
                throw new XunitException("Types missing: " + string.Join(",", allowed.ToArray()));
            }
        }

        [Fact]
        public void EvalWithGlobals()
        {
            var cache = new TypeMarshallerCache();

            var engine = new RecalcEngine();

            var context = cache.NewRecord(new
            {
                x = 15
            });
            var result = engine.Eval("With({y:2}, x+y)", context);

            Assert.Equal(17.0, ((NumberValue)result).Value);
        }

        /// <summary>
        /// Test that helps to ensure that RecalcEngine performs evaluation in thread safe manner.
        /// </summary>
        [Fact]
        public void EvalInMultipleThreads()
        {
            var engine = new RecalcEngine();
            Parallel.For(
                0,
                10000,
                (i) =>
                {
                    Assert.Equal("5", engine.Eval("10-5").ToObject().ToString());
                    Assert.Equal("True", engine.Eval("true Or false").ToObject().ToString());
                    Assert.Equal("15", engine.Eval("10+5").ToObject().ToString());
                });
        }

        [Fact]
        public void BasicRecalc()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", 15);
            engine.SetFormula("B", "A*2", OnUpdate);
            AssertUpdate("B-->30;");

            engine.UpdateVariable("A", 20);
            AssertUpdate("B-->40;");

            // Ensure we can update to null. 
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.Number));
            AssertUpdate("B-->0;");
        }

        // depend on grand child directly 
        [Fact]
        public void Recalc2()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", 1);
            engine.SetFormula("B", "A*10", OnUpdate);
            AssertUpdate("B-->10;");

            engine.SetFormula("C", "B+5", OnUpdate);
            AssertUpdate("C-->15;");

            // depend on grand child directly 
            engine.SetFormula("D", "B+A", OnUpdate);
            AssertUpdate("D-->11;");

            // Updating A will recalc both D and B. 
            // But D also depends on B, so verify D pulls new value of B. 
            engine.UpdateVariable("A", 2);

            // Batched up (we don't double fire)            
            AssertUpdate("B-->20;C-->25;D-->22;");
        }

        [Fact]
        public void DeleteFormula()
        {
            var engine = new RecalcEngine();

            engine.UpdateVariable("A", 1);
            engine.SetFormula("B", "A*10", OnUpdate);
            engine.SetFormula("C", "B+5", OnUpdate);
            engine.SetFormula("D", "B+A", OnUpdate);

            Assert.Throws<InvalidOperationException>(() =>
                engine.DeleteFormula("X"));

            Assert.Throws<InvalidOperationException>(() =>
                engine.DeleteFormula("B"));

            engine.DeleteFormula("D");
            Assert.False(engine.TryGetByName("D", out var retD));

            engine.DeleteFormula("C");
            Assert.False(engine.TryGetByName("C", out var retC));

            // After C and D are deleted, deleting B should pass
            engine.DeleteFormula("B");

            // Ensure B is gone
            engine.Check("B");
            Assert.Throws<InvalidOperationException>(() =>
                engine.Check("B").ThrowOnErrors());
        }

        // Don't fire for formulas that aren't touched by an update
        [Fact]
        public void RecalcNoExtraCallbacks()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A1", 1);
            engine.UpdateVariable("A2", 5);

            engine.SetFormula("B", "A1+A2", OnUpdate);
            AssertUpdate("B-->6;");

            engine.SetFormula("C", "A2*10", OnUpdate);
            AssertUpdate("C-->50;");

            engine.UpdateVariable("A1", 2);
            AssertUpdate("B-->7;"); // Don't fire C, not touched

            engine.UpdateVariable("A2", 7);
            AssertUpdate("B-->9;C-->70;");
        }

        private static readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Fact]
        public void SetFormula()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("A", 1);
            engine.SetFormula("B", "A*2", OnUpdate);
            AssertUpdate("B-->2;");

            // Can't set formulas, they're read only 
            var check = engine.Check("Set(B, 12)"); 
            Assert.False(check.IsSuccess);

            // Set() function triggers recalc chain. 
            engine.Eval("Set(A,2)", options: _opts);
            AssertUpdate("B-->4;");

            // Compare Before/After set within an expression.
            // Before (A,B) = 2,4 
            // After  (A,B) = 3,6
            var result = engine.Eval("With({x:A, y:B}, Set(A,3); x & y & A & B)", options: _opts);
            Assert.Equal("2436", result.ToObject());

            AssertUpdate("B-->6;");
        }

        [Fact]
        public void BasicEval()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("M", 10.0);
            engine.UpdateVariable("M2", -4);
            var result = engine.Eval("M + Abs(M2)");
            Assert.Equal(14.0, ((NumberValue)result).Value);
        }

        [Fact]
        public void FormulaErrorUndefined()
        {
            var engine = new RecalcEngine();

            // formula fails since 'B' is undefined. 
            Assert.Throws<InvalidOperationException>(() =>
               engine.SetFormula("A", "B*2", OnUpdate));
        }

        [Fact]
        public void CantChangeType()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("a", FormulaValue.New(12));

            // not supported: Can't change a variable's type.
            Assert.Throws<InvalidOperationException>(() =>
                engine.UpdateVariable("a", FormulaValue.New("str")));
        }

        [Fact]
        public void FormulaCantRedefine()
        {
            var engine = new RecalcEngine();

            engine.SetFormula("A", "2", OnUpdate);

            // Can't redefine an existing formula. 
            Assert.Throws<InvalidOperationException>(() =>
               engine.SetFormula("A", "3", OnUpdate));
        }

        [Fact]
        public void DefFunc()
        {
            var config = new PowerFxConfig(null);
            var recalcEngine = new RecalcEngine(config);

            IEnumerable<ExpressionError> enumerable = recalcEngine.DefineFunctions(
            new UDFDefinition(
                "foo",
                "x * y",
                FormulaType.Number,
                false,
                new NamedFormulaType("x", FormulaType.Number),
                new NamedFormulaType("y", FormulaType.Number))).Errors;
            Assert.False(enumerable.Any());
            Assert.Equal(17.0, recalcEngine.Eval("foo(3,4) + 5").ToObject());
        }

        [Fact]
        public void DefRecursiveFunc()
        {
            var config = new PowerFxConfig(null);
            var recalcEngine = new RecalcEngine(config);
            var body = @"If(x=0,foo(1),If(x=1,foo(2),If(x=2,2)))";
            IEnumerable<ExpressionError> enumerable = recalcEngine.DefineFunctions(
                new UDFDefinition(
                    "foo",
                    body,
                    FormulaType.Number,
                    false,
                    new NamedFormulaType("x", FormulaType.Number))).Errors;
            var result = recalcEngine.Eval("foo(0)");
            Assert.Equal(2.0, result.ToObject());
            Assert.False(enumerable.Any());
        }

        [Fact]
        public void DefSimpleRecursiveFunc()
        {
            var config = new PowerFxConfig(null);
            var recalcEngine = new RecalcEngine(config);
            Assert.False(recalcEngine.DefineFunctions(
                new UDFDefinition(
                    "foo",
                    "foo()",
                    FormulaType.Blank,
                    false)).Errors.Any());
            var result = recalcEngine.Eval("foo()");
            Assert.IsType<ErrorValue>(result);
        }

        [Fact]
        public void DefHailstoneSequence()
        {
            var config = new PowerFxConfig(null)
            {
                MaxCallDepth = 100
            };
            var recalcEngine = new RecalcEngine(config);
            var body = @"If(Not(x = 1), If(Mod(x, 2)=0, hailstone(x/2), hailstone(3*x+1)), x)";
            var funcName = "hailstone";
            var returnType = FormulaType.Number;
            var variable = new NamedFormulaType("x", FormulaType.Number);

            Assert.False(recalcEngine.DefineFunctions(
                new UDFDefinition(funcName, body, returnType, false, variable)).Errors.Any());
            Assert.Equal(1.0, recalcEngine.Eval("hailstone(192)").ToObject());
        }

        [Fact]
        public void DefMutualRecursionFunc()
        {
            var config = new PowerFxConfig(null)
            {
                MaxCallDepth = 100
            };
            var recalcEngine = new RecalcEngine(config);
            var bodyEven = @"If(number = 0, true, odd(Abs(number)-1))";
            var bodyOdd = @"If(number = 0, false, even(Abs(number)-1))";

            var udfOdd = new UDFDefinition(
                "odd",
                bodyOdd,
                FormulaType.Boolean,
                false,
                new NamedFormulaType("number", FormulaType.Number));
            var udfEven = new UDFDefinition(
                "even",
                bodyEven,
                FormulaType.Boolean,
                false,
                new NamedFormulaType("number", FormulaType.Number));

            Assert.False(recalcEngine.DefineFunctions(udfOdd, udfEven).Errors.Any());

            Assert.Equal(true, recalcEngine.Eval("odd(17)").ToObject());
            Assert.Equal(false, recalcEngine.Eval("even(17)").ToObject());
        }

        [Fact]
        public async void RedefinitionError()
        {
            var config = new PowerFxConfig(null);
            var recalcEngine = new RecalcEngine(config);
            Assert.Throws<InvalidOperationException>(() => recalcEngine.DefineFunctions(
                new UDFDefinition("foo", "foo()", FormulaType.Blank, false),
                new UDFDefinition("foo", "x+1", FormulaType.Number, false)));
        }

        [Fact]
        public void UDFBodySyntaxErrorTest()
        {
            var config = new PowerFxConfig(null);
            var recalcEngine = new RecalcEngine(config);
            Assert.True(recalcEngine.DefineFunctions(new UDFDefinition("foo", "x[", FormulaType.Blank, false)).Errors.Any());
        }

        [Fact]
        public async void UDFIncorrectParametersTest()
        {
            var config = new PowerFxConfig(null);
            var recalcEngine = new RecalcEngine(config);
            Assert.False(recalcEngine.DefineFunctions(new UDFDefinition("foo", "x+1", FormulaType.Number, false, new NamedFormulaType("x", FormulaType.Number))).Errors.Any());
            Assert.False(recalcEngine.Check("foo(False)").IsSuccess);
            Assert.False(recalcEngine.Check("foo(Table( { Value: \"Strawberry\" }, { Value: \"Vanilla\" } ))").IsSuccess);
            Assert.True(recalcEngine.Check("foo(1)").IsSuccess);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await recalcEngine.EvalAsync("foo(False)", CancellationToken.None));
        }

        [Fact]
        public void PropagateNull()
        {
            var engine = new RecalcEngine();
            engine.SetFormula("A", expr: "Blank()", OnUpdate);
            engine.SetFormula("B", "A", OnUpdate);

            var b = engine.GetValue("B");
            Assert.True(b is BlankValue);
        }

        // Record with null values. 
        [Fact]
        public void ChangeRecord()
        {
            var engine = new RecalcEngine();

            engine.UpdateVariable("R", FormulaValue.NewRecordFromFields(
                new NamedValue("F1", FormulaValue.NewBlank(FormulaType.Number)),
                new NamedValue("F2", FormulaValue.New(6))));

            engine.SetFormula("A", "R.F2 + 3 + R.F1", OnUpdate);
            AssertUpdate("A-->9;");

            engine.UpdateVariable("R", FormulaValue.NewRecordFromFields(
                new NamedValue("F1", FormulaValue.New(2)),
                new NamedValue("F2", FormulaValue.New(7))));
            AssertUpdate("A-->12;");
        }

        [Fact]
        public void CheckFunctionCounts()
        {
            var engine1 = new Engine(new PowerFxConfig());

            // Pick a function in core but not implemented in interpreter.
            var nyiFunc = BuiltinFunctionsCore.ISOWeekNum;

            Assert.Contains(nyiFunc, engine1.Functions);

            // RecalcEngine will add the interpreter's functions. 
            var engine2 = new RecalcEngine();

            Assert.DoesNotContain(nyiFunc, engine2.Functions);

            var names = engine2.GetAllFunctionNames().ToArray();
            Assert.True(names.Length > 100);

            // Spot check some known functions
            Assert.Contains("Cos", names);
            Assert.Contains("Filter", names);

            Assert.Contains("Cos", names);
        }

        [Fact]
        public void CheckSuccess()
        {
            var engine = new RecalcEngine();
            var result = engine.Check(
                "3*2+x",
                RecordType.Empty().Add(
                    new NamedFormulaType("x", FormulaType.Number)));

            Assert.True(result.IsSuccess);
            Assert.True(result.ReturnType is NumberType);
            Assert.Single(result.TopLevelIdentifiers);
            Assert.Equal("x", result.TopLevelIdentifiers.First());
        }

        [Fact]
        public void CanRunWithWarnings()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            var result = engine.Check("T.Var = 23", RecordType.Empty()
                .Add(new NamedFormulaType("T", RecordType.Empty().Add(new NamedFormulaType("Var", FormulaType.String)))));

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Errors.Count(x => x.IsWarning));
        }

        [Fact]
        public void CheckSuccessWarning()
        {
            var engine = new RecalcEngine();

            // issues a warning, verify it's still successful.
            var result = engine.Check("Filter([1,2,3],true)");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Errors.Count(x => x.Severity == ErrorSeverity.Warning));
            Assert.NotNull(result.Expression);
        }

        [Fact]
        public void CheckParseError()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("3*1+");

            Assert.False(result.IsSuccess);
            Assert.StartsWith("Error 4-4: Expected an operand", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckBindError()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("3+foo+2"); // foo is undefined 

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 2-5: Name isn't valid. 'foo' isn't recognized", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckLambdaBindError()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("Filter([1,2,3] As X, X.Value > foo)");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 31-34: Name isn't valid. 'foo' isn't recognized", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckDottedBindError()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("[1,2,3].foo");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 7-11: Name isn't valid. 'foo' isn't recognized", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckDottedBindError2()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("[].Value");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 2-8: Name isn't valid. 'Value' isn't recognized", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckBindEnum()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("TimeUnit.Hours");

            Assert.True(result.IsSuccess);

            // The resultant type will be the underlying type of the enum provided to 
            // check.  In the case of TimeUnit, this is StringType
            Assert.True(result.ReturnType is StringType);
            Assert.Empty(result.TopLevelIdentifiers);
        }

        [Fact]
        public void CheckBindErrorWithParseExpression()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("3+foo+2", RecordType.Empty()); // foo is undefined 

            Assert.False(result.IsSuccess);
            Assert.Null(result.Expression);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 2-5: Name isn't valid. 'foo' isn't recognized", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckSuccessWithParsedExpression()
        {
            var engine = new RecalcEngine();
            var result = engine.Check(
                "3*2+x",
                RecordType.Empty().Add(
                    new NamedFormulaType("x", FormulaType.Number)));

            // Test that parsing worked
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Expression);
            Assert.True(result.ReturnType is NumberType);
            Assert.Single(result.TopLevelIdentifiers);
            Assert.Equal("x", result.TopLevelIdentifiers.First());

            // Test evaluation of parsed expression
            var recordValue = FormulaValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(5)));
            var formulaValue = result.GetEvaluator().Eval(recordValue);
            Assert.Equal(11.0, (double)formulaValue.ToObject());
        }

        // Test Globals + Locals + GetValuator() 
        [Fact]
        public void CheckGlobalAndLocal()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("y", FormulaValue.New(10));

            var result = engine.Check(
                "x+y",
                RecordType.Empty().Add(
                    new NamedFormulaType("x", FormulaType.Number)));

            // Test that parsing worked
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Expression);
            Assert.True(result.ReturnType is NumberType);

            // Test evaluation of parsed expression
            var recordValue = FormulaValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(5)));

            var formulaValue = result.GetEvaluator().Eval(recordValue);

            Assert.Equal(15.0, (double)formulaValue.ToObject());
        }

        [Fact]
        public void CheckIntefaceSuccess()
        {
            var engine = new RecalcEngine();
            CheckThroughInterface(engine);
        }

        private void CheckThroughInterface(IPowerFxEngine engine)
        {
            var result = engine.Check(
               "3*2+x",
               RecordType.Empty().Add(
                   new NamedFormulaType("x", FormulaType.Number)));

            Assert.True(result.IsSuccess);
            Assert.True(result.ReturnType is NumberType);
            Assert.Single(result.TopLevelIdentifiers);
            Assert.Equal("x", result.TopLevelIdentifiers.First());
        }

        [Fact]
        public void RecalcEngineMutateConfig()
        {
            var config = new PowerFxConfig(null);
            config.SymbolTable.AddFunction(BuiltinFunctionsCore.Blank);

            var recalcEngine = new Engine(config)
            {
                SupportedFunctions = new SymbolTable() // clear builtins
            };

            var func = BuiltinFunctionsCore.AsType; // Function not already in engine
            Assert.DoesNotContain(func, recalcEngine.Functions); // didn't get auto-added by engine.

            // We can mutate config after engine is created.
            var optionSet = new OptionSet("foo", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() { { "one key", "one value" } }));
            config.SymbolTable.AddFunction(func);
            config.SymbolTable.AddEntity(optionSet);

            Assert.True(config.TryGetVariable(new DName("foo"), out _));
            Assert.Contains(func, recalcEngine.Functions); // function was added to the config.

            Assert.DoesNotContain(BuiltinFunctionsCore.Abs, recalcEngine.Functions);
        }

        [Fact]
        public void RecalcEngine_AddFunction_Twice()
        {
            var config = new PowerFxConfig(null);
            config.AddFunction(BuiltinFunctionsCore.Blank);

            Assert.Throws<ArgumentException>(() => config.AddFunction(BuiltinFunctionsCore.Blank));
        }

        [Fact]
        public void OptionSetChecks()
        {
            var config = new PowerFxConfig(null);

            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            config.AddOptionSet(optionSet);
            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check("OptionSet.Option1 <> OptionSet.Option2");
            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void OptionSetResultType()
        {
            var config = new PowerFxConfig(null);

            var optionSet = new OptionSet("FooOs", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            config.AddOptionSet(optionSet);
            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check("FooOs.Option1");
            Assert.True(checkResult.IsSuccess);
            var osvaluetype = Assert.IsType<OptionSetValueType>(checkResult.ReturnType);
            Assert.Equal("FooOs", osvaluetype.OptionSetName);
        }

        [Fact]
        public void OptionSetChecksWithMakeUniqueCollision()
        {
            var config = new PowerFxConfig(null);

            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "foo", "Option1" },
                    { "bar", "Option2" },
                    { "baz", "foo" }
            }));

            config.AddEntity(optionSet, new DName("SomeDisplayName"));
            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check("SomeDisplayName.Option1 <> SomeDisplayName.'foo (baz)'");
            Assert.True(checkResult.IsSuccess);
        }

        [Fact]
        public void EmptyEnumStoreTest()
        {
            var config = PowerFxConfig.BuildWithEnumStore(null, new EnumStoreBuilder());

            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check("SortOrder.Ascending");
            Assert.True(checkResult.IsSuccess);
            Assert.IsType<StringType>(checkResult.ReturnType);
        }

        [Fact]
        public async void MaxRecursionDepthTest()
        {
            var config = new PowerFxConfig(null)
            {
                MaxCallDepth = 5
            };
            var recalcEngine = new RecalcEngine(config);
            Assert.IsType<ErrorValue>(recalcEngine.Eval("Abs(Abs(Abs(Abs(Abs(Abs(1))))))"));
            Assert.IsType<NumberValue>(recalcEngine.Eval("Abs(Abs(Abs(Abs(Abs(1)))))"));
            Assert.IsType<NumberValue>(recalcEngine.Eval(
                @"Sum(
                Sum(Sum(1),1),
                Sum(Sum(1),1),
                Sum(Sum(1),1)
                )"));
        }

        [Fact]
        public void UDFRecursionLimitTest()
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig(null));
            recalcEngine.DefineFunctions("Foo(x: Number): Number = Foo(x);");
            Assert.IsType<ErrorValue>(recalcEngine.Eval("Foo(1)"));
        }

        [Fact]
        public void UDFRecursionWorkingTest()
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig(null));
            recalcEngine.DefineFunctions("Foo(x: Number): Number = If(x = 1, 1, If(Mod(x, 2) = 0, Foo(x/2), Foo(x*3 + 1)));");
            Assert.Equal(1.0, recalcEngine.Eval("Foo(5)").ToObject());
        }

        [Fact]
        public void IndirectRecursionTest()
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig(null)
            {
                MaxCallDepth = 80
            });
            recalcEngine.DefineFunctions(
                "A(x: Number): Number = If(Mod(x, 2) = 0, B(x/2), B(x));" +
                "B(x: Number): Number = If(Mod(x, 3) = 0, C(x/3), C(x));" +
                "C(x: Number): Number = If(Mod(x, 5) = 0, D(x/5), D(x));" +
                "D(x: Number): Number { If(Mod(x, 7) = 0, F(x/7), F(x)) };" +
                "F(x: Number): Number { If(x = 1, 1, A(x+1)) };");
            Assert.Equal(1.0, recalcEngine.Eval("A(12654)").ToObject());
        }

        [Fact]
        public void DoubleDefinitionTest()
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig(null));
            Assert.Throws<InvalidOperationException>(() => recalcEngine.DefineFunctions("Foo(): Number = 10; Foo(x: Number): String = \"hi\";"));
        }

        [Fact]
        public void TestNumberBinding()
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig(null));
            Assert.True(recalcEngine.DefineFunctions("Foo(): String = 10;").Errors.Any());
        }

        [Fact]
        public void TestWithTimeZoneInfo()
        {
            // CultureInfo not set in PowerFxConfig as we use Symbols
            var pfxConfig = new PowerFxConfig();
            var recalcEngine = new RecalcEngine(pfxConfig);
            var symbols = new RuntimeConfig();

            // 10/30/22 is the date where DST applies in France (https://www.timeanddate.com/time/change/france/paris)
            // So adding 2 hours to 1:34am will result in 2:34am
            var frTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            symbols.SetTimeZone(frTimeZone);

            var jaCulture = new CultureInfo("ja-JP");
            symbols.SetCulture(jaCulture);

            Assert.Same(frTimeZone, symbols.GetService<TimeZoneInfo>());
            Assert.Same(jaCulture, symbols.GetService<CultureInfo>());

            var fv = recalcEngine.EvalAsync(
                @"Text(DateAdd(DateTimeValue(""dimanche 30 octobre 2022 01:34:03"", ""fr-FR""), ""2"", ""hours""), ""dddd, MMMM dd, yyyy hh:mm:ss"")",
                CancellationToken.None,
                runtimeConfig: symbols).Result;

            Assert.NotNull(fv);
            Assert.IsType<StringValue>(fv);

            // Then we convert the result to Japanese date/time format (English equivalent: "Sunday, October 30, 2022 02:34:03")
            Assert.Equal("日曜日, 10月 30, 2022 02:34:03", fv.ToObject());
        }

        [Fact]
        public void TestMultiReturn()
        {
            var recalcEngine = new RecalcEngine(new PowerFxConfig(null));
            var str = "Foo(x: Number): Number { 1+1; 2+2; };";
            recalcEngine.DefineFunctions(str);
            Assert.Equal(4.0, recalcEngine.Eval("Foo(1)", null, new ParserOptions { AllowsSideEffects = true }).ToObject());
        }

        [Fact]
        public void FunctionServices()
        {
            var engine = new RecalcEngine();
            var values = new RuntimeConfig();
            values.AddService<IRandomService>(new TestRandService());

            // Rand 
            var result = engine.EvalAsync("Rand()", CancellationToken.None, runtimeConfig: values).Result;
            Assert.Equal(0.5, result.ToObject());

            // 1 service can impact multiple functions. 
            // It also doesn't replace the function, so existing function logic (errors, range checks, etc) still is used. 
            // RandBetween maps 0.5 to 6. 
            result = engine.EvalAsync("RandBetween(1,10)", CancellationToken.None, runtimeConfig: values).Result;
            Assert.Equal(6.0, result.ToObject());
        }

        [Fact]
        public async Task FunctionServicesHostBug()
        {
            // Need to protect against bogus values from a poorly implemented service.
            // These are exceptions, not ErrorValues, since it's a host bug. 
            var engine = new RecalcEngine();
            var values = new RuntimeConfig();

            // Host bug, service should be 0...1, this is out of range. 
            var buggyService = new TestRandService { _value = 9999 };

            values.AddService<IRandomService>(buggyService);

            try
            {
                await engine.EvalAsync("Rand()", CancellationToken.None, runtimeConfig: values);
                Assert.False(true); // should have thrown on illegal IRandomService service.
            }
            catch (InvalidOperationException e)
            {
                var name = typeof(TestRandService).FullName;
                Assert.Equal($"IRandomService ({name}) returned an illegal value 9999. Must be between 0 and 1", e.Message);
            }
        }

        [Fact]
        public async Task ExecutingWithRemovedVarFails()
        {
            var symTable = new SymbolTable();
            var slot = symTable.AddVariable("x", FormulaType.Number);

            var engine = new RecalcEngine();
            var result = engine.Check("x+1", symbolTable: symTable);
            Assert.True(result.IsSuccess);

            var eval = result.GetEvaluator();
            var symValues = symTable.CreateValues();
            symValues.Set(slot, FormulaValue.New(10));

            var result1 = await eval.EvalAsync(CancellationToken.None, symValues);
            Assert.Equal(11.0, result1.ToObject());

            // Adding a variable is ok. 
            var slotY = symTable.AddVariable("y", FormulaType.Number);
            result1 = await eval.EvalAsync(CancellationToken.None, symValues);
            Assert.Equal(11.0, result1.ToObject());

            // Executing an existing IR fails if it uses a deleted variable.
            symTable.RemoveVariable("x");
            await Assert.ThrowsAsync<InvalidOperationException>(() => eval.EvalAsync(CancellationToken.None, symValues));

            // Even re-adding with same type still fails. 
            // (somebody could have re-added with a different type)
            var slot2 = symTable.AddVariable("x", FormulaType.Number);
            symValues.Set(slot2, FormulaValue.New(20));

            await Assert.ThrowsAsync<InvalidOperationException>(() => eval.EvalAsync(CancellationToken.None, symValues));
        }

        // execute w/ missing var (never adding to SymValues)
        [Fact]
        public async Task ExecutingWithMissingVar()
        {
            var engine = new Engine(new PowerFxConfig());

            var recordType = RecordType.Empty()
                .Add("x", FormulaType.Number)
                .Add("y", FormulaType.Number);
                        
            var result = engine.Check("x+y", recordType);
            var eval = result.GetEvaluator();

            var recordXY = RecordValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(10)),
                new NamedValue("y", FormulaValue.New(100)));

            var result2 = eval.Eval(recordXY);
            Assert.Equal(110.0, result2.ToObject());

            // Missing y , treated as blank (0)
            var recordX = RecordValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(10)));
            result2 = eval.Eval(recordX);
            Assert.Equal(10.0, result2.ToObject());
        }

        [Fact]
        public void GetVariableRecalcEngine()
        {
            var config = new PowerFxConfig();

            var engine = new RecalcEngine(config);
            engine.UpdateVariable("A", FormulaValue.New(0));

            Assert.True(engine.TryGetVariableType("A", out var type));
            Assert.Equal(FormulaType.Number, type);

            Assert.False(engine.TryGetVariableType("Invalid", out type));
            Assert.Equal(default, type);

            engine.DeleteFormula("A");
            Assert.False(engine.TryGetVariableType("A", out type));
            Assert.Equal(default, type);
        }

        private class TestRandService : IRandomService
        {
            public double _value = 0.5;

            // Returns between 0 and 1. 
            public double NextDouble()
            {
                return _value;
            }
        }

        #region Test

        private readonly StringBuilder _updates = new StringBuilder();

        private void AssertUpdate(string expected)
        {
            Assert.Equal(expected, _updates.ToString());
            _updates.Clear();
        }

        private void OnUpdate(string name, FormulaValue newValue)
        {
            var str = newValue.ToObject()?.ToString();

            _updates.Append($"{name}-->{str};");
        }
        #endregion
    }
}
