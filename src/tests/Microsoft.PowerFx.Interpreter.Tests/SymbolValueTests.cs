// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class SymbolValueTests
    {
        // Mutating variables still invalidates a symbol table.
        [Fact]
        public async Task MutationProtectionForSymbolValues()
        {
            var locals = new SymbolValues();
            var s1 = locals.SymbolTable;
            var v1 = s1.VersionHash;

            locals.Add("local", FormulaValue.New(3));

            // Symbol table we handed out is now stale. 
            var v1b = s1.VersionHash;
            Assert.NotEqual(v1, v1b);

            // Fetch a new current one.
            var s2 = locals.SymbolTable;
            var v2 = s2.VersionHash;

            Assert.Same(s1, s2);
            Assert.NotEqual(v1, v2);
        }

        [Fact]
        public void Test()
        {
            var locals = new SymbolValues();
            locals.Add("a", FormulaValue.New(2));

            // Duplicate add fails 
            Assert.Throws<InvalidOperationException>(() => locals.Add("a", FormulaValue.New(2)));

            var ret = locals.TryGetValue("a", out var v1);
            Assert.True(ret);
            Assert.Equal(2.0, v1.ToObject());

            ret = locals.TryGetValue("missing", out v1);
            Assert.False(ret);
            Assert.Null(v1);

            var symbols = locals.SymbolTable;
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Null(symbols.Parent);
#pragma warning restore CS0618 // Type or member is obsolete

            // Internal hook is null;
            var a = new DName("a");
            ret = symbols.TryLookup(a, out var info);
            Assert.False(ret);

            // Lookup via name resolver succeeds.
            INameResolver resolver = symbols;
            ret = resolver.Lookup(a, out info);
            Assert.True(ret);
            Assert.Equal(Core.Types.DKind.Number, info.Type.Kind);
        }

        [Fact]
        public void TestMissingRegular()
        {
            var symTable = new SymbolTable();
            var symValues = new SymbolValues(symTable);

            var slot = symTable.AddVariable("x", FormulaType.Number);

            // Valid slot, but hasn't been set yet. 
            var result = symValues.Get(slot);
            Assert.IsType<BlankValue>(result);
            Assert.IsType<NumberType>(result.Type);

            // In an expression, becomes blank. 
            var engine = new RecalcEngine();
            result = engine.EvalAsync("x", CancellationToken.None, runtimeConfig: symValues).Result;
            Assert.IsType<BlankValue>(result);
            Assert.IsType<NumberType>(result.Type);
        }

        [Fact]
        public void TestSetNul()
        {
            var symTable = new SymbolTable();
            var symValues = new SymbolValues(symTable);

            var slot = symTable.AddVariable("x", FormulaType.Number);

            symValues.Set(slot, FormulaValue.New(10));
            var result = symValues.Get(slot);
            Assert.Equal(10.0, result.ToObject());

            // Set to null will blank
            symValues.Set(slot, null);

            result = symValues.Get(slot);
            Assert.IsType<BlankValue>(result);
            Assert.IsType<NumberType>(result.Type);
        }

        [Fact]
        public void ShadowValues()
        {
            var r1 = new SymbolValues { DebugName = "L1" };
            var r2 = new SymbolValues { DebugName = "L2" };
            r1.Add("x", FormulaValue.New(2));
            r1.Add("y", FormulaValue.New(3));

            r2.Add("x", FormulaValue.New("str")); // Shadows r1.X

            var r21 = ReadOnlySymbolValues.Compose(r2, r1);

            // Shadowed            
            var ret = r1.TryGetValue("x", out var v1);
            Assert.True(ret);
            Assert.Equal(FormulaType.Number, v1.Type);

            ret = r21.TryGetValue("x", out v1);
            Assert.True(ret);
            Assert.Equal(FormulaType.String, v1.Type);

            // Inherits
            ret = r21.TryGetValue("y", out v1);
            Assert.True(ret);
            Assert.Equal(FormulaType.Number, v1.Type);
        }

        [Fact]
        public void TestRowScope()
        {
            var r1 = new SymbolValues();
            r1.Add("b", FormulaValue.New(1));
            Assert.Equal("RuntimeValues", r1.DebugName);

            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("a", FormulaValue.New(10)));

            var r2 = NewRowScope(record, r1);
            Assert.Equal("(RowScope,RuntimeValues)", r2.DebugName);

            var engine = new RecalcEngine();

            var result = engine.EvalAsync("ThisRecord.a + a + b", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(21.0, result.ToObject());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRowScopeValues(bool allowThisRecord)
        {
            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("a", FormulaValue.New(10)),
                new NamedValue("b", FormulaValue.New(20)));

            var symbolTable = ReadOnlySymbolTable.NewFromRecord(record.Type, allowThisRecord: allowThisRecord);
            var s2 = (SymbolTableOverRecordType)symbolTable;

            bool found;
            found = symbolTable.TryLookupSlot("a", out var slot1);
            Assert.True(found);
            Assert.Same(symbolTable, slot1.Owner);

            // Same value returns same slot. 
            found = symbolTable.TryLookupSlot("a", out var slot2);
            Assert.True(found);
            Assert.Same(symbolTable, slot2.Owner);
            Assert.Equal(slot1.SlotIndex, slot2.SlotIndex);

            // Getting values. 
            var value = s2.GetValue(slot1, record);
            Assert.Equal(10.0, value.ToObject());

            // check ThisRecord
            found = symbolTable.TryLookupSlot("ThisRecord", out var slotThisRecord);
            if (allowThisRecord)
            {
                Assert.True(found);
                Assert.Same(symbolTable, slotThisRecord.Owner);
                Assert.True(s2.IsThisRecord(slotThisRecord));

                value = s2.GetValue(slotThisRecord, record);
                Assert.Same(record, value);

                // Can't set ThisRecord, it's readonly
                var values = new RowScopeSymbolValues(s2, record);
                Assert.Throws<InterpreterConfigException>(() => values.Set(slotThisRecord, record));
            }
            else
            {
                Assert.False(found);
                Assert.Null(slotThisRecord);
            }

            // chcek missing 
            found = symbolTable.TryLookupSlot("missing", out var slot3);
            Assert.False(found);
            Assert.Null(slot3);
        }

        [Fact]
        public void TestRowScopeNoParent()
        {
            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("a", FormulaValue.New(10)));

            var r2 = NewRowScope(record);
            Assert.Equal("RowScope", r2.DebugName);

            var engine = new RecalcEngine();

            var result = engine.EvalAsync("ThisRecord.a + a", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(20.0, result.ToObject());
        }

        // Ensure the RowScope is lazy and doesn't call fields. 
        [Fact]
        public void TestRowScopeLazy()
        {
            var r1 = new SymbolValues();
            r1.Add("b", FormulaValue.New(1));

            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("a", FormulaValue.New(10)));

            record = LazyType.Wrap(record); // Ensure we don't call RecordType.Fields

            var r2 = NewRowScope(record, r1);

            var table = r2.SymbolTable;

            var engine = new RecalcEngine();

            var result = engine.EvalAsync("ThisRecord.a + a + b", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(21.0, result.ToObject());
        }

        // Type to ensure we don't call Fields. 
        private class LazyType : RecordType
        {
            private readonly RecordType _inner;

            public LazyType(RecordType inner)
            {
                _inner = inner;
            }

            // Wrap the existing record, but ensure we don't call FieldNames on it. 
            // This ensures operations stay lazy. 
            public static RecordValue Wrap(RecordValue record)
            {
                return FormulaValue.NewRecordFromFields(new LazyType(record.Type), record.Fields);
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                return _inner.TryGetFieldType(name, out type);
            }

            // Explicit don't enumerate fields
            public override IEnumerable<string> FieldNames => throw new NotImplementedException();

            public override bool Equals(object other) => throw new NotImplementedException();

            public override int GetHashCode() => throw new NotImplementedException();
        }

        /* $$$ Split this into 2 tests. 
        // Test Composing symbol tables. 
        [Fact]
        public void Compose()
        {
            var culture1 = new CultureInfo("en-us");

            var r1 = new SymbolValues { DebugName = "L1" };
            r1.Add("x", FormulaValue.New("x1"));
            r1.AddService(culture1);

            var r2 = new SymbolValues { DebugName = "L2" };
            r2.Add("y", FormulaValue.New("y2"));
            r2.Add("x", FormulaValue.New("x2"));

            // Compose. r1 is first, has higher precedence
            var r3 = ReadOnlySymbolValues.Compose(r1, r2);

            var found = r3.TryGetValue("x", out var result);
            Assert.True(found);
            Assert.Equal("x1", result.ToObject());

            found = r3.TryGetValue("y", out result);
            Assert.True(found);
            Assert.Equal("y2", result.ToObject());

            found = r3.TryGetValue("missing", out result);
            Assert.False(found);
            Assert.Null(result);

            var culture2 = r3.GetService<CultureInfo>();
            Assert.Same(culture1, culture2);

            // Flipping the order changes precedence
            r3 = ReadOnlySymbolValues.Compose(r2, r1);
            found = r3.TryGetValue("x", out result);
            Assert.True(found);
            Assert.Equal("x2", result.ToObject());

            culture2 = r3.GetService<CultureInfo>();
            Assert.Same(culture1, culture2);
        }*/

        [Fact]
        public void ComposeNest()
        {
            var r1 = new SymbolValues();
            r1.Add("x", FormulaValue.New("x1"));

            var r2 = new SymbolValues();
            r2.Add("y", FormulaValue.New("y2"));

            var r3 = new SymbolValues();
            r3.Add("z", FormulaValue.New("z3"));

            // Compose can nest
            var r2_3 = ReadOnlySymbolValues.Compose(r2, r3);

            var rAll = ReadOnlySymbolValues.Compose(r1, r3);

            var found = rAll.TryGetValue("z", out var result);
            Assert.True(found);
            Assert.Equal("z3", result.ToObject());
        }

        [Fact]
        public void TestNew()
        {
            var dict = new Dictionary<string, NumberValue>
            {
                { "a", FormulaValue.New(1) },
                { "b", FormulaValue.New(10) }
            };

            var r1 = new SymbolValues { DebugName = "L1" };
            r1.Add("global", FormulaValue.New(100));

            var r2 = ReadOnlySymbolValues.New(dict, r1);

            var engine = new RecalcEngine();
            var result = engine.EvalAsync("a + b + global", CancellationToken.None, runtimeConfig: r2).Result;

            Assert.Equal(111.0, result.ToObject());

            // Without a parent
            var r3 = ReadOnlySymbolValues.New(dict);
            var result3 = engine.EvalAsync("a + b", CancellationToken.None, runtimeConfig: r2).Result;
            Assert.Equal(11.0, result3.ToObject());
        }

        [Fact]
        public void Test1()
        {
            var symbolTable = new SymbolTable
            {
                DebugName = "My Locals"
            };
            var slot1 = symbolTable.AddVariable("x", FormulaType.Number);

            var values = symbolTable.CreateValues();
            values.Set(slot1, FormulaValue.New(10));

            var result = values.Get(slot1);
            Assert.Equal(10.0, result.ToObject());

            var engine = new RecalcEngine();
            result = engine.EvalAsync("x+1", CancellationToken.None, runtimeConfig: values).Result;

            Assert.Equal(11.0, result.ToObject());
        }

        [Fact]
        public void UpdateCompose()
        {
            var sym1 = new SymbolValues { DebugName = "L1 " };
            sym1.Add("v1", FormulaValue.New(1));
            sym1.Add("v2", FormulaValue.New(2)); // shadowed 

            Assert.Equal("v1=1;v2=2;|", Get(sym1));

            var sym2b = new SymbolValues { DebugName = "L2" };
            sym2b.Add("v2", FormulaValue.New(20));

            var sym2 = ReadOnlySymbolValues.Compose(sym2b, sym1);

            Assert.Equal("v2=20;|v1=1;v2=<shadow>;|", Get(sym2));

            sym2.UpdateValue("v2", FormulaValue.New(21));
            Assert.Equal("v2=21;|v1=1;v2=<shadow>;|", Get(sym2));
            Assert.Equal("v1=1;v2=2;|", Get(sym1)); // v2 in parent not updated 

            sym2.UpdateValue("v1", FormulaValue.New(19)); // finds in Parent 
            Assert.Equal("v2=21;|v1=19;v2=<shadow>;|", Get(sym2));
            Assert.Equal("v1=19;v2=2;|", Get(sym1)); // v2 in parent not updated 

            var sym3a = new SymbolValues();
            sym3a.Add("v3a", FormulaValue.New(30));

            var sym3b = new SymbolValues { DebugName = "L3 " };
            sym3b.Add("v3b", FormulaValue.New(31));

            var sym3 = ReadOnlySymbolValues.Compose(sym3a, sym3b, sym2);

            sym3.UpdateValue("v1", FormulaValue.New(18));
            Assert.Equal("v1=18;v2=2;|", Get(sym1));

            sym3.UpdateValue("v3a", FormulaValue.New(38));
            sym3.UpdateValue("v3b", FormulaValue.New(39));
            Assert.Equal("v3a=38;|v3b=39;|v1=18;v2=21;|", Get(sym3));
        }

        [Fact]
        public void UpdateCompose2()
        {
            // Nested composes
            var sym1a = new SymbolValues();
            var sym1b = new SymbolValues();

            var sym1 = ReadOnlySymbolValues.Compose(sym1a, sym1b);

            var sym2a = new SymbolValues();
            sym2a.Add("v2a", FormulaValue.New(1));

            var sym2 = ReadOnlySymbolValues.Compose(sym1, sym2a);

            sym2.UpdateValue("v2a", FormulaValue.New(2));
            Assert.Equal("|v2a=2;|", Get(sym2));
        }

        [Fact]
        public void RowScope()
        {
            var displayName = "displayNum";
            var recordType = RecordType.Empty()
                .Add(new NamedFormulaType("num", FormulaType.Number, displayName));

            var record = FormulaValue.NewRecordFromFields(
                recordType,
                new NamedValue("num", FormulaValue.New(11)));

            var sym = ReadOnlySymbolValues.NewFromRecord(record);

            Assert.Equal("num=11;|", Get(sym));

            // No 'ThisRecord'
            var ok = sym.TryGetValue("ThisRecord", out var x);
            Assert.False(ok);
            Assert.Null(x);

            // Update
            sym.UpdateValue("num", FormulaValue.New(12));
            Assert.Equal("num=12;|", Get(sym));
        }

        [Fact]
        public void TestMismatch()
        {
            var symTable1 = new SymbolTable { DebugName = "L1" };
            var symTable2 = new SymbolTable { DebugName = "L2" };

            var slotX1 = symTable1.AddVariable("x", FormulaType.Number);
            var slotX2 = symTable2.AddVariable("x", FormulaType.Number);

            var symValues = symTable1.CreateValues();
            symValues.Set(slotX1, FormulaValue.New(1)); // ok

            // Illegal operation, slot doesn't match this table.
            Assert.Throws<InvalidOperationException>(() => symValues.Set(slotX2, FormulaValue.New(1)));
        }

        // Test removing a variable disposes the slot and operations will fail.
        [Fact]
        public void TestRemove()
        {
            var symTable1 = new SymbolTable { DebugName = "L1" };
            var slot = symTable1.AddVariable("x", FormulaType.Number);
            Assert.False(slot.IsDisposed());

            var symValues = symTable1.CreateValues();
            symValues.Set(slot, FormulaValue.New(10));

            var oldIndex = slot.SlotIndex;
            symTable1.RemoveVariable("x");
            Assert.True(slot.IsDisposed()); // Disposed now 

            // Operations fail on disposed slots
            Assert.Throws<InvalidOperationException>(() => symValues.Get(slot));
            Assert.Throws<InvalidOperationException>(() => symValues.Set(slot, FormulaValue.New(11)));

            // readding (And reusing the slot) shouldn't get the old oprhaned value. 
            var slot2 = symTable1.AddVariable("y", FormulaType.Number);
            var value2 = symValues.Get(slot2);

            // same index, but ensure we're not reusing. 
            Assert.Equal(oldIndex, slot2.SlotIndex);
            Assert.Null(value2.ToObject()); // Blank, hasn't been set 
        }

        // Test a mismatch.
        // If we check against a SymbolTable based on AddVariables; we can't eval with a RowScope. 
        [Fact]
        public async Task TestMismatch1()
        {
            var symTable1 = new SymbolTable { DebugName = "L1" };
            var slot = symTable1.AddVariable("x", FormulaType.Number);

            var record = FormulaValue.NewRecordFromFields(
                new NamedValue("x", FormulaValue.New(11)));
            var recordType = record.Type;

            // A row scope SymbolValues must use a Row-scope symbol Table. 
            Assert.Throws<ArgumentException>(() => ReadOnlySymbolValues.NewFromRecord(symTable1, record));

            var engine = new Engine(new PowerFxConfig());
            var check = engine.Check("x+1", symbolTable: symTable1);

            var eval = check.GetEvaluator();

            // Fails when trying a row-scope symbol values 
            await Assert.ThrowsAsync<ArgumentException>(() => eval.EvalAsync(CancellationToken.None, record));

            // Succeeds when trying  a symbol values associated with the original symbol table.
            var symValues = new SymbolValues(symTable1);
            symValues.Set(slot, FormulaValue.New(10));
            eval.Eval(symValues);
        }

        [Fact]
        public async Task TestMismatch2()
        {
            var symTable1 = new SymbolTable { DebugName = "L1" };
            var slot = symTable1.AddVariable("x", FormulaType.Number);

            // can't assign a string to a number
            var symValues = new SymbolValues(symTable1);
            Assert.Throws<InvalidOperationException>(() => symValues.Set(slot, FormulaValue.New("abc")));
        }

        [Fact]
        public void CreateSingle()
        {
            // Compose on a single item is an identity
            var s1 = new SymbolValues();
            var s2 = ReadOnlySymbolValues.Compose(s1);

            Assert.Same(s2, s1);
        }

        // Trying to provide Multiple Values for the same SymbolTable is an error 
        // That could lead to an ambiguity about which values to use. 
        [Fact]
        public void MultipleValues()
        {
            var symTable1 = new SymbolTable { DebugName = "L1" };
            var slot = symTable1.AddVariable("x", FormulaType.Number);

            // can't assign a string to a number
            var values1 = new SymbolValues(symTable1) { DebugName = "V1" };
            var values2 = new SymbolValues(symTable1) { DebugName = "V1" };

            symTable1.CreateValues(values1); // ok 

            try
            {
                symTable1.CreateValues(values1, values2); // error
            }
            catch (InvalidOperationException e)
            {
                // Message should be useful. 
                Assert.Contains(symTable1.DebugName, e.Message);
                Assert.Contains(values1.DebugName, e.Message);
                Assert.Contains(values2.DebugName, e.Message);
            }
        }

        // Trying to eval with missing symbol values will fail
        [Fact]
        public async Task MissingSymbolValues()
        {
            // Get meaningful error if we eval with the "wrong" symbols 
            var symTable1 = new SymbolTable { DebugName = "L1" };
            symTable1.AddVariable("var1", FormulaType.Number);

            var engine = new RecalcEngine();
            var check = engine.Check("1+2", symbolTable: symTable1);

            var symTable2 = new SymbolTable { DebugName = "L2" };
            var symValues2 = symTable2.CreateValues();
            var run = check.GetEvaluator();

            // Pass in symbolValues that aren't associated with symTable1 used in check. 
            Assert.NotSame(symTable1, symValues2.SymbolTable);

            // Catch and proactively get error. 
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await run.EvalAsync(CancellationToken.None, symValues2));

            // Works when we pass in right symbol values (tied to what we checked against)
            var symValues1 = symTable1.CreateValues();
            run.Eval(symValues1);
        }
                
        // Fail when trying to eval with an extra SymbolValue that doesn't below.
        [Fact]
        public async Task MismatchedSymbolValues()
        {
            var symValue = new SymbolValues { DebugName = "Extra" };
            var engine = new RecalcEngine();

            // Succeeds since eval will get the symbol table from values 
            await engine.EvalAsync("1+2", CancellationToken.None, runtimeConfig: symValue);

            // Check() without binding to a symbol table
            var check = engine.Check("1+2");
            Assert.True(check.IsSuccess);

            var run = check.GetEvaluator();

            // Succeeds, no extra SymbolValues
            await run.EvalAsync(CancellationToken.None, new RuntimeConfig());

            // Fails, extra symbol Values 
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await run.EvalAsync(CancellationToken.None, new RuntimeConfig(symValue)));
        }

        // Demonstrate how to use ThisItem in a loop.
        // All Loop iterations can access common global state,
        // but also have their own loop-specific identifer. 
        [Fact]
        public async Task ThisItemTest()
        {
            var symTableGlobals = new SymbolTable { DebugName = "Globals" };
            var slotGlobal = symTableGlobals.AddVariable("globalVar", FormulaType.Number);

            // Note from PowerFx perspective, there is nothing special about the identifier 'ThisItem'.
            // It's all determined by SymbolTable topology. 
            var symTableThisItem = new SymbolTable { DebugName = "ThisItem" };
            var slotThisItem = symTableThisItem.AddVariable("ThisItem", FormulaType.Number);

            // Combine together
            var symTable = ReadOnlySymbolTable.Compose(symTableThisItem, symTableGlobals);

            var expr = "globalVar + ThisItem";

            // Compile once outside the loop, then run many times. 
            var engine = new RecalcEngine();
            var check = engine.Check(expr, symbolTable: symTable);
            check.ThrowOnErrors();

            // Now move to runtime...
            var run = check.GetEvaluator();

            // all loop bodies can read-only access common global state. 
            var symValuesGlobals = symTableGlobals.CreateValues();
            symValuesGlobals.Set(slotGlobal, FormulaValue.New(1000));

            // Loop body can be run in parallel since each eval is independent. 
            // An there are no mutations on shared state.
            Parallel.For(0, 10, (i) =>
            {
                // Each loop body gets its own iteration-specific values.
                var symValuesThisItem = symTableThisItem.CreateValues();
                symValuesThisItem.Set(slotThisItem, FormulaValue.New(i));

                var symValues = symTable.CreateValues(symValuesGlobals, symValuesThisItem);

                // In the loop, just eval.
                var result = run.Eval(symValues);
                var expected = 1000D + i; // matches 'expr'
                Assert.Equal(expected, result.ToObject());
            });

            // ThisItem is also immutable.
            // But we could do other mutations from within - but if it's shared state, be careful to not run in parallel.
            {
                var symValuesThisItem = symTableThisItem.CreateValues();
                symValuesThisItem.Set(slotThisItem, FormulaValue.New(5));

                // Add another table with a mutable var, 'Counter', and demonstrate 
                // we can call Set. 
                var symTable2 = new SymbolTable();
                symTable2.EnableMutationFunctions();
                var slot = symTable2.AddVariable("counter", FormulaType.Number, mutable: true);

                var symTableAll = ReadOnlySymbolTable.Compose(symTable2, symTableThisItem);
                var symValuesAll = symTableAll.CreateValues(symValuesThisItem);

                var opts = new ParserOptions { AllowsSideEffects = true };

                var runtimeConfig = new RuntimeConfig(symValuesAll);
                var result = await engine.EvalAsync("Set(counter, ThisItem);counter", CancellationToken.None, options: opts, runtimeConfig: runtimeConfig);

                Assert.Equal(5.0, result.ToObject());

                // but we can't call Set on ThisItem 
                var check2 = engine.Check("Set(ThisItem, 5)", options: opts, symbolTable: symValuesAll.SymbolTable);
                Assert.False(check2.IsSuccess);
            }
        }

        // Get a convenient string representation of a SymbolValue
        private static string Get(ReadOnlySymbolValues values)
        {
            var sb = new StringBuilder();

            var symbolTableAll = values.SymbolTable;

            var seen = new HashSet<string>();

            foreach (var symbolTable in symbolTableAll.SubTables)
            {
                foreach (var sym in symbolTable.SymbolNames.OrderBy(x => x.Name.Value))
                {
                    sb.Append(sym.Name);
                    sb.Append('=');

                    if (seen.Add(sym.Name.Value))
                    {
                        values.TryGetValue(sym.Name, out var value);
                        sb.Append(value?.ToObject()?.ToString());
                    }
                    else
                    {
                        // We have no way to query parent variables from the symbol Values. 
                        // So we just call it shadowed. 
                        sb.Append("<shadow>");
                    }

                    sb.Append(';');
                }

                sb.Append('|'); // break between symbol tables.
            }

            return sb.ToString();
        }

        public static ReadOnlySymbolValues NewRowScope(
            RecordValue parameters, ReadOnlySymbolValues parent = null, string debugName = null)
        {
            var symTable = ReadOnlySymbolTable.NewFromRecord(parameters.Type, allowMutable: true, allowThisRecord: true, debugName: debugName);

            var symValues = ReadOnlySymbolValues.NewFromRecord(symTable, parameters);

            if (parent != null)
            {
                return ReadOnlySymbolValues.Compose(symValues, parent);
            }

            return symValues;
        }
    }

    // Test helpers. 
    internal static class SymTextExtenions
    {
        public static void UpdateValue(this ReadOnlySymbolValues symValues, string v, NumberValue numberValue)
        {
            var table = symValues.SymbolTable;
            if (table.TryLookupSlot(v, out var slot))
            {
                symValues.Set(slot, numberValue);
                return;
            }

            throw new InvalidOperationException();
        }

        public static bool TryGetValue(this ReadOnlySymbolValues symValues, string name, out FormulaValue value)
        {
            if (symValues.SymbolTable.TryLookupSlot(name, out var slot))
            {
                value = symValues.Get(slot);
                return true;
            }

            value = null;
            return false;
        }
    }
}
