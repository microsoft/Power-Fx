// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class CheckInvocationTests : PowerFxTest
    {
        [Fact]
        public void CheckIf()
        {
            // TODO: Split into multiple tests? Need to replicate common stuff?

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
            var result5 = checkResult.ValidateInvocation("If", args5, out _);
            Assert.False(result5);

            // Use different (invalid) function
            var args6 = args;
            var result6 = checkResult.ValidateInvocation("CountIf", args6, out _);
            Assert.False(result6);

            // Use different (valid) function
            var args7 = args;
            var result7 = checkResult.ValidateInvocation("IfError", args7, out _);
            Assert.True(result7);
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

            var mixedNodes = new[] { args1[0], args2[0], args1[1] };
            checkResult2.ValidateInvocation("If", mixedNodes, out _);
        }
    }
}
