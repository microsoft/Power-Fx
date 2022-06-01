// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class BindingEngineTests : PowerFxTest
    {       
        [Fact]
        public void CheckSuccess()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var result = engine.Check(
                "3*2+x",
                new RecordType().Add(
                    new NamedFormulaType("x", FormulaType.Number)));

            Assert.True(result.IsSuccess);
            Assert.True(result.ReturnType is NumberType);
            Assert.Single(result.TopLevelIdentifiers);
            Assert.Equal("x", result.TopLevelIdentifiers.First());
        }

        // Parse and Bind separately. 
        [Fact]
        public void CheckParseSuccess()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var parse = engine.Parse("3*x");

            Assert.False(parse.HasError);
            Assert.Empty(parse.Errors);

            Assert.NotNull(parse.Root);

            var str = parse.Root.ToString();
            Assert.Equal("3 * x", str);

            var r = new RecordType().Add(
                   new NamedFormulaType("x", FormulaType.Number));
                        
            var check = engine.Check(parse, r);
            Assert.True(check.IsSuccess);

            // Can reuse Parse 
            var check2 = engine.Check(parse, r);
            Assert.True(check2.IsSuccess);
        }

        // Parse and Bind separately. 
        [Fact]
        public void CheckChainingParseSuccess()
        {
            var opts = new ParserOptions
            {  
                AllowsSideEffects = true
            };

            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var parse = engine.Parse("a;b;c", opts);

            Assert.False(parse.HasError);
            Assert.Empty(parse.Errors);

            Assert.NotNull(parse.Root);

            var str = parse.Root.ToString();
            Assert.Equal("a ; b ; c", str);
        }

        [Fact]
        public void CheckParseOnlyError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Parse("3*1+");

            Assert.True(result.HasError);
            Assert.Single(result.Errors);
                        
            AssertContainsError(result, "Error 4-4: Expected an operand");
        }

        [Fact]
        public void CheckParseError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("3*1+");

            Assert.False(result.IsSuccess);
            Assert.True(result.Errors.Count() >= 1);
            AssertContainsError(result, "Error 4-4: Expected an operand");
        }        

        [Fact]
        public void CheckParseErrorCommaSeparatedLocale()
        {
            var engine = new Engine(new PowerFxConfig(CultureInfo.GetCultureInfo("it-IT")));
            var result = engine.Parse("3.145");

            Assert.False(result.IsSuccess);
            Assert.StartsWith("Error 2-5: Unexpected character", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckParseSuccessCommaSeparatedLocale()
        {
            var engine = new Engine(new PowerFxConfig(CultureInfo.GetCultureInfo("de-DE")));
            var result = engine.Parse("Function(args; separated; by; semicolons) + 123,456");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckBindError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("3+foo+2"); // foo is undefined

            Assert.False(result.IsSuccess);
            AssertContainsError(result, "Error 2-5: Name isn't valid. 'foo' isn't recognized");
        }

        [Fact]
        public void CheckBadFunctionError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("abc(123)");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 0-8: 'abc' is an unknown or unsupported function.");
        }

        [Fact]
        public void CheckBadNamespaceFunctionError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("abc.def(123)");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 0-12: 'def' is an unknown or unsupported function in namespace 'abc'.");
        }

        [Fact]
        public void CheckLambdaBindError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("Filter([1,2,3] As X, X.Value > foo)");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 31-34: Name isn't valid. 'foo' isn't recognized");
        }

        [Fact]
        public void CheckDottedBindError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("[1,2,3].foo");

            Assert.False(result.IsSuccess);
            AssertContainsError(result, "Error 7-11: Name isn't valid. 'foo' isn't recognized");
        }

        [Fact]
        public void TableInRegressionError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var formula = "Table() in \"\"";
            var result = engine.Check(formula);

            Assert.False(result.IsSuccess);
            AssertContainsError(result, "Error 0-7: Invalid argument type. Cannot use Table values in this context.");
        }

        private void AssertContainsError(IOperationStatus result, string errorMessage)
        {
            Assert.Contains(result.Errors, x => x.ToString().StartsWith(errorMessage));
        }
    }
}
