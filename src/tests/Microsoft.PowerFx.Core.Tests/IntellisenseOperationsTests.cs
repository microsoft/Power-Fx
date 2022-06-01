// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class IntellisenseOperationsTests : PowerFxTest
    {
        [Fact]
        public void CheckIf()
        {
            var formula = "If(!IsBlank(X), \"Hello \" & X & \"!\", \"Hello world!\")";

            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var formulaParams = new RecordType();
            formulaParams = formulaParams.Add("X", FormulaType.String);

            var parseResult = engine.Parse(formula);
            Assert.True(parseResult.IsSuccess);

            var checkResult = engine.Check(parseResult, formulaParams);
            Assert.True(checkResult.IsSuccess);

            // Check existing args
            var args = parseResult.Root.AsCall().Args.ChildNodes;
            var result = checkResult.ValidateInvocation("If", args, out var returnType);
            Assert.True(result);
            Assert.Equal(FormulaType.String, returnType);

            // Swap args
            var args2 = new[] { args[0], args[2], args[1] };
            var result2 = checkResult.ValidateInvocation("If", args2, out var returnType2);
            Assert.True(result2);
            Assert.Equal(FormulaType.String, returnType2);

            // Take sub-tree
            var args3 = new[] { args[0], args[1].AsBinaryOp().Right, args[2] };
            var result3 = checkResult.ValidateInvocation("If", args3, out var returnType3);
            Assert.True(result3);
            Assert.Equal(FormulaType.String, returnType3);

            // Remove one arg
            var args4 = new[] { args[0], args[2] };
            var result4 = checkResult.ValidateInvocation("If", args4, out var returnType4);
            Assert.True(result4);
            Assert.Equal(FormulaType.String, returnType4);

            // Remove two args
            var args5 = new[] { args[1] };
            var result5 = checkResult.ValidateInvocation("If", args5, out var returnType5);
            Assert.False(result5);
            Assert.Null(returnType5); // Not part of contract, but we expect this to be null

            // Use different (invalid) function
            var args6 = args;
            var result6 = checkResult.ValidateInvocation("CountIf", args6, out var returnType6);
            Assert.False(result6);
            Assert.Null(returnType6);

            // Use different (valid) function
            var args7 = args;
            var result7 = checkResult.ValidateInvocation("IfError", args7, out var _);
            Assert.True(result7);
        }

        [Theory]
        [InlineData("SUM(X)")]
        [InlineData("SUM(1)")]
        [InlineData("SUM(X, 1)")]
        [InlineData("SUM(Y, X)")]
        [InlineData("SUM(Y, X, Y)")]
        [InlineData("SUM(T)")]
        [InlineData("SUM(T, A + B)")]
        [InlineData("SUM(T, A + B, 1)")]
        public void CheckOverloads(string formula)
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var tableType =
                new TableType().Add(new NamedFormulaType("A", FormulaType.Number))
                               .Add(new NamedFormulaType("B", FormulaType.Number));
            var formulaParams =
                new RecordType().Add("X", FormulaType.Number).Add("Y", FormulaType.Number).Add("Table", tableType);

            var parseResult = engine.Parse(formula);
            var args = parseResult.Root.AsCall().Args.ChildNodes;
            var fnc = parseResult.Root.AsCall().Head;
            var fncName = fnc.Namespace.IsRoot ? fnc.Name.Value : $"{fnc.Namespace.ToDottedSyntax()}.{fnc.Name.Value}";

            Assert.True(parseResult.IsSuccess);

            var checkResult = engine.Check(parseResult, formulaParams);
            var expectedType = checkResult.IsSuccess ? checkResult.ReturnType : null;
            var validateResult = checkResult.ValidateInvocation(fncName, args, out var retType);

            Assert.Equal(checkResult.IsSuccess, validateResult);
            Assert.Equal(expectedType, retType);
        }

        [Fact]
        public void CheckNamespace()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var formula = "true"; // does not matter what the formula is (not using arguments from it)
            var checkResult = engine.Check(formula);
            Assert.True(checkResult.IsSuccess);

            var result = checkResult.ValidateInvocation("Clock.AmPm", new TexlNode[0], out var _);
            Assert.True(result);
        }

        [Fact]
        public void InvalidFncNames()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var formula = "true"; // does not matter what the formula is (not using arguments from it)
            var checkResult = engine.Check(formula);
            Assert.True(checkResult.IsSuccess);

            var result = checkResult.ValidateInvocation("invalid fnc name", new TexlNode[0], out var _);
            Assert.False(result);
        }

        [Fact]
        public void InvalidNodes()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var formula1 = "If(true, false, true)";
            var parseResult1 = engine.Parse(formula1);
            Assert.True(parseResult1.IsSuccess);
            var args1 = parseResult1.Root.AsCall().Args.ChildNodes;

            var formula2 = "If(false, true, false)";
            var parseResult2 = engine.Parse(formula2);
            Assert.True(parseResult2.IsSuccess);
            var checkResult2 = engine.Check(parseResult2);
            var args2 = parseResult2.Root.AsCall().Args.ChildNodes;

            var mixedNodes = new[] { args2[0], args1[0], args2[1] };
            Assert.Throws<ArgumentException>(() => checkResult2.ValidateInvocation("If", mixedNodes, out _));
        }

        [Theory]
        [InlineData("normalFnc", "normalFnc")]
        [InlineData("ns1.normalFnc", "normalFnc", "ns1")]
        [InlineData("ns1.ns2.someFnc", "someFnc", "ns1.ns2")]
        [InlineData("'escaped fnc name'", "escaped fnc name")]
        [InlineData("ns1.'escaped fnc name'", "escaped fnc name", "ns1")]
        [InlineData("ns1.'escaped namespace'.'escaped fnc name'", "escaped fnc name", "ns1.'escaped namespace'")]
        [InlineData("invalid fnc", null)]
        [InlineData("ns1.", null)]
        [InlineData(".fnc", null)]
        [InlineData("abc(", null)]
        public void FunctionNameParse(string fncName, string expectedName, string expectedNs = "")
        {
            var expectedResult = expectedName != null;

            var result = IntellisenseOperations.TryParseFunctionNameWithNamespace(fncName, out var ident);
            Assert.Equal(expectedResult, result);

            if (result)
            {
                Assert.Equal(expectedName, ident.Name.Value);
                Assert.Equal(expectedNs, ident.Namespace.ToDottedSyntax());
            }
            else
            {
                Assert.Null(ident);
            }
        }
    }

    internal static class ValidateUtils
    {
        public static bool ValidateInvocation(
            this CheckResult result,
            string fncName,
            IReadOnlyList<TexlNode> args,
            out FormulaType retType) => new IntellisenseOperations(result).ValidateInvocation(fncName, args, out retType);
    }
}
