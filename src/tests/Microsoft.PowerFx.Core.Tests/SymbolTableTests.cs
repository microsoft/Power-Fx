// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private static void AssertUnique(HashSet<VersionHash> set, SymbolTable symbolTable)
        {
            AssertUnique(set, symbolTable.VersionHash);
        }

        [Fact]
        public void Parent()
        {
            var s0 = new SymbolTable();
            var s1 = new SymbolTable
            {
                Parent = s0
            };
            
            ReadOnlySymbolTable r0 = s0;
            ReadOnlySymbolTable r1 = s1;

            Assert.Same(s1.Parent, s0);
            Assert.Same(r1.Parent, s0);
        }

        // Changing the config changes its hash
        [Fact]
        public void ConfigHash()
        {
            var set = new HashSet<VersionHash>();

            var s0 = new SymbolTable();
            AssertUnique(set, s0);

            var s1 = new SymbolTable
            {
                Parent = s0
            };
            AssertUnique(set, s1);

            s1.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s1);

            s1.RemoveVariable("x");
            AssertUnique(set, s1);

            // Same as before, but should still be unique VersionHash!
            s1.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s1);

            // Try other mutations
            s1.AddConstant("c", FormulaValue.New(1));
            AssertUnique(set, s1);

            // New function 
            var func = new PowerFx.Tests.BindingEngineTests.BehaviorFunction();
            var funcName = func.Name;
            s1.AddFunction(func);
            AssertUnique(set, s1);

            s1.RemoveFunction(funcName);
            AssertUnique(set, s1);

            s1.RemoveFunction(func);
            AssertUnique(set, s1);

            var optionSet = new OptionSet("foo", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() { { "one key", "one value" } }));
            s1.AddEntity(optionSet);
            AssertUnique(set, s1);

            // Adding to parent still changes our checksum (even if shadowed)
            s0.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s1);
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
            var s2 = new SymbolTable
            {
                Parent = s1
            };

            s2.AddVariable("x", FormulaType.Boolean);
            result = _engine.Check("x", symbolTable: s2);
            Assert.Equal(FormulaType.Boolean, result.ReturnType);
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

            var originalCount = symbolTableOriginal.Functions.Count();
            var copyCount1 = symbolTableCopy1.Functions.Count();
            var copyCount2 = symbolTableCopy2.Functions.Count();

            Assert.Equal(copyCount1, originalCount);
            Assert.Equal(copyCount2, originalCount);

            symbolTableCopy1.RemoveFunction("Abs");
            symbolTableCopy2.RemoveFunction("Day");

            Assert.Equal(originalCount, symbolTableOriginal.Functions.Count());
            Assert.Equal(copyCount1 - 2, symbolTableCopy1.Functions.Count());
            Assert.Equal(copyCount2 - 1, symbolTableCopy2.Functions.Count());

            Assert.NotEqual(copyCount1, symbolTableCopy1.Functions.Count());
            Assert.NotEqual(copyCount2, symbolTableCopy2.Functions.Count());

            Assert.True(symbolTableOriginal.Functions.Any("Abs"));
            Assert.True(symbolTableOriginal.Functions.Any("Day"));
            Assert.True(symbolTableOriginal.Functions.Any("Text"));
            Assert.True(symbolTableOriginal.Functions.Any("Value"));

            Assert.True(symbolTableCopy1.Functions.Any("Day"));
            Assert.True(symbolTableCopy1.Functions.Any("Text"));
            Assert.True(symbolTableCopy1.Functions.Any("Value"));

            Assert.True(symbolTableCopy2.Functions.Any("Abs"));
            Assert.True(symbolTableCopy2.Functions.Any("Text"));
            Assert.True(symbolTableCopy2.Functions.Any("Value"));

            Assert.False(symbolTableCopy1.Functions.Any("Abs"));
            Assert.False(symbolTableCopy2.Functions.Any("Day"));

            Assert.Same(symbolTableCopy1.Parent, symbolTableOriginal.Parent);
            Assert.Same(symbolTableCopy2.Parent, symbolTableOriginal.Parent);

            // Check if nothing else has been copied
            Assert.Empty(symbolTableCopy1.SymbolNames);
            Assert.Empty(symbolTableCopy2.SymbolNames);
        }
    }
}
