// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Tests.IntellisenseTests;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Tests.CustomFunctions;

namespace Microsoft.PowerFx.Interpreter.Tests.CustomFunction
{
    public class CustomFunctionSignatureTests
    {
        [Theory]
        [InlineData("Overload(|", 10001)]
        [InlineData("UserAsync(|", 10002)]
        public void TestCustomFunctionSignature(string expression, int helpId)
        {
            var config = new PowerFxConfig();

            // Overloads
            config.AddFunction(new TestOverload1());
            config.AddFunction(new TestOverload2());

            // With Config Param
            config.AddFunction(new UserAsyncFunction());

            var engine = new RecalcEngine(config);

            var signatureHelper = new SignatureHelpTest();
            (_, var cursorPosition) = IntellisenseTestBase.Decode(expression);
            
            signatureHelper.CheckSignatureHelpTest(engine.Suggest(expression, RecordType.Empty(), cursorPosition).SignatureHelp, helpId);          
        }

        private abstract class TestOverload : ReflectionFunction
        {
            public TestOverload(params FormulaType[] param)
                : base("Overload", FormulaType.String, param)
            {
            }
        }

        private class TestOverload1 : TestOverload
        {
            public TestOverload1() 
                : base(FormulaType.Number, FormulaType.String)
            {
            }

            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            public async Task<StringValue> Execute(NumberValue input1, StringValue input2, CancellationToken cancellationToken)
            {
                return FormulaValue.New("O1");
            }
        }

        private class TestOverload2 : TestOverload
        {
            public TestOverload2() 
                : base(FormulaType.String)
            {
            }

            // Must have "Execute" method. 
            // Cancellation Token must be the last argument for custom async function.
            public async Task<StringValue> Execute(StringValue input, CancellationToken cancellationToken)
            {
                return FormulaValue.New("O2");
            }
        }
    }
}
