// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
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

        // Changing the config changes its hash
        [Fact]
        public void ConfigHash()
        {
            var set = new HashSet<VersionHash>();

            var s1 = new SymbolTable();
            AssertUnique(set, s1);

            s1.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s1);

            s1.RemoveVariable("x");
            AssertUnique(set, s1);

            // Same as before, but should still be unique VersionHash!
            s1.AddVariable("x", FormulaType.Number);
            AssertUnique(set, s1);

            // $$$ Test Parent, other mutations. 
        }

        [Fact]
        public void Overwrite()
        {
            var s1 = new SymbolTable();
            s1.AddVariable("x", FormulaType.Number);

            // Can't overwrite (even with same type)
            var v1 = s1.VersionHash;
            Assert.Throws<ArgumentException>(() => s1.AddVariable("x", FormulaType.Number));

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
    }
}
