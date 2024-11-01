﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class SymbolTableTests
    {
        private readonly Engine _engine = new Engine(new PowerFxConfig());

        private static void AssertUnique(HashSet<VersionHash> set, VersionHash hash)
        {
            Assert.True(set.Add(hash), "Hash value should be unique");
        }

        private static void AssertUnique(HashSet<VersionHash> set, ReadOnlySymbolTable symbolTable)
        {
            AssertUnique(set, symbolTable.VersionHash);
        }

        // Changing the config changes its hash
        [Fact]
        public void ConfigHash()
        {
            var set = new HashSet<VersionHash>();

            var s0 = new SymbolTable();
            AssertUnique(set, s0);

            var s1 = new SymbolTable();
            var s10 = ReadOnlySymbolTable.Compose(s1, s0);
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            s1.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            s1.RemoveVariable("x");
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            // Same as before, but should still be unique VersionHash!
            s1.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            // Try other mutations
            s1.AddConstant("c", FormulaValue.New(1));
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            // New function 
            var func = new PowerFx.Tests.BindingEngineTests.BehaviorFunction();
            var funcName = func.Name;
            s1.AddFunction(func);
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            s1.RemoveFunction(funcName);
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            s1.RemoveFunction(func);
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            var optionSet = new OptionSet("foo", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() { { "one key", "one value" } }));
            s1.AddEntity(optionSet);
            AssertUnique(set, s1);
            AssertUnique(set, s10);

            // Adding to parent still changes our checksum (even if shadowed)
            s0.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s10);
        }

        // Ensure Storage slots are densely assigned 
        [Fact]
        public void Slots()
        {
            var symTable = new SymbolTable();
            var s1 = symTable.AddVariable("x1", FormulaType.Number);
            Assert.Same(s1.Owner, symTable);
            Assert.Equal(0, s1.SlotIndex); // Densely packed

            var s2 = symTable.AddVariable("x2", FormulaType.Number);
            Assert.Same(s2.Owner, symTable);
            Assert.Equal(1, s2.SlotIndex); // Densely packed

            symTable.RemoveVariable("x1");
            Assert.Equal(-1, s1.SlotIndex); // disposed 

            // Fills in gap
            var s3 = symTable.AddVariable("x3", FormulaType.Number);
            Assert.Same(s3.Owner, symTable);
            Assert.Equal(0, s3.SlotIndex); // Densely packed
        }

        [Fact]
        public void Overwrite()
        {
            var s1 = new SymbolTable();
            s1.AddVariable("x", FormulaType.Number);

            // Can't overwrite (even with same type)
            var v1 = s1.VersionHash;
            Assert.Throws<InvalidOperationException>(() => s1.AddVariable("x", FormulaType.Number));

            // Even a failed mutation changes the version hash. 
            Assert.NotEqual(v1, s1.VersionHash);

            // But can remove and re-add
            s1.RemoveVariable("x");
            s1.RemoveVariable("x"); // Ok to remove if missing.
            s1.AddVariable("x", FormulaType.String);

            var result = _engine.Check("x", symbolTable: s1);
            Assert.Equal(FormulaType.String, result.ReturnType);

            // But can shadow. 
            var s2 = new SymbolTable();
            var s21 = ReadOnlySymbolTable.Compose(s2, s1);

            s2.AddVariable("x", FormulaType.Boolean); // hides s1.
            result = _engine.Check("x", symbolTable: s21);
            Assert.Equal(FormulaType.Boolean, result.ReturnType);
        }

        [Fact]
        public void Compose()
        {
            var func1 = new PowerFx.Tests.BindingEngineTests.BehaviorFunction();
            var func2 = new PowerFx.Tests.BindingEngineTests.BehaviorFunction();

            Assert.Equal(func1.Name, func2.Name); // same name, different instances
            Assert.NotSame(func1, func2);

            var s1 = new SymbolTable { DebugName = "Sym1" };
            var s2 = new SymbolTable { DebugName = "Sym2" };

            var s12 = ReadOnlySymbolTable.Compose(s1, s2);

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Empty(s12.Functions.Functions);

            s2.AddFunction(func2);
            var funcs = s12.Functions.Functions.ToArray();
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Single(funcs);
            Assert.Same(func2, funcs[0]);

            // Superceded 
            s1.AddFunction(func1);
#pragma warning disable CS0618 // Type or member is obsolete
            funcs = s12.Functions.Functions.ToArray(); // Query again
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Equal(2, funcs.Length); // both even though they have same name

            // Enumerable is ordered. Takes s1 since that's higher precedence. 
            Assert.Same(func1, funcs[0]);

            // Returns all combined. 
            INameResolver nr = s12;
            var list = nr.LookupFunctions(func1.Namespace, func1.Name).ToArray();
            Assert.Equal(2, list.Length); // both even though they have same name
            Assert.Same(func1, list[0]);
        }

        [Fact]
        public void ValidateNames()
        {
            var s1 = new SymbolTable();

            var invalidName = "    ";
            Assert.Throws<ArgumentException>(() => s1.AddVariable(invalidName, FormulaType.Number));

            Assert.Throws<ArgumentException>(() => s1.AddConstant(invalidName, FormulaValue.New(3)));
        }

        [Fact]
        public void MutableSupportedFunctionsTest()
        {
            var symbolTableOriginal = new Engine(new PowerFxConfig()).SupportedFunctions;
            var symbolTableCopy1 = symbolTableOriginal.GetMutableCopyOfFunctions();
            var symbolTableCopy2 = symbolTableOriginal.GetMutableCopyOfFunctions();
            var symbolTableCopy3 = symbolTableOriginal.GetMutableCopyOfFunctions();

            var originalCount = symbolTableOriginal.Functions.Count();
            var copyCount1 = symbolTableCopy1.Functions.Count();
            var copyCount2 = symbolTableCopy2.Functions.Count();
            var copyCount3 = symbolTableCopy2.Functions.Count();

            Assert.Equal(copyCount1, originalCount);
            Assert.Equal(copyCount2, originalCount);
            Assert.Equal(copyCount3, originalCount);

            symbolTableCopy1.RemoveFunction("Abs");
            symbolTableCopy2.RemoveFunction("Day");
            symbolTableCopy3.RemoveFunction("Cos");

            Assert.Equal(originalCount, symbolTableOriginal.Functions.Count());
            Assert.Equal(copyCount1 - 2, symbolTableCopy1.Functions.Count());
            Assert.Equal(copyCount2 - 1, symbolTableCopy2.Functions.Count());
            Assert.Equal(copyCount3 - 2, symbolTableCopy3.Functions.Count());

            Assert.NotEqual(copyCount1, symbolTableCopy1.Functions.Count());
            Assert.NotEqual(copyCount2, symbolTableCopy2.Functions.Count());
            Assert.NotEqual(copyCount3, symbolTableCopy3.Functions.Count());

            Assert.True(symbolTableOriginal.Functions.AnyWithName("Abs"));
            Assert.True(symbolTableOriginal.Functions.AnyWithName("Day"));
            Assert.True(symbolTableOriginal.Functions.AnyWithName("Text"));
            Assert.True(symbolTableOriginal.Functions.AnyWithName("Cos"));

            Assert.True(symbolTableCopy1.Functions.AnyWithName("Day"));
            Assert.True(symbolTableCopy1.Functions.AnyWithName("Text"));
            Assert.True(symbolTableCopy1.Functions.AnyWithName("Cos"));

            Assert.True(symbolTableCopy2.Functions.AnyWithName("Abs"));
            Assert.True(symbolTableCopy2.Functions.AnyWithName("Text"));
            Assert.True(symbolTableCopy2.Functions.AnyWithName("Cos"));

            Assert.True(symbolTableCopy3.Functions.AnyWithName("Abs"));
            Assert.True(symbolTableCopy3.Functions.AnyWithName("Day"));
            Assert.True(symbolTableCopy3.Functions.AnyWithName("Text"));

            Assert.False(symbolTableCopy1.Functions.AnyWithName("Abs"));
            Assert.False(symbolTableCopy2.Functions.AnyWithName("Day"));
            Assert.False(symbolTableCopy3.Functions.AnyWithName("Cos"));

            // Check if nothing else has been copied
            Assert.Empty(symbolTableCopy1.SymbolNames);
            Assert.Empty(symbolTableCopy2.SymbolNames);
            Assert.Empty(symbolTableCopy3.SymbolNames);
        }

        [Theory]
        [InlineData("logical1+ 5", true)] // logical name
        [InlineData("display1 + 5", true)] // display name
        [InlineData("missing + 5", false)] // display name
        [InlineData("logical1 + logical1", true)] // logical name
        public void Deferred(string expr, bool expectSuccess)
        {
            var map = new SingleSourceDisplayNameProvider(new Dictionary<DName, DName>
            {
                { new DName("logical1"), new DName("display1") }
            });

            int callbackCount = 0;
            var symTable = ReadOnlySymbolTable.NewFromDeferred(map, (disp, logical) =>
            {
                callbackCount++;
                return FormulaType.Number;
            });

            var check = new CheckResult(new Engine());
            check.SetText(expr);
            check.SetBindingInfo(symTable);

            check.ApplyBinding();
            if (expectSuccess)
            {
                Assert.True(check.IsSuccess);
                Assert.Equal(1, callbackCount);
            }
            else
            {
                Assert.False(check.IsSuccess);
                Assert.Equal(0, callbackCount);
            }
        }

        [Fact]
        public void ComposedReadOnlySymbolTableFunctionCacheTest()
        {
            var symbolTable = new SymbolTable();
            symbolTable.AddFunction(new BlankFunction());

            var composed = new ComposedReadOnlySymbolTable(symbolTable);
            var func1 = composed.Functions;

            Assert.NotNull(func1);
            Assert.Equal(1, func1.Count());

            var func2 = composed.Functions;

            Assert.NotNull(func2);
            Assert.Equal(1, func2.Count());

            Assert.Same(func1, func2);

            symbolTable.AddFunction(new SqrtFunction());

            var func3 = composed.Functions;
            Assert.NotNull(func3);
            Assert.Equal(2, func3.Count());
        }

        [Fact]
        public void OptionSetTests()
        {
            var os1 = new OptionSet("os1", DisplayNameProvider.New(new Dictionary<DName, DName>() { { new DName("ln1"), new DName("dn1") } }));
            var os2 = new OptionSet("os2", DisplayNameProvider.New(new Dictionary<DName, DName>() { { new DName("ln2"), new DName("dn2") }, { new DName("ln3"), new DName("dn3") } }));
            
            var st1 = new SymbolTable();
            st1.AddOptionSet(os1);

            Assert.Single(st1.OptionSets);

            var st2 = new SymbolTable();
            st2.AddOptionSet(os2);

            var st3 = SymbolTable.Compose(st1, st2);

            Assert.Equal(2, st3.OptionSets.Count());            
        }

        [Fact]
        public void AddVariableAndOptionSetWithConflicts()
        {
            var st1 = new SymbolTable();            
            st1.AddVariable("var1", FormulaType.String, displayName: "displayName1");
            var os1 = new OptionSet("os1", DisplayNameProvider.New(new Dictionary<DName, DName>() { { new DName("ln1"), new DName("dn1") } }));
            var os2 = new OptionSet("os2", DisplayNameProvider.New(new Dictionary<DName, DName>() { { new DName("xx2"), new DName("yy2") } }));
            st1.AddOptionSet(os1);
            st1.AddOptionSet(os2);

            // The variable we just added should be there
            bool b = st1.TryGetVariable(new DName("var1"), out NameLookupInfo info, out DName displayName);

            Assert.True(b);
            NameSymbol nameSymbol = Assert.IsType<NameSymbol>(info.Data);
            Assert.Equal("var1", nameSymbol.Name);
            Assert.Equal("displayName1", displayName.Value);
            Assert.Equal("s", info.Type.ToString());

            // The optionset we just added should be there
            b = st1.TryGetVariable(new DName("os1"), out info, out displayName);

            Assert.True(b);
            OptionSet optionSet = Assert.IsType<OptionSet>(info.Data);
            Assert.Equal("os1", optionSet.EntityName.Value);
            Assert.Equal("os1", displayName.Value);
            Assert.Equal("L{ln1:l}", info.Type.ToString());

            // The second optionset we just added should be there
            b = st1.TryGetVariable(new DName("os2"), out info, out displayName);

            Assert.True(b);
            optionSet = Assert.IsType<OptionSet>(info.Data);
            Assert.Equal("os2", optionSet.EntityName.Value);
            Assert.Equal("os2", displayName.Value);
            Assert.Equal("L{xx2:l}", info.Type.ToString());

            // Can't add the same variable twice
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => st1.AddVariable("var1", FormulaType.String, displayName: "displayName1"));
            Assert.Equal("var1 is already defined", exception.Message);

            // Can't add the same optionset twice
            NameCollisionException exception2 = Assert.Throws<NameCollisionException>(() => st1.AddOptionSet(os1));
            Assert.Equal("Name os1 has a collision with another display or logical name", exception2.Message);

            // New symbol table with SAME variable name but different type & display name
            var st2 = new SymbolTable();
            st2.AddVariable("var1", FormulaType.Boolean, displayName: "displayName2");
            var os3 = new OptionSet("os2", DisplayNameProvider.New(new Dictionary<DName, DName>() { { new DName("zz2"), new DName("tt2") } }));
            st2.AddOptionSet(os3);

            // The var1 we just added in st2 should be there
            b = st2.TryGetVariable(new DName("var1"), out info, out displayName);

            Assert.True(b);
            nameSymbol = Assert.IsType<NameSymbol>(info.Data);
            Assert.Equal("var1", nameSymbol.Name);
            Assert.Equal("displayName2", displayName.Value);
            Assert.Equal("b", info.Type.ToString());

            // The optionset we just added should be there
            b = st2.TryGetVariable(new DName("os2"), out info, out displayName);

            Assert.True(b);
            optionSet = Assert.IsType<OptionSet>(info.Data);
            Assert.Equal("os2", optionSet.EntityName.Value);
            Assert.Equal("os2", displayName.Value);
            Assert.Equal("L{zz2:l}", info.Type.ToString());

            // Compose symbol tables in st1, st2 order
            // even if there are variable name conflicts, Compose will just work fine
            ReadOnlySymbolTable rost1 = SymbolTable.Compose(new[] { st1, st2 });

            // Here we'll get var1 from st1 and first always win
            b = rost1.TryGetVariable(new DName("var1"), out info, out displayName);

            Assert.True(b);
            nameSymbol = Assert.IsType<NameSymbol>(info.Data);
            Assert.Equal("var1", nameSymbol.Name);
            Assert.Equal("displayName1", displayName.Value);
            Assert.Equal("s", info.Type.ToString());

            // os1 is only defined once, so we'll get it as excepted
            b = rost1.TryGetVariable(new DName("os1"), out info, out displayName);

            Assert.True(b);
            optionSet = Assert.IsType<OptionSet>(info.Data);
            Assert.Equal("os1", optionSet.EntityName.Value);
            Assert.Equal("os1", displayName.Value);
            Assert.Equal("L{ln1:l}", info.Type.ToString());

            // The optionset os2 is defined twice but we'll get the version from st1 (first one wins)
            b = rost1.TryGetVariable(new DName("os2"), out info, out displayName);

            Assert.True(b);
            optionSet = Assert.IsType<OptionSet>(info.Data);
            Assert.Equal("os2", optionSet.EntityName.Value);
            Assert.Equal("os2", displayName.Value);
            Assert.Equal("L{xx2:l}", info.Type.ToString());

            // There should only be 2 'visible' optionset (os1 and os2)
            Assert.Equal(2, rost1.OptionSets.Count());

            // Check types of OptionSets and confirm os2 is from st1
            Assert.Equal("os1:ln1_dn1, os2:xx2_yy2", string.Join(", ", rost1.OptionSets.Select(kvp => $"{kvp.Key}:{string.Join(",", kvp.Value.FormulaType._type.DisplayNameProvider.LogicalToDisplayPairs.Select(p => $"{p.Key}_{p.Value}"))}")));

            // Now we compose in the other order: st2 first
            ReadOnlySymbolTable rost2 = SymbolTable.Compose(new[] { st2, st1 });

            // This time, we'll get var1 from st2 and we can see the difference on display name and type
            b = rost2.TryGetVariable(new DName("var1"), out info, out displayName);

            Assert.True(b);
            nameSymbol = Assert.IsType<NameSymbol>(info.Data);
            Assert.Equal("var1", nameSymbol.Name);
            Assert.Equal("displayName2", displayName.Value);
            Assert.Equal("b", info.Type.ToString());

            // No change here as there is only one option set os1
            b = rost2.TryGetVariable(new DName("os1"), out info, out displayName);

            Assert.True(b);
            optionSet = Assert.IsType<OptionSet>(info.Data);
            Assert.Equal("os1", optionSet.EntityName.Value);
            Assert.Equal("os1", displayName.Value);
            Assert.Equal("L{ln1:l}", info.Type.ToString());

            // The optionset os2 is defined twice but we'll get the version from st2 (first one wins)
            b = rost2.TryGetVariable(new DName("os2"), out info, out displayName);

            Assert.True(b);
            optionSet = Assert.IsType<OptionSet>(info.Data);
            Assert.Equal("os2", optionSet.EntityName.Value);
            Assert.Equal("os2", displayName.Value);
            Assert.Equal("L{zz2:l}", info.Type.ToString());

            // There should only be 2 'visible' optionset (os1 and os2)
            Assert.Equal(2, rost2.OptionSets.Count());

            // Check types of OptionSets and confirm os2 is from st2
            Assert.Equal("os2:zz2_tt2, os1:ln1_dn1", string.Join(", ", rost2.OptionSets.Select(kvp => $"{kvp.Key}:{string.Join(",", kvp.Value.FormulaType._type.DisplayNameProvider.LogicalToDisplayPairs.Select(p => $"{p.Key}_{p.Value}"))}")));
        }

        [Fact]
        public void VoidIsNotAllowed()
        {
            var symbol = new SymbolTable();
            Assert.Throws<InvalidOperationException>(() => symbol.AddVariable("x", FormulaType.Void, mutable: true));
        }

        // $$$ Consistent with SymbolNames

        [Fact]
        public void TryGetType()
        {
            var os = new OptionSet(
                "os1",
                DisplayNameUtility.MakeUnique(new Dictionary<string, string> { { "Yes", "Yes1" }, { "No", "No1" } }));

            PowerFxConfig config = new PowerFxConfig();

            bool fOk = config.SymbolTable.TryGetSymbolType("os1", out var type);
            Assert.False(fOk);

            config.AddOptionSet(os);

            fOk = config.SymbolTable.TryGetSymbolType("os1", out type);
            Assert.True(fOk);

            AssertOptionSetType(type, os);

            // Case sensitivity 
            fOk = config.SymbolTable.TryGetSymbolType("OS1", out type);
            Assert.False(fOk); // case sensitive

            // Consistent with SymbolNames
            var names = config.SymbolTable.SymbolNames.ToArray();
            Assert.Single(names);
            var name0 = names[0];
            Assert.Equal("os1", name0.Name);
            Assert.Equal(string.Empty, name0.DisplayName);
            AssertOptionSetType(name0.Type, os);

            // Composed tables.
            var st1 = new SymbolTable();
            var st2 = ReadOnlySymbolTable.Compose(st1, config.SymbolTable);

            fOk = st1.TryGetSymbolType("os1", out type);
            Assert.False(fOk);

            fOk = st2.TryGetSymbolType("os1", out type);
            Assert.True(fOk);
        }

        // Reads should be thread safe. 
        [Fact]
        public void Threading()
        {
            int nLoops = 100; 

            for (int i = 0; i < nLoops; i++)
            {
                // Create complex tree of composed tables. 
                var composed1a = new ComposedReadOnlySymbolTable(new SymbolTable(), new SymbolTable(), new SymbolTable(), new SymbolTable());
                var composed1b = new ComposedReadOnlySymbolTable(new SymbolTable(), new SymbolTable(), new SymbolTable(), new SymbolTable());

                var composed1 = new ComposedReadOnlySymbolTable(composed1a, composed1b);
                var composed2 = new ComposedReadOnlySymbolTable(new SymbolTable(), new SymbolTable(), new SymbolTable(), new SymbolTable());

                var composed = new ComposedReadOnlySymbolTable(composed1, composed2, new SymbolTable(), new SymbolTable());
                
                var engine = new Engine();

                // Reads should be thread-safe. 
                Parallel.For(
                    0,
                    2,
                    (i) =>
                    {
                        Assert.True(engine.Check("Sum(1)", symbolTable: composed).IsSuccess);
                    });
            }
        }

        [Fact]
        public void Cache1()
        {
            var table1 = new SymbolTable { DebugName = "Table1" };
            var table2 = new SymbolTable { DebugName = "Table2" };

            ComposedSymbolTableCache cache = new ComposedSymbolTableCache();
            var c1 = cache.GetComposedCached(table1, table2);

            // Same args, cache returns identical instance
            {
                var c1b = cache.GetComposedCached(table1, table2);

                Assert.True(object.ReferenceEquals(c1, c1b));
            }

            // Mutating an existing table is ok - ComposedSymbolTable will catch that
            {
                table1.AddFunction(new BlankFunction());
                var c1c = cache.GetComposedCached(table1, table2);
                Assert.True(object.ReferenceEquals(c1, c1c));

                bool hasFunc = c1c.Functions.AnyWithName("Blank");
                Assert.True(hasFunc);
            }

            // But using different table instances will invalidate
            {
                var table1b = new SymbolTable { DebugName = "Table1b" };
                Assert.False(object.ReferenceEquals(table1, table1b)); // different instances!!!

                var c2 = cache.GetComposedCached(table1b, table2);
                Assert.False(object.ReferenceEquals(c1, c2));

                var hasFunc = c2.Functions.AnyWithName("Blank");
                Assert.False(hasFunc);
            }
        }

        // We can't change the lenght to GetComposedCached()
        [Fact]
        public void CacheLengthChange()
        {
            var table1 = new SymbolTable { DebugName = "Table1" };
            var table2 = new SymbolTable { DebugName = "Table2" };

            ComposedSymbolTableCache cache = new ComposedSymbolTableCache();
            var c1 = cache.GetComposedCached(table1);

            // Can't change 
            Assert.Throws<InvalidOperationException>(() => cache.GetComposedCached(table1, table2));
        }

        // Since it's ok to pass nulls to ComposedReadOnlySymbolTable,
        // It's ok to pass nulls to GetComposedCached. 
        [Fact]
        public void CacheNullOk()
        {
            var table1 = new SymbolTable { DebugName = "Table1" };
            table1.AddConstant("c1", FormulaValue.New("constant"));
            ReadOnlySymbolTable nullTable = null;
            
            ComposedSymbolTableCache cache = new ComposedSymbolTableCache();
            var c1 = cache.GetComposedCached(table1, nullTable);

            var ok = c1.TryGetSymbolType("c1", out var type);
            Assert.True(ok);
            Assert.Equal(FormulaType.String, type);

            // Shorter args 
            ComposedSymbolTableCache cache1arg = new ComposedSymbolTableCache();
            cache1arg.GetComposedCached(nullTable);

            // Shorter args 
            ComposedSymbolTableCache cache0args = new ComposedSymbolTableCache();
            cache0args.GetComposedCached();
        }

        // Type is wrong: https://github.com/microsoft/Power-Fx/issues/2342
        // Option Set returns as a record. 
        private void AssertOptionSetType(FormulaType actualType, OptionSet expected)
        {
            RecordType recordType = (RecordType)actualType;
            var actualNames = recordType.FieldNames.OrderBy(x => x).ToArray();
            
            var expectedNames = expected.Options.Select(kv => kv.Key.Value).OrderBy(x => x).ToArray();

            Assert.True(actualNames.SequenceEqual(expectedNames));
        }

        // IsDefined also maps display names. 
        [Fact]
        public void TryGetTypeFindsDisplayNames()
        {
            var st = new SymbolTable();
            st.AddVariable("var1", FormulaType.Number, displayName: "Display1");

            var ok = st.TryGetSymbolType("var1", out var type);
            Assert.True(ok);

            // not display names 
            ok = st.TryGetSymbolType("Display1", out type);
            Assert.True(ok);
        }
    }
}
