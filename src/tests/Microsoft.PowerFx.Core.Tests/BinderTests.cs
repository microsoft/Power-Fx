// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class BinderTests : PowerFxTest
    {
        [Theory]
        [InlineData("x")]
        [InlineData("x.Value")]
        [InlineData("x.Tbl")]
        [InlineData("First(x.Tbl)")]
        [InlineData("First(x.Tbl).Value2")]
        [InlineData("Last(x.Tbl).Value2")]
        [InlineData("Index(x.Tbl, 3).Value2")]
        [InlineData("x.Rec")]
        [InlineData("x.Rec.Value3")]
        [InlineData("tbl")]
        [InlineData("First(tbl)")]
        [InlineData("Last(tbl)")]
        [InlineData("Index(tbl, 3)")]
        [InlineData("MyDataSource")]
        public void TestMutableNodes(string expression)
        {
            var config = new PowerFxConfig();
            config.SymbolTable.AddVariable(
                "x",
                new KnownRecordType(TestUtils.DT("![Value:n,Tbl:*[Value2:n],Rec:![Value3:n,Value4:n]]")),
                mutable: true);
            config.SymbolTable.AddVariable(
                "tbl",
                TableType.Empty().Add("Value", FormulaType.Number),
                mutable: true);
            var schema = DType.CreateTable(
                new TypedName(DType.Guid, new DName("ID")),
                new TypedName(DType.Number, new DName("Value")));
            config.SymbolTable.AddEntity(new TestDataSource("MyDataSource", schema));

            var engine = new Engine(config);
            var checkResult = engine.Check(expression);
            Assert.True(checkResult.IsSuccess);
            var binding = checkResult.Binding;
            Assert.True(binding.IsMutable(binding.Top));
        }

        [Theory]
        [InlineData("nf")]
        [InlineData("const")]
        [InlineData("FirstN(x.Tbl, 3).Value2")]
        [InlineData("LastN(x.Tbl, 3).Value2")]
        [InlineData("Filter(tbl, Value > 10)")]
        [InlineData("FirstN(tbl, 2)")]
        [InlineData("LastN(tbl, 2)")]
        [InlineData("First(MyDataSource)")]
        public void TestImmutableNodes(string expression)
        {
            var config = new PowerFxConfig();
            config.SymbolTable.AddConstant("const", new NumberValue(IR.IRContext.NotInSource(FormulaType.Number), 2));
            config.SymbolTable.AddVariable("nf", new TableType(TestUtils.DT("*[Value:n]")), mutable: false);
            config.SymbolTable.AddVariable(
                "x",
                new KnownRecordType(TestUtils.DT("![Value:n,Tbl:*[Value2:n],Rec:![Value3:n,Value4:n]]")),
                mutable: true);
            config.SymbolTable.AddVariable(
                "tbl",
                TableType.Empty().Add("Value", FormulaType.Number),
                mutable: true);
            var schema = DType.CreateTable(
                new TypedName(DType.Guid, new DName("ID")),
                new TypedName(DType.Number, new DName("Value")));
            config.SymbolTable.AddEntity(new TestDataSource("MyDataSource", schema));

            var engine = new Engine(config);
            var checkResult = engine.Check(expression);
            Assert.True(checkResult.IsSuccess);
            var binding = checkResult.Binding;
            Assert.False(binding.IsMutable(binding.Top));
        }

        private class FakePatchFunction : BuiltinFunction
        {
            public FakePatchFunction()
                : base("Patch", (x) => "Patch", FunctionCategories.Table, DType.EmptyRecord, 0, 3, 3)
            {
            }

            public override bool IsSelfContained => false;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                return Enumerable.Empty<TexlStrings.StringGetter[]>();
            }
        }

        [Fact]
        public void TestImmutableNodesWithScopeVariable()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            config.SymbolTable.AddVariable("tableVar", new TableType(TestUtils.DT("*[Value:n]")), mutable: true);
            config.AddFunction(new FakePatchFunction());

            var checkResult = engine.Check("With({x:[1,2,3]},x)");
            Assert.True(checkResult.IsSuccess);
            var binding = checkResult.Binding;
            var withCallNode = binding.Top.AsCall();
            var xScopeVarNode = withCallNode.Args.ChildNodes[1];
            Assert.False(binding.IsMutable(xScopeVarNode));

            var parserOptions = new ParserOptions { AllowsSideEffects = true };
            checkResult = engine.Check("With({a:1},Patch(tableVar,{Value:a},{Value:a+1}))", parserOptions);
            Assert.True(checkResult.IsSuccess);
            binding = checkResult.Binding;
            withCallNode = binding.Top.AsCall();
            var patchCallNode = withCallNode.Args.ChildNodes[1].AsCall();
            var tableVarNode = patchCallNode.Args.ChildNodes[0];
            Assert.True(binding.IsMutable(tableVarNode));

            var valueANode = patchCallNode.Args.ChildNodes[1].AsRecord();
            var aNode = valueANode.ChildNodes[0];
            Assert.False(binding.IsMutable(aNode));
        }
    }
}
