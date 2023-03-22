// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SetFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Fact]
        public void SetVar()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12m));

            var r1 = engine.Eval("x", null, _opts); // 12
            Assert.Equal(12m, r1.ToObject());

            var r2 = engine.Eval("Set(x, 15)", null, _opts);

            // Set() returns constant 'true;
            Assert.Equal(true, r2.ToObject());

            r1 = engine.Eval("x"); // 15
            Assert.Equal(15m, r1.ToObject());

            r1 = engine.GetValue("x");
            Assert.Equal(15m, r1.ToObject());          
        }

        // Decimal TODO: Set( x, 1 ); Set( x, Sqrt(2) )

        [Fact]
        public void SetVarNumber()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12));

            var r1 = engine.Eval("x", null, _opts); // 12.0
            Assert.Equal(12.0, r1.ToObject());

            var r2 = engine.Eval("Set(x, Float(15))", null, _opts);

            // Set() returns constant 'true;
            Assert.Equal(true, r2.ToObject());

            r1 = engine.Eval("x"); // 15
            Assert.Equal(15.0, r1.ToObject());

            r1 = engine.GetValue("x");
            Assert.Equal(15.0, r1.ToObject());
        }

        [Fact]
        public void Circular()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.NewBlank(FormulaType.Decimal));

            // Circular reference ok
            var r3 = engine.Eval("Set(x, 1);Set(x,x+1);x", null, _opts);
            Assert.Equal(2.0m, r3.ToObject());
        }

        [Fact]
        public void SetVar2()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(5));
            engine.UpdateVariable("y", FormulaValue.New(7));

            var r1 = engine.Eval("Set(y, x*2);y", null, _opts);
            Assert.Equal(10.0, r1.ToObject());
        }

        // Work with records
        [Fact]
        public void SetRecord()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            var cache = new TypeMarshallerCache();
            var obj = cache.Marshal(new { X = 10m, Y = 20m });

            engine.UpdateVariable("obj", obj);
            
            // Can update record
            var r1 = engine.Eval("Set(obj, {X: 11, Y: 21}); obj.X", null, _opts);
            Assert.Equal(11m, r1.ToObject());

            // But SetField fails 
            var r2 = engine.Check("Set(obj.X, 31); obj.X", null, _opts);
            Assert.False(r2.IsSuccess);
        }

        [Fact]
        public void SetRecordFloat()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            var cache = new TypeMarshallerCache();
            var obj = cache.Marshal(new { X = 10, Y = 20 });

            engine.UpdateVariable("obj", obj);

            // Can update record
            var r1 = engine.Eval("Set(obj, {X: Float(11), Y: Float(21)}); obj.X", null, _opts);
            Assert.Equal(11.0, r1.ToObject());

            // But SetField fails 
            var r2 = engine.Check("Set(obj.X, Float(31)); obj.X", null, _opts);
            Assert.False(r2.IsSuccess);
        }

        // Test various failure cases 
        [Fact]
        public void SetVarFailures()
        {
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12));

            // Fails, can't set var that wasn't already declared
            var result = engine.Check("Set(missing, 12)");
            Assert.False(result.IsSuccess);

            // Fails, behavior function must be in behavior context. 
            result = engine.Check("Set(x, true)");
            Assert.False(result.IsSuccess);

            // Fails, type mismatch
            result = engine.Check("Set(x, true)", null, _opts);
            Assert.False(result.IsSuccess);

            // Fails, arg0 is not a settable object. 
            result = engine.Check("Set({y:x}.y, 20)", null, _opts);
            Assert.False(result.IsSuccess);
        }

        // Set() can only be called if it's enabled.
        [Fact]
        public void SetVarFailureEnabled()
        {
            var config = new PowerFxConfig();
            var engine = new RecalcEngine(config);

            engine.UpdateVariable("x", FormulaValue.New(12));

            var result = engine.Check("Set(x, 15)", null, _opts);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void UpdateSimple()
        {
            var symTable = new SymbolTable();
            var slotX = symTable.AddVariable("x", FormulaType.Number, mutable: true);

            var sym = symTable.CreateValues();            
            sym.Set(slotX, FormulaValue.New(12));

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            var expr = "Set(x, x+1);x";

            var runtimeConfig = new RuntimeConfig(sym);
            var result = engine.EvalAsync(expr, CancellationToken.None, options: _opts, runtimeConfig: runtimeConfig).Result;
            Assert.Equal(13.0, result.ToObject());

            result = sym.Get(slotX);
            Assert.Equal(13.0, result.ToObject());

            var found = sym.TryGetValue("x", out var result2);
            Assert.True(found);
            Assert.Equal(13.0, result2.ToObject());
        }

        [Fact]
        public void MutableByDefault()
        {            
            var sym = new SymbolValues();
            sym.Add("x", FormulaValue.New(12m)); // mutable by default

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            // ok, mutable 
            var expr = "Set(x,5)";

            var result = engine.Check(expr, options: _opts, symbolTable: sym.SymbolTable);
            Assert.True(result.IsSuccess);
        }

        // Attempting to Set() a readonly variable should be a binding error. 
        [Fact]
        public void UpdateFailsOnReadOnlyValue()
        {
            var symTable = new SymbolTable();
            var slotX = symTable.AddVariable("x", FormulaType.Number, mutable: true);

            var sym = symTable.CreateValues();
            sym.Set(slotX, FormulaValue.New(12));

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            // fails on readonly failure. 
            var expr = "With({y : x},Set(y,5))";
            
            var result = engine.Check(expr, options: _opts, symbolTable: symTable);
            Assert.False(result.IsSuccess);

            // but ok to set X since it's mutable. 
            expr = "With({y : x},Set(x,y*2))";

            //result = engine.Check(expr, symbolTable: sym.GetSymbolTableSnapshot());
            result = engine.Check(expr, options: _opts, symbolTable: symTable);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void UpdateFailsOnReadOnlyValue2()
        {
            var symTable = new SymbolTable();
            var slotX = symTable.AddVariable("x", FormulaType.Number, mutable: false);

            var sym = symTable.CreateValues();
            sym.Set(slotX, FormulaValue.New(12)); // Ok 

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            // fails on readonly failure. 
            var expr = "Set(x,5)";

            var result = engine.Check(expr, options: _opts, symbolTable: symTable);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void UpdateFailsOnReadOnlyValue3()
        {
            var symTable = new SymbolTable();
            symTable.AddConstant("x", FormulaValue.New(12));

            var found = symTable.TryLookupSlot("x", out var slot);
            Assert.False(found); // no slots for constants. 
            Assert.Null(slot);

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            // fails on readonly failure. 
            var expr = "Set(x,5)";

            var result = engine.Check(expr, options: _opts, symbolTable: symTable);
            Assert.False(result.IsSuccess);
        }

        // Demonstrate we can have a single Check w/ SymbolTable used against multiple SymbolValues.
        [Fact]
        public void Update3()
        {
            var symTable = new SymbolTable();
            var slot = symTable.AddVariable("num", FormulaType.Number, mutable: true);

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            // Create multiple values that share a symbol table.
            var symValues1 = symTable.CreateValues();
            symValues1.Set(slot, FormulaValue.New(10));

            var symValues2 = symTable.CreateValues();
            symValues2.Set(slot, FormulaValue.New(20));

            var check = engine.Check("Set(num, num+5)", options: _opts, symbolTable: symTable);
            check.ThrowOnErrors();
            var eval = check.GetEvaluator();

            foreach (var symValue in new[] { symValues1, symValues2 })
            {
                var runtimeConfig = new RuntimeConfig(symValue);
                var result = eval.EvalAsync(CancellationToken.None, runtimeConfig: runtimeConfig);
            }

            AssertValue(symValues1, "num", 15.0);
            AssertValue(symValues2, "num", 25.0);
        }

        [Fact]
        public void UpdateRowScope()
        {
            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("num", FormulaType.Number, "displayNum"))
                .Add(new NamedFormulaType("str", FormulaType.String, "displayStr"));

            var record = FormulaValue.NewRecordFromFields(
                recordType,
                new NamedValue("num", FormulaValue.New(11)),
                new NamedValue("str", FormulaValue.New("abc")));

            var expr = "Set(displayNum, Float(12)); displayNum";
                        
            var sym = NewMutableFromRecord(record);
            
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            var runtimeConfig = new RuntimeConfig(sym);
            var result = engine.EvalAsync(expr, CancellationToken.None, options: _opts, runtimeConfig: runtimeConfig).Result;

            Assert.Equal(12.0, result.ToObject());

            AssertValue(sym, "num", 12.0);

            // Can get invariant form.
            var invariant = engine.GetInvariantExpression(expr, recordType);
            Assert.Equal("Set(num, Float(12)); num", invariant);
        }

        // Expression from 2 row scopes!
        [Fact]
        public void UpdateRowScope2()
        {
            var recordType1 = RecordType.Empty()
                .Add(new NamedFormulaType("num", FormulaType.Number, "displayNum"))
                .Add(new NamedFormulaType("str", FormulaType.String, "displayStr"));

            var record1 = FormulaValue.NewRecordFromFields(
                recordType1,
                new NamedValue("num", FormulaValue.New(10)),
                new NamedValue("str", FormulaValue.New("abc")));

            var recordType2 = RecordType.Empty()
                .Add(new NamedFormulaType("num2", FormulaType.Number, "displayNum2"));

            var record2 = FormulaValue.NewRecordFromFields(
                recordType2,
                new NamedValue("num2", FormulaValue.New(20)));

            var expr = "Set(displayNum, displayNum2+5); displayNum";
                        
            var sym1 = NewMutableFromRecord(record1);
            var sym2 = NewMutableFromRecord(record2);

            var sym = ReadOnlySymbolValues.Compose(sym1, sym2);

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            // Verify Check, creating symbol table several ways 
            var check1 = engine.Check(expr, _opts, sym.SymbolTable);
            Assert.True(check1.IsSuccess);

            var symTable1 = NewMutableFromRecord(recordType1);
            var symTable2 = NewMutableFromRecord(recordType2);
            var symTable = ReadOnlySymbolTable.Compose(symTable1, symTable2);

            var check2 = engine.Check(expr, _opts, symTable);
            Assert.True(check1.IsSuccess);

            // Verify evaluation. 
            var runtimeConfig = new RuntimeConfig(sym);
            var result = engine.EvalAsync(expr, CancellationToken.None, options: _opts, runtimeConfig: runtimeConfig).Result;

            Assert.Equal(25.0, result.ToObject());

            AssertValue(sym, "num", 25.0);

            // Can get invariant form.
            // Can't pass in symbol table here...
            // https://github.com/microsoft/Power-Fx/issues/767
            //var invariant = engine.GetInvariantExpression(expr, recordType);
            //Assert.Equal("Set(num, 12); num", invariant);
        }

        private static void AssertMissing(ReadOnlySymbolValues symValues, string name)
        {
            var found = symValues.TryGetValue(name, out var result);
            Assert.False(found);
            Assert.Null(result);
        }

        private static void AssertValue(ReadOnlySymbolValues symValues, string name, object expected)
        {
            var found = symValues.TryGetValue(name, out var result);
            Assert.True(found);
            Assert.Equal(expected, result.ToObject());
        }

        public static ReadOnlySymbolValues NewMutableFromRecord(RecordValue parameters)
        {
            var symTable = NewMutableFromRecord(parameters.Type);
            return ReadOnlySymbolValues.NewFromRecord(symTable, parameters);
        }

        public static ReadOnlySymbolTable NewMutableFromRecord(RecordType type)
        {
            var symTable = ReadOnlySymbolTable.NewFromRecord(type, allowMutable: true);
            return symTable;
        }

        [Fact]
        public void NewErrorMessageWithTypes()
        {
            // Create an engine with variable and enable mutation functions
            var engine = new RecalcEngine(new PowerFxConfig());
            engine.UpdateVariable("testDoubleVariable", 2.0);
            engine.Config.SymbolTable.EnableMutationFunctions();

            // Run compliation check
            var check = engine.Check("Set(testDoubleVariable, true)", options: _opts);

            // Verify check
            Assert.False(check.IsSuccess);
            Assert.Contains(check.Errors, d => d.Message.Contains("Invalid argument type (Boolean). Expecting a Number value instead."));
        }
    }
}
