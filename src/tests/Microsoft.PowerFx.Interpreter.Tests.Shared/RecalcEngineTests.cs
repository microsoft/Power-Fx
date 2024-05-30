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
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
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
                $"{ns}.{nameof(Governor)}",
                $"{ns}.{nameof(ReflectionFunction)}",
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
#pragma warning disable CS0618 // Type or member is obsolete
                $"{nsType}.{nameof(QueryableTableValue)}",
#pragma warning restore CS0618 // Type or member is obsolete
                $"{ns}.InterpreterConfigException",
                $"{ns}.Interpreter.{nameof(NotDelegableException)}",
                $"{ns}.Interpreter.{nameof(CustomFunctionErrorException)}",
                $"{ns}.{nameof(TypeCoercionProvider)}",             

                // Services for functions. 
                $"{ns}.Functions.IRandomService",
                $"{ns}.Functions.IClockService"                
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

            Assert.Equal(17m, ((DecimalValue)result).Value);
        }

        [Fact]
        public void EvalWithoutParse()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("x", 2.0);

            var check = new CheckResult(engine)
                .SetText("x*3")
                .SetBindingInfo();

            // Call Evaluator directly.
            // Ensure it also pulls engine's symbols. 
            var run = check.GetEvaluator();

            var result = run.Eval();
            Assert.Equal(2.0 * 3, result.ToObject());
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
            engine.UpdateVariable("A", 15.0);
            engine.SetFormula("B", "A*2", OnUpdate);
            AssertUpdate("B-->30;");

            engine.UpdateVariable("A", 20.0);
            AssertUpdate("B-->40;");

            // Ensure we can update to null. 
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.Number));
            AssertUpdate("B-->0;");
        }

        [Fact]
        public void BasicRecalcDecimal()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", 15m);
            engine.SetFormula("B", "A*2", OnUpdate);
            AssertUpdate("B-->30;");

            engine.UpdateVariable("A", 20m);
            AssertUpdate("B-->40;");

            // Ensure we can update to null. 
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.Decimal));
            AssertUpdate("B-->0;");
        }

        [Fact]
        public void BasicRecalcString()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", "abcdef");
            engine.SetFormula("B", "Mid(A,3,2)", OnUpdate);
            engine.SetFormula("C", "Len(A)", OnUpdate);
            AssertUpdate("B-->cd;C-->6;");

            engine.UpdateVariable("A", "hello");
            AssertUpdate("B-->ll;C-->5;");

            // Ensure we can update to null. 
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.String));
            AssertUpdate("B-->;C-->0;");
        }

        [Fact]
        public void BasicRecalcBoolean()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", true);
            engine.SetFormula("B", "Not(A)", OnUpdate);
            engine.SetFormula("C", "A Or false", OnUpdate);
            AssertUpdate("B-->False;C-->True;");

            engine.UpdateVariable("A", false);
            AssertUpdate("B-->True;C-->False;");

            // Ensure we can update to null.
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.Boolean));
            AssertUpdate("B-->True;C-->False;");
        }

        [Fact]
        public void BasicRecalcGuid()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", new Guid("0f8fad5b-D9CB-469f-a165-70867728950E"));
            engine.SetFormula("B", "A", OnUpdate);
            AssertUpdate("B-->0f8fad5b-d9cb-469f-a165-70867728950e;");

            engine.UpdateVariable("A", new Guid("f9168c5e-CEB2-4FAA-b6bf-329bf39fa1e4"));
            AssertUpdate("B-->f9168c5e-ceb2-4faa-b6bf-329bf39fa1e4;");

            // Ensure we can update to null. 
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.Guid));
            AssertUpdate("B-->;");
        }

        [Fact]
        public void BasicRecalcDateTime()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", new DateTime(2023, 09, 06, 03, 12, 45));
            engine.SetFormula("B", "Hour(DateAdd(A,20,TimeUnit.Minutes))", OnUpdate);
            engine.SetFormula("C", "Minute(DateAdd(A,20,TimeUnit.Minutes))", OnUpdate);
            AssertUpdate("B-->3;C-->32;");

            engine.UpdateVariable("A", new DateTime(2023, 09, 06, 12, 45, 45));
            AssertUpdate("B-->13;C-->5;");

            // Ensure we can update to null. 
            // null is treated as 0 or DateTime(1899,12,30,0,0,0,0)
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.DateTime));
            AssertUpdate("B-->0;C-->20;");
        }

        [Fact]
        public void BasicRecalcTime()
        {
            var engine = new RecalcEngine();
            engine.UpdateVariable("A", new TimeSpan(03, 12, 45));
            engine.SetFormula("B", "Hour(DateAdd(A,20,TimeUnit.Minutes))", OnUpdate);
            engine.SetFormula("C", "Minute(DateAdd(A,20,TimeUnit.Minutes))", OnUpdate);
            AssertUpdate("B-->3;C-->32;");

            engine.UpdateVariable("A", new TimeSpan(12, 45, 45));
            AssertUpdate("B-->13;C-->5;");

            // Ensure we can update to null. 
            // null is treated as 0 or Time(0,0,0,0)
            engine.UpdateVariable("A", FormulaValue.NewBlank(FormulaType.Time));
            AssertUpdate("B-->0;C-->20;");
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

            engine.UpdateVariable("A", 1m);
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
        public void UserDefinitionOnUpdateTest()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("A", 1m);
            engine.AddUserDefinitions("B=A*2;C=A*B;", onUpdate: OnUpdate);
            AssertUpdate("B-->2;C-->2;");

            // Can't set formulas, they're read only 
            var check = engine.Check("Set(B, 12)");
            Assert.False(check.IsSuccess);

            // Set() function triggers recalc chain. 
            engine.Eval("Set(A,10)", options: _opts);
            AssertUpdate("B-->20;C-->200;");

            // Compare Before/After set within an expression.
            // Before (A,B) = 10,20 
            // After  (A,B) = 3,6
            var result = engine.Eval("With({x:A, y:B}, Set(A,3); x & y & A & B)", options: _opts);
            Assert.Equal("102036", result.ToObject());
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

        [Theory]
        [InlineData(
            "func1(x:Number/*comment*/): Number = x * 10;\nfunc2(x:Number): Number = y1 * 10;",
            null,
            true)]
        [InlineData(
            "foo(x:Number):Number = If(x=0,foo(1),If(x=1,foo(2),If(x=2,Float(2))));",
            "foo(Float(0))",
            false,
            2.0)]
        [InlineData(
            "foo():Blank = foo();",
            "foo()",
            true)]
        [InlineData(
            "Add(x: Number, y:Number): Number = x + y; Foo(x: Number): Number = Abs(x);",
            "Add(10, Foo(-10))",
            false,
            20.0)]
        [InlineData(
            "Add(x: Number, y:Number): Number = x + y; Foo(x: Number): Number = Abs(x);",
            "Add(1 , Add(1 , Add(1 , Add(1 , Add(1 , Foo(-1))))))",
            false,
            6.0)]
        [InlineData(
            "TriplePowerSum(x: Number, y:Number, z:Number): Number = Power(2,x) + Power(2,y) + Power(2,z);",
            "TriplePowerSum(1 , 2, 3)",
            false,
            14.0)]

        // Recursive calls are not allowed
        [InlineData(
            "hailstone(x:Number):Number = If(Not(x = 1), If(Mod(x, 2)=0, hailstone(x/2), hailstone(3*x+1)), x);",
            "hailstone(Float(192))",
            true)]
        [InlineData(
            "odd(number:Number):Boolean = If(number = 0, false, even(Abs(number)-1)); even(number:Number):Boolean = If(number = 0, true, odd(Abs(number)-1));",
            "odd(17)",
            true)]
        [InlineData(
            "odd(number:Number):Boolean = If(number = 0, false, even(Abs(number)-1)); even(number:Number):Boolean = If(number = 0, true, odd(Abs(number)-1));",
            "even(17)",
            true)]
        [InlineData(
            "odd(number:Number):Boolean = If(number = 0, false, even(If(number<0,-number,number)-1)); even(number:Decimal):Boolean = If(number = 0, true, odd(If(number<0,-number,number)-1));",
            "odd(17)",
            true)]
        [InlineData(
            "odd(number:Number):Boolean = If(number = 0, false, even(If(number<0,-number,number)-1)); even(number:Decimal):Boolean = If(number = 0, true, odd(If(number<0,-number,number)-1));",
            "even(17)",
            true)]

        // Redefinition is not allowed
        [InlineData(
            "foo():Blank = foo(); foo():Number = x + 1;", 
            null,
            true)]

        // Syntax error
        [InlineData(
            "foo():Blank = x[",
            null,
            true)]

        // Incorrect parameters
        [InlineData(
            "foo(x:Number):Number = x + 1;",
            "foo(False)",
            true)]
        [InlineData(
            "foo(x:Number):Number = x + 1;",
            "foo(Table( { Value: \"Strawberry\" }, { Value: \"Vanilla\" } ))",
            true)]
        [InlineData(
            "foo(x:Number):Number = x + 1;",
            "foo(Float(1))",
            false,
            2.0)]

        public void UserDefinedFunctionTest(string udfExpression, string expression, bool expectedError, double expected = 0)
        {
            var config = new PowerFxConfig()
            {
                MaxCallDepth = 100
            };
            var recalcEngine = new RecalcEngine(config);

            try
            {
                recalcEngine.AddUserDefinedFunction(udfExpression, CultureInfo.InvariantCulture);

                var check = recalcEngine.Check(expression);

                Assert.Equal(check.IsSuccess, !expectedError);

                var result = recalcEngine.Eval(expression);
                var fvExpected = FormulaValue.New(expected);

                Assert.Equal(fvExpected.AsDecimal(), result.AsDecimal());
            }
            catch (Exception ex)
            {
                Assert.True(expectedError, ex.Message);
            }
        }

        [Theory]
        [InlineData("foo(x: Number, y:Number):Number = x + y;", "foo(1,2)", 3.0)]
        [InlineData("foo(x: Number, y:Number):Number = x - Abs(y);", "foo(myArg,1)", 9.0)]
        public void UserDefinedFunctionSymbolTableTest(string script, string expression, double expected)
        {
            var engine = new RecalcEngine();
            var symbolTable = new SymbolTable();

            engine.UpdateVariable("myArg", FormulaValue.New(10));

            symbolTable.AddUserDefinedFunction(script, CultureInfo.InvariantCulture, engine.SupportedFunctions, engine.PrimitiveTypes);

            var check = engine.Check(expression, symbolTable: symbolTable);
            var result = check.GetEvaluator().Eval();
            var fvExpected = FormulaValue.New(expected);

            Assert.Equal(fvExpected.AsDecimal(), result.AsDecimal());
        }

        [Theory]
        [InlineData("foo(x:Number):Number = x + missingArg1 - missingArg2;")]
        [InlineData("foo(x:Number):Number = x + ;")]
        public void DefinedFunctionsErrorsTest(string script)
        {
            var engine = new RecalcEngine();

            Assert.Throws<InvalidOperationException>(() => engine.AddUserDefinedFunction(script, CultureInfo.InvariantCulture));
        }

        // Overloads and conflict 
        [Theory]
        [InlineData("foo(Text:Number):Number = Text;", 1)] // param name conflicts with type name
        [InlineData("foo(K1:Number):Number = K1;", 1)] // param takes precedence
        [InlineData("foo(param:Number):Number = K1;", 9999)] // param takes precedence
        public void FunctionPrecedenceTest(string script, double expected)
        {
            SymbolTable st = new SymbolTable { DebugName = "Extras" };
            st.AddConstant("K1", FormulaValue.New(9999));            

            var engine = new RecalcEngine();
            engine.AddUserDefinedFunction(script, symbolTable: st);

            var check = engine.Check("foo(1)");
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.Number, check.ReturnType);

            var result = check.GetEvaluator().Eval();            
            Assert.Equal(expected, result.AsDouble());
        }

        [Theory]

        // Return value with side effectful UDF
        [InlineData(
            "F1(x:Number) : Number = { Set(a, x); a+1; };",
            "F1(123)",
            false,
            null,
            124)]

        // Mismatch return value with side effectful UDF
        [InlineData(
            "F1(x:Number) : Boolean = { Set(a, x); Today(); };",
            null,
            true,
            "AddUserDefinedFunction",
            0)]

        public void ImperativeUserDefinedFunctionTest(string udfExpression, string expression, bool expectedError, string expectedMethodFailure, double expected)
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var recalcEngine = new RecalcEngine(config);
            recalcEngine.UpdateVariable("a", 1m);

            try
            {
                recalcEngine.AddUserDefinedFunction(udfExpression, CultureInfo.InvariantCulture, symbolTable: recalcEngine.EngineSymbols, allowSideEffects: true);

                var result = recalcEngine.Eval(expression, options: _opts);
                var fvExpected = FormulaValue.New(expected);

                Assert.Equal(fvExpected.AsDecimal(), result.AsDecimal());
                Assert.False(expectedError);
            }
            catch (Exception ex)
            {
                Assert.True(expectedError, ex.Message);
                Assert.Contains(expectedMethodFailure, ex.StackTrace);
            }
        }

        // Binding to inner functions does not impact outer functions. 
        [Fact]
        public void FunctionInner()
        {
            // Inner table 
            SymbolTable stInner = SymbolTable.WithPrimitiveTypes();
            stInner.AddUserDefinedFunction("Func1() : Text = \"inner\";");

            SymbolTable st = SymbolTable.WithPrimitiveTypes();
            st.AddUserDefinedFunction("Func2() : Text = Func1() & \"2\";", symbolTable: stInner);

            var engine = new RecalcEngine();
            engine.AddUserDefinedFunction("Func1() : Text = \"Outer\";", symbolTable: st);

            // Func1() here should bind to the top-level "outer" one, not the "inner" one.
            var result = engine.EvalAsync("Func1() & Func2()", default, symbolTable: st).Result;
            var str = ((StringValue)result).Value;
            Assert.Equal("Outerinner2", str);
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
                new NamedValue("F2", FormulaValue.New(6.0))));

            engine.SetFormula("A", "R.F2 + 3 + R.F1", OnUpdate);
            AssertUpdate("A-->9;");

            engine.UpdateVariable("R", FormulaValue.NewRecordFromFields(
                new NamedValue("F1", FormulaValue.New(2.0)),
                new NamedValue("F2", FormulaValue.New(7.0))));
            AssertUpdate("A-->12;");
        }

        [Fact]
        public void ChangeRecord_Decimal()
        {
            var engine = new RecalcEngine();

            engine.UpdateVariable("R", FormulaValue.NewRecordFromFields(
                new NamedValue("F1", FormulaValue.NewBlank(FormulaType.Decimal)),
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
            var config = new PowerFxConfig();
            config.EnableJsonFunctions();

            var engine1 = new Engine(config);

            // Pick a function in core but not implemented in interpreter.
            var nyiFunc = BuiltinFunctionsCore.ISOWeekNum;

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Contains(nyiFunc, engine1.Functions.Functions);
#pragma warning restore CS0618 // Type or member is obsolete

            // RecalcEngine will add the interpreter's functions. 
            var engine2 = new RecalcEngine(config);

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.DoesNotContain(nyiFunc, engine2.Functions.Functions);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.True(engine2.FunctionCount > 100);

            // Spot check some known functions
            Assert.NotEmpty(engine2.Functions.WithName("Cos"));
            Assert.NotEmpty(engine2.Functions.WithName("ParseJSON"));
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
            var config = new PowerFxConfig(Features.None);
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
            var result = engine.Check("First([1,2,3]).foo");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 14-18: Name isn't valid. 'foo' isn't recognized", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckDottedBindErrorForSingleColumnAccess()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("[1,2,3].foo");
            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 7-11: Deprecated use of '.'. Please use the 'ShowColumns' function instead.", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckDottedBindError2()
        {
            var engine = new RecalcEngine();
            var result = engine.Check("First([]).Value");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            Assert.StartsWith("Error 9-15: Name isn't valid. 'Value' isn't recognized", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckBindEnum()
        {
            var engine = new RecalcEngine(new PowerFxConfig(Features.None));
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
            Assert.True(result.ReturnType is NumberType);
            Assert.Single(result.TopLevelIdentifiers);
            Assert.Equal("x", result.TopLevelIdentifiers.First());

            // Test evaluation of parsed expression
            var recordValue = FormulaValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(5.0)));
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
            Assert.True(result.ReturnType is NumberType);

            // Test evaluation of parsed expression
            var recordValue = FormulaValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(5.0)));

            var formulaValue = result.GetEvaluator().Eval(recordValue);

            Assert.Equal(15.0, (double)formulaValue.ToObject());
        }

        [Fact]
        public void RecalcEngineMutateConfig()
        {
            var config = new PowerFxConfig();
            config.SymbolTable.AddFunction(BuiltinFunctionsCore.Blank);

            var recalcEngine = new Engine(config)
            {
                SupportedFunctions = new SymbolTable() // clear builtins
            };

            var func = BuiltinFunctionsCore.AsType; // Function not already in engine
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.DoesNotContain(func, recalcEngine.Functions.Functions); // didn't get auto-added by engine.
#pragma warning restore CS0618 // Type or member is obsolete

            // We can mutate config after engine is created.
            var optionSet = new OptionSet("foo", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() { { "one key", "one value" } }));
            config.SymbolTable.AddFunction(func);
            config.SymbolTable.AddEntity(optionSet);

            Assert.True(config.TryGetVariable(new DName("foo"), out _));
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Contains(func, recalcEngine.Functions.Functions); // function was added to the config.
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.DoesNotContain(BuiltinFunctionsCore.Abs, recalcEngine.Functions.Functions);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void RecalcEngine_AddFunction_Twice()
        {
            var config = new PowerFxConfig();
            config.AddFunction(BuiltinFunctionsCore.Blank);

            Assert.Throws<ArgumentException>(() => config.AddFunction(BuiltinFunctionsCore.Blank));
        }

        [Fact]
        public void RecalcEngine_FunctionOrdering1()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.AddFunction(new TestFunctionMultiply());
            config.AddFunction(new TestFunctionSubstract());

            var engine = new RecalcEngine(config);
            var result = engine.Eval("Func(7, 11)");

            Assert.IsType<NumberValue>(result);

            // Multiply function is first and a valid overload so that's the one we use as coercion is valid for this one
            Assert.Equal(77.0, (result as NumberValue).Value);
        }

        [Fact]
        public void RecalcEngine_FunctionOrdering2()
        {
            var config = new PowerFxConfig(Features.PowerFxV1);
            config.AddFunction(new TestFunctionSubstract());
            config.AddFunction(new TestFunctionMultiply());

            var engine = new RecalcEngine(config);
            var result = engine.Eval("Func(7, 11)");

            Assert.IsType<NumberValue>(result);

            // Substract function is first and a valid overload so that's the one we use as coercion is valid for this one
            Assert.Equal(-4.0, (result as NumberValue).Value);
        }

        private class TestFunctionMultiply : CustomTexlFunction
        {
            public override bool IsSelfContained => true;

            public TestFunctionMultiply()
                : base(DPath.Root, "Func", FunctionCategories.MathAndStat, DType.Number, null, DType.Number, DType.String)
            {
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield return new[] { TexlStrings.IsBlankArg1 };
            }

            public override Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, FormulaValue[] args, CancellationToken cancellationToken)
            {
                var arg0 = args[0] as NumberValue;
                var arg1 = args[1] as StringValue;

                return Task.FromResult<FormulaValue>(NumberValue.New(arg0.Value * double.Parse(arg1.Value)));
            }
        }

        private class TestFunctionSubstract : CustomTexlFunction
        {
            public override bool IsSelfContained => true;

            public TestFunctionSubstract()
                : base(DPath.Root, "Func", FunctionCategories.MathAndStat, DType.Number, null, DType.String, DType.Number)
            {
            }

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield return new[] { TexlStrings.IsBlankArg1 };
            }

            public override Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, FormulaValue[] args, CancellationToken cancellationToken)
            {
                var arg0 = args[0] as StringValue;
                var arg1 = args[1] as NumberValue;

                return Task.FromResult<FormulaValue>(NumberValue.New(double.Parse(arg0.Value) - arg1.Value));
            }
        }

        [Fact]
        public void OptionSetChecks()
        {
            var config = new PowerFxConfig();

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

        [Theory]

        // Text() returns the display name of the input option set value
        [InlineData("Text(OptionSet.option_1)", "Option1")]
        [InlineData("Text(OptionSet.Option1)", "Option1")]
        [InlineData("Text(Option1)", "Option1")]
        [InlineData("Text(If(1<0, Option1))", null)]

        // OptionSetInfo() returns the logical name of the input option set value
        [InlineData("OptionSetInfo(OptionSet.option_1)", "option_1")]
        [InlineData("OptionSetInfo(OptionSet.Option1)", "option_1")]
        [InlineData("OptionSetInfo(Option1)", "option_1")]
        [InlineData("OptionSetInfo(If(1<0, Option1))", "")]
        public async void OptionSetInfoTests(string expression, string expected)
        {
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            optionSet.TryGetValue(new DName("option_1"), out var option1);

            var symbol = new SymbolTable();
            var option1Solt = symbol.AddVariable("Option1", FormulaType.OptionSetValue, null);
            var symValues = new SymbolValues(symbol);
            symValues.Set(option1Solt, option1);

            var config = new PowerFxConfig() { SymbolTable = symbol };
#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableOptionSetInfo();
#pragma warning restore CS0618 // Type or member is obsolete
            config.AddOptionSet(optionSet);
            var recalcEngine = new RecalcEngine(config);

            var result = await recalcEngine.EvalAsync(expression, CancellationToken.None, symValues).ConfigureAwait(false);
            Assert.Equal(expected, result.ToObject());
        }

        [Theory]
        [InlineData("Text(OptionSet)")]

        [InlineData("OptionSetInfo(OptionSet)")]
        [InlineData("OptionSetInfo(\"test\")")]
        [InlineData("OptionSetInfo(1)")]
        [InlineData("OptionSetInfo(true)")]
        [InlineData("OptionSetInfo(Color.Red)")]
        public async Task OptionSetInfoNegativeTest(string expression)
        {
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            var config = new PowerFxConfig(Features.None);
            config.AddOptionSet(optionSet);
            var recalcEngine = new RecalcEngine(config);
            var checkResult = recalcEngine.Check(expression, RecordType.Empty());
            Assert.False(checkResult.IsSuccess);
        }

        [Fact]
        public void OptionSetResultType()
        {
            var config = new PowerFxConfig();

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
            var config = new PowerFxConfig();

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
            var config = PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder());

            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check("SortOrder.Ascending");
            Assert.True(checkResult.IsSuccess);
            Assert.IsType<StringType>(checkResult.ReturnType);
        }

        [Theory]
        [InlineData("Text(TestEnum.Choice1)", true)]
        [InlineData("\"Label: \" & TestEnum.Choice1", true)]
        [InlineData("Value(TestEnum.Choice1)", false)]
        [InlineData("TestEnum.Choice1 + 1", false)]
        [InlineData("Decimal(TestEnum.Choice1)", false)]
        [InlineData("Float(TestEnum.Choice1)", false)]
        [InlineData("Boolean(TestEnum.Choice1)", false)]
        [InlineData("Boolean([TestEnum.Choice1,TestEnum.Choice2])", false)]
        [InlineData("TestEnum.Choice1 And true", false)]
        [InlineData("ColorFade(TestEnum.Choice1,10%)", false)]
        [InlineData("ColorFade([TestEnum.Choice1,TestEnum.Choice2],10%)", false)]
        public void OptionSetBackingTextTests(string expression, bool valid)
        {
            var enumStoreBuilder = new EnumStoreBuilder();
            enumStoreBuilder.TestOnly_WithCustomEnum(new EnumSymbol(
                new DName("TestEnum"),
                DType.String,
                new Dictionary<string, object>()
                {
                    { "Choice1", "Choice_1" },
                    { "Choice2", "Choice_2" },
                }));
            var config = PowerFxConfig.BuildWithEnumStore(enumStoreBuilder, features: Features.PowerFxV1);
            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check(expression, RecordType.Empty());
            Assert.Equal(valid, checkResult.IsSuccess);
        }

        [Theory]
        [InlineData("Text(TestEnum.Choice1)", true)]
        [InlineData("\"Label: \" & TestEnum.Choice1", true)]
        [InlineData("Value(TestEnum.Choice1)", true)]
        [InlineData("TestEnum.Choice1 + 1", false)] // see https://github.com/microsoft/Power-Fx/issues/2229
        [InlineData("Decimal(TestEnum.Choice1)", true)]
        [InlineData("Float(TestEnum.Choice1)", true)]
        [InlineData("Boolean(TestEnum.Choice1)", false)]
        [InlineData("Boolean([TestEnum.Choice1,TestEnum.Choice2])", false)]
        [InlineData("TestEnum.Choice1 And true", false)]
        [InlineData("ColorFade(TestEnum.Choice1,10%)", false)]
        [InlineData("ColorFade([TestEnum.Choice1,TestEnum.Choice2],10%)", false)]
        public void OptionSetBackingNumberTests(string expression, bool valid)
        {
            var enumStoreBuilder = new EnumStoreBuilder();
            enumStoreBuilder.TestOnly_WithCustomEnum(new EnumSymbol(
                new DName("TestEnum"),
                DType.Number,
                new Dictionary<string, object>()
                {
                    { "Choice1", 1 },
                    { "Choice2", 2 },
                }));
            var config = PowerFxConfig.BuildWithEnumStore(enumStoreBuilder, features: Features.PowerFxV1);
            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check(expression, RecordType.Empty());
            Assert.Equal(valid, checkResult.IsSuccess);
        }

        [Theory]
        [InlineData("Text(TestEnum.Choice1)", true)]
        [InlineData("\"Label: \" & TestEnum.Choice1", true)]
        [InlineData("Value(TestEnum.Choice1)", false)]
        [InlineData("TestEnum.Choice1 + 1", false)]
        [InlineData("Decimal(TestEnum.Choice1)", false)]
        [InlineData("Float(TestEnum.Choice1)", false)]
        [InlineData("Boolean(TestEnum.Choice1)", true)]
        [InlineData("Boolean([TestEnum.Choice1,TestEnum.Choice2])", true)]
        [InlineData("TestEnum.Choice1 And true", true)]
        [InlineData("ColorFade(TestEnum.Choice1,10%)", false)]
        [InlineData("ColorFade([TestEnum.Choice1,TestEnum.Choice2],10%)", false)]
        public void OptionSetBackingBooleanTests(string expression, bool valid)
        {
            var enumStoreBuilder = new EnumStoreBuilder();
            enumStoreBuilder.TestOnly_WithCustomEnum(new EnumSymbol(
                new DName("TestEnum"),
                DType.Boolean,
                new Dictionary<string, object>()
                {
                    { "Choice1", true },
                    { "Choice2", false },
                },
                canCoerceToBackingKind: true));
            var config = PowerFxConfig.BuildWithEnumStore(enumStoreBuilder, features: Features.PowerFxV1);
            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check(expression, RecordType.Empty());
            Assert.Equal(valid, checkResult.IsSuccess);
        }

        [Theory]
        [InlineData("Text(TestEnum.Choice1)", true)]
        [InlineData("\"Label: \" & TestEnum.Choice1", true)]
        [InlineData("Value(TestEnum.Choice1)", false)]
        [InlineData("TestEnum.Choice1 + 1", false)]
        [InlineData("Decimal(TestEnum.Choice1)", false)]
        [InlineData("Float(TestEnum.Choice1)", false)]
        [InlineData("Boolean(TestEnum.Choice1)", false)]
        [InlineData("Boolean([TestEnum.Choice1,TestEnum.Choice2])", false)]
        [InlineData("TestEnum.Choice1 And true", false)]
        [InlineData("ColorFade(TestEnum.Choice1,10%)", false)]
        [InlineData("ColorFade([TestEnum.Choice1,TestEnum.Choice2],10%)", false)]
        public void OptionSetBackingColorTests(string expression, bool valid)
        {
            var enumStoreBuilder = new EnumStoreBuilder();
            enumStoreBuilder.TestOnly_WithCustomEnum(new EnumSymbol(
                new DName("TestEnum"),
                DType.Color,
                new Dictionary<string, object>()
                {
                    { "Choice1", 0 },
                    { "Choice2", 255 },
                }));
            var config = PowerFxConfig.BuildWithEnumStore(enumStoreBuilder, features: Features.PowerFxV1);
            var recalcEngine = new RecalcEngine(config);

            var checkResult = recalcEngine.Check(expression, RecordType.Empty());
            Assert.Equal(valid, checkResult.IsSuccess);
        }

        [Fact]
        public void TestWithTimeZoneInfo()
        {
            // CultureInfo not set in PowerFxConfig as we use Symbols
            var pfxConfig = new PowerFxConfig(Features.None);
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
        public void FunctionServices()
        {
            var engine = new RecalcEngine();
            var values = new RuntimeConfig();
            values.SetRandom(new TestRandService());

            // Rand 
            var result = engine.EvalAsync("Rand()", CancellationToken.None, runtimeConfig: values).Result;
            Assert.Equal(0.5, result.ToObject());

            // 1 service can impact multiple functions. 
            // It also doesn't replace the function, so existing function logic (errors, range checks, etc) still is used. 
            // RandBetween maps 0.5 to 6. 
            result = engine.EvalAsync("RandBetween(1,10)", CancellationToken.None, runtimeConfig: values).Result;
            Assert.Equal(6.0m, result.ToObject());
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
                await engine.EvalAsync("Rand()", CancellationToken.None, runtimeConfig: values).ConfigureAwait(false);
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
            var slot = symTable.AddVariable("x", FormulaType.Number, null);

            var engine = new RecalcEngine();
            var result = engine.Check("x+1", symbolTable: symTable);
            Assert.True(result.IsSuccess);

            var eval = result.GetEvaluator();
            var symValues = symTable.CreateValues();
            symValues.Set(slot, FormulaValue.New(10.0));

            var result1 = await eval.EvalAsync(CancellationToken.None, symValues).ConfigureAwait(false);
            Assert.Equal(11.0, result1.ToObject());

            // Adding a variable is ok. 
            var slotY = symTable.AddVariable("y", FormulaType.Number, null);
            result1 = await eval.EvalAsync(CancellationToken.None, symValues).ConfigureAwait(false);
            Assert.Equal(11.0, result1.ToObject());

            // Executing an existing IR fails if it uses a deleted variable.
            symTable.RemoveVariable("x");
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await eval.EvalAsync(CancellationToken.None, symValues).ConfigureAwait(false)).ConfigureAwait(false);

            // Even re-adding with same type still fails. 
            // (somebody could have re-added with a different type)
            var slot2 = symTable.AddVariable("x", FormulaType.Number, null);
            symValues.Set(slot2, FormulaValue.New(20.0));

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await eval.EvalAsync(CancellationToken.None, symValues).ConfigureAwait(false)).ConfigureAwait(false);
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
                new NamedValue("x", FormulaValue.New(10.0)),
                new NamedValue("y", FormulaValue.New(100.0)));

            var result2 = eval.Eval(recordXY);
            Assert.Equal(110.0, result2.ToObject());

            // Missing y , treated as blank (0)
            var recordX = RecordValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(10.0)));
            result2 = eval.Eval(recordX);
            Assert.Equal(10.0, result2.ToObject());
        }

        [Theory]
        [InlineData("ThisRecord.Field2", "_field2")] // row scope, no conflcit
        [InlineData("Task", "_fieldTask")] // row scope wins the conflict, it's closer.         
        [InlineData("[@Task]", "_globalTask")] // global scope
        [InlineData("[@Task] & Task", "_globalTask_fieldTask")] // both in same expression
        [InlineData("[@Task] & ThisRecord.Task", "_globalTask_fieldTask")] // both, fully unambiguous. 
        [InlineData("With({Task : true}, Task)", true)] // With() wins, shadows rowscope. 
        [InlineData("With({Task : true}, ThisRecord.Task)", true)] // With() also has ThisRecord, shadows previous rowscope.
        [InlineData("With({Task : true}, [@Task])", "_globalTask")] // Globals.
        [InlineData("With({Task : true} As T2, Task)", "_fieldTask")] // As avoids the conflict. 
        [InlineData("With({Task : true} As T2, ThisRecord.Task)", "_fieldTask")] // As avoids the conflict. 
        [InlineData("With({Task : true} As T2, ThisRecord.Field2)", "_field2")] // As avoids the conflict. 

        // Errors
        [InlineData("[@Field2]")] // error, doesn't exist in global scope. 
        [InlineData("With({Task : true}, ThisRecord.Field2)")] // Error. ThisRecord doesn't union, it refers exclusively to With().
        public void DisambiguationTest(string expr, object expected = null)
        {
            var engine = new RecalcEngine();

            // Setup Global "Task", and RowScope with "Task" field.
            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("Task", FormulaValue.New("_fieldTask")),
                new NamedValue("Field2", FormulaValue.New("_field2")));

            var globals = new SymbolTable();
            var slot = globals.AddVariable("Task", FormulaType.String, null);

            var rowScope = ReadOnlySymbolTable.NewFromRecord(record.Type, allowThisRecord: true);

            // ensure rowScope is listed first since that should get higher priority 
            var symbols = ReadOnlySymbolTable.Compose(rowScope, globals);

            // Values 
            var rowValues = ReadOnlySymbolValues.NewFromRecord(rowScope, record);
            var globalValues = globals.CreateValues();
            globalValues.Set(slot, FormulaValue.New("_globalTask"));

            var runtimeConfig = new RuntimeConfig
            {
                Values = symbols.CreateValues(globalValues, rowValues)
            };

            var check = engine.Check(expr, symbolTable: symbols); // never throws

            if (expected == null)
            {
                Assert.False(check.IsSuccess);
                return;
            }

            var run = check.GetEvaluator();
            var result = run.Eval(runtimeConfig);

            Assert.Equal(expected, result.ToObject());
        }

        [Fact]
        public void GetVariableRecalcEngine()
        {
            var config = new PowerFxConfig();

            var engine = new RecalcEngine(config);
            engine.UpdateVariable("A", FormulaValue.New(0.0));

            Assert.True(engine.TryGetVariableType("A", out var type));
            Assert.Equal(FormulaType.Number, type);

            Assert.False(engine.TryGetVariableType("Invalid", out type));
            Assert.Equal(default, type);

            engine.DeleteFormula("A");
            Assert.False(engine.TryGetVariableType("A", out type));
            Assert.Equal(default, type);
        }

        [Fact]
        public void ComparisonWithMismatchedTypes()
        {
            foreach ((Features f, ErrorSeverity es) in new[]
            {
                (Features.PowerFxV1, ErrorSeverity.Severe),
                (Features.None, ErrorSeverity.Warning)
            })
            {
                var config = new PowerFxConfig(f);
                var engine = new RecalcEngine(config);

                CheckResult cr = engine.Check(@"If(2 = ""2"", 3, 4 )");
                ExpressionError firstError = cr.Errors.First();

                Assert.Equal(es, firstError.Severity);
                Assert.Equal("Incompatible types for comparison. These types can't be compared: Decimal, Text.", firstError.Message);
            }
        }

        [Fact]
        public void TryGetValueShouldNotThrowOnNonExistingValue()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            var success = engine.TryGetValue("Invalid", out var shouldBeNull);

            Assert.False(success);
            Assert.Null(shouldBeNull);
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

        [Fact]
        public void LookupBuiltinOptionSets()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            // Builtin enums are on engine.SupportedFunctionm
            var ok = engine.SupportedFunctions.TryGetSymbolType("Color", out var type);
            Assert.True(ok);

            ok = engine.GetCombinedEngineSymbols().TryGetSymbolType("Color", out type);
            Assert.True(ok);

            // Wrong type: https://github.com/microsoft/Power-Fx/issues/2342
        }

        [Theory]
        [InlineData(
            "Point = Type({x : Number, y : Number}); distance(a: Point, b: Point): Number = Sqrt(Power(b.x-a.x, 2) + Power(b.y-a.y, 2));",
            "distance({x: 0, y: 0}, {x: 0, y: 5})",
            true,
            5.0)]

        // Table types are accepted
        [InlineData(
            "People = Type([{Id:Number, Age: Number}]); countMinors(p: People): Number = CountRows(Filter(p, Age < 18));",
            "countMinors([{Id: 1, Age: 17}, {Id: 2, Age: 21}])",
            true,
            1.0)]
        [InlineData(
            "Numbers = Type([Number]); countEven(nums: Numbers): Number = CountRows(Filter(nums, Mod(Value, 2) = 0));",
            "countEven([1,2,3,4,5,6,7,8,9,10])",
            true,
            5.0)]

        // Type Aliases are allowed
        [InlineData(
            "CarYear = Type(Number); Car = Type({Model: Text, ModelYear: CarYear}); createCar(model:Number, year: Number): Car = {Model:model, ModelYear: year};",
            "createCar(\"Model Y\", 2024).ModelYear",
            true,
            2024.0)]

        // Type definitions order shouldn't matter
        [InlineData(
            "Person = Type({Id: IdType, Age: Number}); IdType = Type(Number); createUser(id:Number, a: Number): Person = {Id:id, Age: a};",
            "createUser(1, 42).Age",
            true,
            42.0)]

        // Functions accept record with more/less fields
        [InlineData(
            "People = Type([{Name: Text, Age: Number}]); countMinors(p: People): Number = CountRows(Filter(p, Age < 18));",
            "countMinors([{Name: \"Bob\", Age: 21, Title: \"Engineer\"}, {Name: \"Alice\", Age: 25, Title: \"Manager\"}])",
            true,
            0.0)]
        [InlineData(
            "Employee = Type({Name: Text, Age: Number, Title: Text}); getAge(e: Employee): Number = e.Age;",
            "getAge({Name: \"Bob\", Age: 21})",
            true,
            21.0)]
        [InlineData(
            @"Employee = Type({Name: Text, Age: Number, Title: Text}); Employees = Type([Employee]);  EmployeeNames = Type([{Name: Text}]); 
              getNames(e: Employees):EmployeeNames = ShowColumns(e, Name); 
              getNamesCount(e: EmployeeNames):Number = CountRows(getNames(e));",
            "getNamesCount([{Name: \"Jim\", Age:25}, {Name: \"Tony\", Age:42}])",
            true,
            2.0)]
        [InlineData(
            @"Employee = Type({Name: Text, Age: Number, Title: Text}); 
              getAge(e: Employee): Number = e.Age;
              hasNoAge(e: Employee): Number = IsBlank(getAge(e));",
            "hasNoAge({Name: \"Bob\", Title: \"CEO\"})",
            true,
            1.0)]

        // Types with UDF restricted primitive types resolve successfully 
        [InlineData(
            @"Patient = Type({DOB: DateTimeTZInd, Weight: Decimal, Dummy: None}); 
              Patients = Type([Patient]);
              Dummy():Number = CountRows([]);",
            "Dummy()",
            true,
            0.0)]

        // Aggregate types with restricted types are not allowed in UDF
        [InlineData(
            @"Patient = Type({DOB: DateTimeTZInd, Weight: Decimal, Dummy: None}); 
              Patients = Type([Patient]);
              getAnomaly(p: Patients): Patients = Filter(p, Weight < 0);",
            "",
            false)]

        [InlineData(
            @"Patient = Type({Name: Text, Details: {h: Number, w:Decimal}}); 
              getPatient(): Patient = {Name:""Alice"", Details: {h: 1, w: 2}};",
            "",
            false)]

        // Cycles not allowed
        [InlineData(
            "Z = Type([{a: {b: Z}}]);",
            "",
            false)]
        [InlineData(
            "X = Type(Y); Y = Type(X);",
            "",
            false)]
        [InlineData(
            "C = Type({x: Boolean, y: Date, f: B});B = Type({ x: A }); A = Type([C]);",
            "",
            false)]

        // Redeclaration not allowed
        [InlineData(
            "Number = Type(Text);",
            "",
            false)]
        [InlineData(
            "Point = Type({x : Number, y : Number}); Point = Type({x : Number, y : Number, z: Number})",
            "",
            false)]

        // UDFs with body errors should fail
        [InlineData(
            "S = Type({x:Text}); f():S = ({);",
            "",
            false)]

        // UDFs with Enitity Types should work in parameter and return types

        [InlineData(
            "f():TestEntity = Entity; g(e: TestEntity):Number = 1;",
            "g(f())",
            true,
            1.0)]

        public void UserDefinedTypeTest(string userDefinitions, string evalExpression, bool isValid, double expectedResult = 0)
        {
            var config = new PowerFxConfig();
            var recalcEngine = new RecalcEngine(config);
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false,
                AllowParseAsTypeLiteral = true
            };

            if (isValid)
            {
                var entityType = new Interpreter.Tests.DatabaseSimulationTests.TestEntityType(new Tests.BindingEngineTests.LazyRecursiveRecordType().ToTable()._type);
                var entityValue = new Interpreter.Tests.DatabaseSimulationTests.TestEntityValue(IRContext.NotInSource(entityType));
                recalcEngine._symbolTable.AddType(new DName("TestEntity"), entityType);
                recalcEngine._symbolValues.Add("Entity", entityValue);
                recalcEngine.AddUserDefinitions(userDefinitions, CultureInfo.InvariantCulture);
                Assert.Equal(expectedResult, recalcEngine.Eval(evalExpression, options: parserOptions).ToObject());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => recalcEngine.AddUserDefinitions(userDefinitions, CultureInfo.InvariantCulture));
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
