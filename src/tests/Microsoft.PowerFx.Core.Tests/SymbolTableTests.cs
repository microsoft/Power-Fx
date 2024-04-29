﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
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
