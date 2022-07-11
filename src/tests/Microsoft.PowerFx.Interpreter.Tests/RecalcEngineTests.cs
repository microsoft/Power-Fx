// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
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
                $"{ns}.{nameof(RecalcEngine)}",
                $"{ns}.{nameof(ReflectionFunction)}",
                $"{ns}.{nameof(RecalcEngineScope)}",
                $"{ns}.{nameof(PowerFxConfigExtensions)}",
                $"{ns}.{nameof(ITypeMarshallerProvider)}",
                $"{ns}.{nameof(ITypeMarshaller)}",
                $"{ns}.{nameof(IDynamicTypeMarshaller)}",
                $"{ns}.{nameof(ObjectMarshallerProvider)}",
                $"{ns}.{nameof(ObjectMarshaller)}",
                $"{ns}.{nameof(PrimitiveMarshallerProvider)}",
                $"{ns}.{nameof(PrimitiveTypeMarshaller)}",
                $"{ns}.{nameof(TableMarshallerProvider)}",
                $"{ns}.{nameof(TypeMarshallerCache)}",
                $"{ns}.{nameof(TypeMarshallerCacheExtensions)}",   
                $"{ns}.{nameof(MarshalledObjectIdentity)}",
                $"{nsType}.{nameof(ObjectRecordValue)}"
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
            Assert.False(engine.Formulas.TryGetValue("D", out var retD));

            engine.DeleteFormula("C");
            Assert.False(engine.Formulas.TryGetValue("C", out var retC));

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
            Assert.Throws<NotSupportedException>(() =>
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
            var config = new PowerFxConfig();

            // Pick a function in core but not implemented in interpreter.
            var nyiFunc = BuiltinFunctionsCore.ISOWeekNum;

            Assert.Contains(nyiFunc, config.Functions);

            // RecalcEngine will add the interpreter's functions. 
            var engine = new RecalcEngine(config);

            Assert.DoesNotContain(nyiFunc, config.Functions);
                
            var names = engine.GetAllFunctionNames().ToArray();
            Assert.True(names.Length > 100);

            // Spot check some known functions
            Assert.Contains("Cos", names);
            Assert.Contains("ParseJSON", names);

            Assert.Contains("Cos", names);
        }

        [Fact]
        public void CheckSuccess()
        {
            var engine = new RecalcEngine();
            var result = engine.Check(
                "3*2+x",
                new KnownRecordType().Add(
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

            var result = engine.Check("T.Var = 23", new KnownRecordType()
                .Add(new NamedFormulaType("T", new KnownRecordType().Add(new NamedFormulaType("Var", FormulaType.String)))));

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
            var result = engine.Check("3+foo+2", new KnownRecordType()); // foo is undefined 

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
                new KnownRecordType().Add(
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
            var formulaValue = result.Expression.Eval(recordValue);
            Assert.Equal(11.0, (double)formulaValue.ToObject());
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
               new KnownRecordType().Add(
                   new NamedFormulaType("x", FormulaType.Number)));

            Assert.True(result.IsSuccess);
            Assert.True(result.ReturnType is NumberType);
            Assert.Single(result.TopLevelIdentifiers);
            Assert.Equal("x", result.TopLevelIdentifiers.First());
        }

        [Fact]
        public void RecalcEngineLocksConfig()
        {
            var config = new PowerFxConfig(null);
            config.SetCoreFunctions(new TexlFunction[0]); // clear builtins
            config.AddFunction(BuiltinFunctionsCore.Blank);
            
            var recalcEngine = new Engine(config);

            var func = BuiltinFunctionsCore.AsType; // Function not already in engine
            Assert.DoesNotContain(func, config.Functions); // didn't get auto-added by engine.

            var optionSet = new OptionSet("foo", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() { { "one key", "one value" } }));
            Assert.Throws<InvalidOperationException>(() => config.AddFunction(func));
            Assert.Throws<InvalidOperationException>(() => config.AddOptionSet(optionSet));

            Assert.False(config.TryGetSymbol(new DName("foo"), out _, out _));

            Assert.DoesNotContain(BuiltinFunctionsCore.Abs, config.Functions);
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

            var enums = config.EnumStoreBuilder.Build().EnumSymbols;

            Assert.True(enums.Count() > 0);
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
