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

            engine.UpdateVariable("x", FormulaValue.New(12));

            var r1 = engine.Eval("x", null, _opts); // 12
            Assert.Equal(12.0, r1.ToObject());

            var r2 = engine.Eval("Set(x, 15)", null, _opts);

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

            engine.UpdateVariable("x", FormulaValue.NewBlank(FormulaType.Number));

            // Circular reference ok
            var r3 = engine.Eval("Set(x, 1);Set(x,x+1);x", null, _opts);
            Assert.Equal(2.0, r3.ToObject());
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
            var obj = cache.Marshal(new { X = 10, Y = 20 });

            engine.UpdateVariable("obj", obj);
            
            // Can update record
            var r1 = engine.Eval("Set(obj, {X: 11, Y: 21}); obj.X", null, _opts);
            Assert.Equal(11.0, r1.ToObject());

            // But SetField fails 
            var r2 = engine.Check("Set(obj.X, 31); obj.X", null, _opts);
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
            var sym = new SymbolValues();
            sym.Add("x", FormulaValue.New(12));

            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            var expr = "Set(x, x+1);x";
            
            var result = engine.EvalAsync(expr, CancellationToken.None, options: _opts, runtimeConfig: sym).Result;

            Assert.Equal(13.0, result.ToObject());

            var found = sym.TryGetValue("x", out var result2);
            Assert.True(found);
            Assert.Equal(13.0, result2.ToObject());
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

            var expr = "Set(displayNum, 12); displayNum";

            var sym = ReadOnlySymbolValues.NewFromRecord(record);
            
            var config = new PowerFxConfig();
            config.EnableSetFunction();
            var engine = new RecalcEngine(config);

            var result = engine.EvalAsync(expr, CancellationToken.None, options: _opts, runtimeConfig: sym).Result;

            Assert.Equal(12.0, result.ToObject());

            var found = sym.TryGetValue("num", out var result2);
            Assert.True(found);
            Assert.Equal(12.0, result2.ToObject());

            // Can get invariant form.
            var invariant = engine.GetInvariantExpression(expr, recordType);
            Assert.Equal("Set(num, 12); num", invariant);
        }

        // $$$ What about type-not found? Or failure to update?        
    }
}
