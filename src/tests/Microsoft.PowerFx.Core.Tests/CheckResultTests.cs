﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Core.Tests
{
    public class CheckResultTests
    {
        // A non-default culture that  uses comma as a decimal separator
        private static readonly CultureInfo _frCulture = new CultureInfo("fr-FR");
        private static readonly ParserOptions _frCultureOpts = new ParserOptions { Culture = _frCulture };
        private static readonly ParserOptions _numberIsFloatOpts = new ParserOptions { NumberIsFloat = true };

        [Fact]
        public void Ctors()
        {
            // CheckResult must have an engine so that it can invoke operations.
            Assert.Throws<ArgumentNullException>(() => new CheckResult((Engine)null));
            Assert.Throws<ArgumentNullException>(() => new CheckResult((IEnumerable<ExpressionError>)null));
        }

        [Fact]
        public void Errors()
        {
            var error = new ExpressionError { Message = "MyError" };
            var errors = new[] { error };

            var check = new CheckResult(errors);
            Assert.False(check.IsSuccess); // Initialized with errors. 
            Assert.Throws<InvalidOperationException>(() => check.ThrowOnErrors());

            var errors2 = check.Errors;
            Assert.Single(errors2);
            Assert.Same(error, errors2.First());
        }

        [Fact]
        public void EmptyErrors()
        {
            var errorList = new List<ExpressionError>();

            var check = new CheckResult(errorList); // 0-length is ok 
            Assert.True(check.IsSuccess); // Initialized with errors. 
            check.ThrowOnErrors();

            // Takes snapshot - so adding new errors doesn't change. 
            errorList.Add(new ExpressionError { Message = "new error" });

            Assert.Empty(check.Errors);

            // Other operations will now fail
            var parse = ParseResult.ErrorTooLarge("abc", 2); // any parse result

            Assert.Throws<InvalidOperationException>(() => check.SetText("1+2"));
            Assert.Throws<InvalidOperationException>(() => check.SetText(parse));
            Assert.Throws<InvalidOperationException>(() => check.SetBindingInfo());
        }

        [Fact]
        public void ExtraErrors()
        {
            var engine = new ExtraErrorsEngine();
            var check = new CheckResult(engine);

            Assert.True(check.IsSuccess); // no errors so far. 
            check.ThrowOnErrors();

            check.SetText("1+2");

            check.SetBindingInfo();
            check.ApplyBinding();
            Assert.True(check.IsSuccess); // no errors so far. 

            check.ApplyErrors(); // do final pass for per-engine restrictions. 

            Assert.False(check.IsSuccess);

            // Errors include custom errors from PostCheck
            var errors2 = check.Errors;
            Assert.Single(errors2);
            Assert.Same(ExtraErrorsEngine._error, errors2.First());
        }

        private class ExtraErrorsEngine : Engine
        {
            public static readonly ExpressionError _error = new ExpressionError
            {
                Message = "Extra"
            };

            protected override IEnumerable<ExpressionError> PostCheck(CheckResult check)
            {
                yield return _error;
            }
        }

        [Fact]
        public void BasicParse()
        {
            var engine = new Engine(new PowerFxConfig());
            var check = new CheckResult(engine);

            // Must Set before we parse
            Assert.Throws<InvalidOperationException>(() => check.ApplyParse());
            Assert.Throws<InvalidOperationException>(() => check.GetParseFormula());

            check.SetText("1+2");

            Assert.Throws<ArgumentNullException>(() => check.SetText((string)null));

            // Can't Set twice. 
            Assert.Throws<InvalidOperationException>(() => check.SetText("1+2"));

            check.ApplyParse();

            var parse = check.Parse;
            Assert.NotNull(parse);

            Assert.True(check.IsSuccess);

            var formula = check.GetParseFormula();
            Assert.NotNull(formula);
        }

        [Fact]
        public void ParseResultTest()
        {
            var check = new CheckResult(new Engine());
            Assert.Throws<ArgumentNullException>(() => check.SetText((ParseResult)null));

            var parseResult = Engine.Parse("1+2");
            check.SetText(parseResult);
            check.ApplyParse();

            Assert.Throws<InvalidOperationException>(() => check.SetText(parseResult));

            Assert.Same(parseResult, check.Parse);
        }

        [Fact]
        public void BasicParseErrors()
        {
            var check = new CheckResult(new Engine());
            check.SetText("1+"); // parse error

            var parse = check.ApplyParse();
            Assert.NotNull(parse);
            Assert.False(parse.IsSuccess);

            Assert.False(check.IsSuccess);

            // Can't set Binding if we called ApplyParse 
            Assert.Throws<InvalidOperationException>(() => check.SetBindingInfo());
        }

        [Theory]
        [InlineData("1+", true)]
        [InlineData("1+", false)]
        public void BasicParseErrors2(string expr, bool numberIsFloat)
        {
            var check = new CheckResult(new Engine());
            check.SetText(expr, numberIsFloat ? _numberIsFloatOpts : null); // parse error

           // Can still try to bind even with parse errors. 
            // But some information like Returntype isn't computed.
            check.SetBindingInfo();
            var parse = check.ApplyParse();
            Assert.NotNull(parse);
            Assert.False(parse.IsSuccess);

            Assert.False(check.IsSuccess);

            check.ApplyBinding();
            Assert.NotNull(check.Binding);
            Assert.Null(check.ReturnType);

            // Still assign some types
            var node = ((BinaryOpNode)parse.Root).Left;
            var type = check.GetNodeType(node);
            Assert.Equal(numberIsFloat ? FormulaType.Number : FormulaType.Decimal, type);
        }

        // Ensure we can pass in ParserOptions. 
        [Fact]
        public void ParserOptions()
        {
            var check = new CheckResult(new Engine());
            var opts = _frCultureOpts;
            check.SetText("1,234", opts); // , is decimal separator for fr-FR.

            var parse = check.ApplyParse();
            Assert.Same(opts, parse.Options);

            Assert.True(check.IsSuccess);
            var value = ((DecLitNode)parse.Root).ActualDecValue;
            Assert.Equal(1.234m, value);
        }

        [Fact]
        public void Binding()
        {
            SymbolTable symbolTable = new SymbolTable();
            symbolTable.AddVariable("x", FormulaType.Number);

            var check = new CheckResult(new Engine());

            Assert.Throws<InvalidOperationException>(() => check.ApplyBinding());

            check.SetText("x+1");
            check.SetBindingInfo(symbolTable);

            Assert.Throws<InvalidOperationException>(() => check.SetBindingInfo(symbolTable));

            var parse = check.ApplyParse();

            // Must call ApplyBinding before getting binding data
            var node = ((BinaryOpNode)parse.Root).Left;
            Assert.Throws<ArgumentNullException>(() => check.GetNodeType(null));
            Assert.Throws<InvalidOperationException>(() => check.GetNodeType(node));
            Assert.Throws<InvalidOperationException>(() => check.Binding);

            check.ApplyBinding();
            Assert.NotNull(check.Binding);

            // Future calls to ApplyBinding() are nops and return the already-computed binding.
            Assert.Same(check.Binding, check.ApplyBindingInternal());

            Assert.Equal(FormulaType.Number, check.ReturnType);

            // Binding doesn't compute dependency analysis. 
            // These are other Apply* calls. 
            Assert.Throws<InvalidOperationException>(() => check.TopLevelIdentifiers);
            check.ApplyDependencyAnalysis();
            Assert.NotNull(check.TopLevelIdentifiers);
        }

        [Fact]
        public void BindingChangeSymbols()
        {
            SymbolTable symbolTable = new SymbolTable();
            symbolTable.AddVariable("x", FormulaType.Number);

            var check = new CheckResult(new Engine());
            check.SetText("x+1");
            check.SetBindingInfo(symbolTable);

            // Now mutate! This is illegal.
            // Can still parse, but binding will fail. 
            symbolTable.AddVariable("y", FormulaType.Number);

            check.ApplyParse();

            Assert.Throws<InvalidOperationException>(() => check.ApplyBinding());
        }

        [Theory]
        [InlineData("\"test string\"", false, true, "string", "")]
        [InlineData("\"test string\"", true, true, "string", "")]
        [InlineData("12", false, false, "decimal", "The type of this expression does not match the expected type 'Text'")]       
        [InlineData("12", true, true, "decimal", "")]
        [InlineData("{a:12, b:15}", true, false, "record", "The type of this expression does not match the expected type 'Text, Number, Decimal, DateTime, Date, Time, Boolean, Guid'")]
        [InlineData("{a:12, b:15}", false, false, "record", "The type of this expression does not match the expected type 'Text'")]
        public void CheckResultExpectedReturnValueString(string inputExpr, bool allowCoerceTo, bool isSuccess, string expectedType, string errorMsg)
        {
            CheckResultExpectedReturnValue(inputExpr, allowCoerceTo, isSuccess, errorMsg, FormulaType.String, GetFormulaType(expectedType));
        }

        [Theory]
        [InlineData("12", false, false, "decimal", "")]
        [InlineData("12", true, true, "decimal", "")]
        [InlineData("\"test string\"", true, true, "string", "")]
        [InlineData("\"test string\"", false, false, "string", "The type of this expression does not match the expected type 'Number'")]
        [InlineData("{a:12, b:15}", true, false, "record", "The type of this expression does not match the expected type 'Text, Number, DateTime, Date, Boolean, Decimal'")]
        [InlineData("{a:12, b:15}", false, false, "record", "The type of this expression does not match the expected type 'Number'")]
        public void CheckResultExpectedReturnValueNumber(string inputExpr, bool allowCoerceTo, bool isSuccess, string expectedType, string errorMsg)
        {
            CheckResultExpectedReturnValue(inputExpr, allowCoerceTo, isSuccess, errorMsg, FormulaType.Number, GetFormulaType(expectedType));
        }

        [Theory]
        [InlineData("23.45", false, true, "decimal", "")]
        [InlineData("23.45", true, true, "decimal", "")]
        [InlineData("{a:12, b:15}", true, false, "record", "The type of this expression does not match the expected type 'Text, Number, Decimal, DateTime, Date, Boolean'")]
        [InlineData("{a:12, b:15}", false, false, "record", "The type of this expression does not match the expected type 'Decimal'")]
        public void CheckResultExpectedReturnValueDecimal(string inputExpr, bool allowCoerceTo, bool isSuccess, string expectedType, string errorMsg)
        {
            CheckResultExpectedReturnValue(inputExpr, allowCoerceTo, isSuccess, errorMsg, FormulaType.Decimal, GetFormulaType(expectedType));
        }

        [Theory]
        [InlineData("1.2", true, "decimal", "")]
        [InlineData("203", true, "decimal", "")]
        [InlineData("\"12\"", false, "string", "The type of this expression does not match the expected type 'Number, Decimal'")]
        [InlineData("{a:1, b:2}", false, "record", "The type of this expression does not match the expected type 'Number, Decimal'")]
        public void CheckResultExpectedReturnValueNumberDecimal(string inputExp, bool isSuccess, string expectedType, string errorMsg)
        {
            var expectedReturnTypes = new FormulaType[] { FormulaType.Number, FormulaType.Decimal };
            CheckResultExpectedReturnTypes(inputExp, isSuccess, errorMsg, expectedReturnTypes, GetFormulaType(expectedType));
        }

        [Theory]
        [InlineData("1.2", true, "decimal", "")]
        [InlineData("203", true, "decimal", "")]
        [InlineData("\"12\"", true, "string", "")]
        [InlineData("{a:1, b:2}", false, "record", "The type of this expression does not match the expected type 'Number, Decimal, Text'")]
        public void CheckResultExpectedReturnValueNumberDecimalString(string inputExp, bool isSuccess, string expectedType, string errorMsg)
        {
            var expectedReturnTypes = new FormulaType[] { FormulaType.Number, FormulaType.Decimal, FormulaType.String };
            CheckResultExpectedReturnTypes(inputExp, isSuccess, errorMsg, expectedReturnTypes, GetFormulaType(expectedType));
        }

        [Fact]
        public void BindingSetRecordType()
        {
            var check = new CheckResult(new Engine());
            check.SetText("1+x");
            check.SetBindingInfo(RecordType.Empty().Add("x", FormulaType.Number));

            check.ApplyBinding();
            Assert.Equal(FormulaType.Number, check.ReturnType);
            Assert.Equal(FormulaType.Number, check.GetNodeType(check.Parse.Root));
        }

        [Fact]
        public void BindingSymbols()
        {
            // Test Symbol property. 
            var config = new PowerFxConfig();
            config.SymbolTable.AddVariable("Global1", FormulaType.Number);

            var localSymbols = new SymbolTable { DebugName = "Locals" };
            localSymbols.AddVariable("Local1", FormulaType.Number);

            var check = new CheckResult(new Engine(config));

            check.SetText("Global1 + Local1 +"); // has error.
            check.SetBindingInfo(localSymbols);

            Assert.Throws<InvalidOperationException>(() => check.Symbols);

            check.ApplyBinding();
            Assert.False(check.IsSuccess); // Still have symbols even on binding errors. 
            
            // Validate symbol table.
            var allSymbols = check.Symbols;
            Assert.NotNull(allSymbols);

            var ok = allSymbols.TryLookupSlot("Global1", out var slotGlobal);
            Assert.True(ok);
            Assert.Same(config.SymbolTable, slotGlobal.Owner);

            ok = allSymbols.TryLookupSlot("Local1", out var slotLocal);
            Assert.True(ok);
            Assert.Same(localSymbols, slotLocal.Owner);            
        }

        // Still have Symbols even if we thing it's empty 
        [Fact]
        public void BindingSymbolsEmpty()
        {
            var check = new CheckResult(new Engine());
            check.SetText("1+2");
            check.SetBindingInfo();

            check.ApplyBinding();

            var allSymbols = check.Symbols;
            Assert.NotNull(allSymbols);
        }

        [Fact] 
        public void TestIR()
        {
            var check = new CheckResult(new Engine());

            Assert.Throws<InvalidOperationException>(() => check.ApplyIR());
            check.SetText("1+2");
            check.SetBindingInfo();

            var ir = check.ApplyIR();
            Assert.NotNull(ir);
            Assert.Equal("AddDecimals:w(1:w, 2:w)", ir.TopNode.ToString());
        }

        // IR can only be produced for successful bindings
        [Fact]
        public void TestIRFail()
        {
            var check = new CheckResult(new Engine());

            Assert.Throws<InvalidOperationException>(() => check.ApplyIR());
            check.SetText("1+x");
            check.SetBindingInfo();

            check.ApplyBinding();
            Assert.False(check.IsSuccess);

            Assert.Throws<InvalidOperationException>(() => check.ApplyIR());
        }

        // CheckResult properly wired up to invariant translator. 
        // More tests at DisplayNameTests
        [Fact]
        public void TestApplyGetInvariant()
        {
            var check = new CheckResult(new Engine());

            Assert.Throws<InvalidOperationException>(() => check.ApplyGetInvariant());
        }

        // CheckResult properly wired up to invariant translator. 
        // More tests at DisplayNameTests
        [Fact]
        public void TestApplyGetInvariant2()
        {
            var check = new CheckResult(new Engine());

            var r1 = RecordType.Empty()
              .Add(new NamedFormulaType("new_field", FormulaType.Number, "Field"));

            // display name: Field --> new_field
            // lexer locale: 2,3 --> 2.3
            check.SetText("Field + 2,3", _frCultureOpts);
            Assert.Throws<InvalidOperationException>(() => check.ApplyGetInvariant());
        }

        // CheckResult properly wired up to invariant translator. 
        // More tests at DisplayNameTests
        [Fact]
        public void TestApplyGetInvariant3()
        {
            var check = new CheckResult(new Engine());

            var r1 = RecordType.Empty()
              .Add(new NamedFormulaType("new_field", FormulaType.Number, "Field"));

            // display name: Field --> new_field
            // lexer locale: 2,3 --> 2.3
            check.SetText("Field + 2,3", _frCultureOpts);

            check.SetBindingInfo(r1);
            var invariant = check.ApplyGetInvariant();

            Assert.Equal("new_field + 2.3", invariant);
        }

        // CheckResult properly wired up to Apply logging. 
        [Theory]
        [InlineData("123+abc", "#$decimal$# + #$firstname$#", true)] // display names
        [InlineData("123+", "#$decimal$# + #$error$#", false)] // error 
        [InlineData("123,456", "#$decimal$#", true)] // locales 
        [InlineData("Power(2,3)", "Power(#$decimal$#)", true)] // functions aren't Pii
        public void TestApplyGetLogging(string expr, string execptedLog, bool success)
        {
            var check = new CheckResult(new Engine());

            Assert.Throws<InvalidOperationException>(() => check.ApplyGetLogging());

            // Only requires text, not binding
            check.SetText(expr, _frCultureOpts);
            var log = check.ApplyGetLogging();
            Assert.Equal(success, check.IsSuccess);
            Assert.Equal(execptedLog, log);
        }

        private void CheckResultExpectedReturnValue(string inputExpr, bool allowCoerceTo, bool isSuccess, string errorMsg, FormulaType returnType, FormulaType expectedType)
        {
            var check = new CheckResult(new Engine())
                .SetText(inputExpr)
                .SetBindingInfo()
                .SetExpectedReturnValue(returnType, allowCoerceTo);

            check.ApplyBinding();

            CheckExpectedReturn(check, isSuccess, errorMsg, expectedType);
        }

        private void CheckResultExpectedReturnTypes(string inputExpr, bool isSuccess, string errorMsg, FormulaType[] returnTypes, FormulaType expectedType)
        {
            var check = new CheckResult(new Engine())
                .SetText(inputExpr)
                .SetBindingInfo()
                .SetExpectedReturnValue(returnTypes);

            check.ApplyBinding();

            CheckExpectedReturn(check, isSuccess, errorMsg, expectedType);
        }

        private void CheckExpectedReturn(CheckResult check, bool isSuccess, string errorMsg, FormulaType expectedType)
        {
            if (isSuccess)
            {
                Assert.True(check.IsSuccess);
                Assert.Equal(expectedType, check.ReturnType);
            }
            else
            {
                string exMsg = null;

                try
                {
                    var errors = check.ApplyErrors();
                    exMsg = errors.First().Message;
                    Assert.False(check.IsSuccess);
                }
                catch (Exception ex)
                {
                    exMsg = ex.ToString();
                }

                Assert.Contains(errorMsg, exMsg);
            }
        }

        private FormulaType GetFormulaType(string type)
        {
            switch (type)
            {
                case "decimal":
                    return FormulaType.Decimal;
                case "number":
                    return FormulaType.Number;
                case "string":
                    return FormulaType.String;
                case "boolean": 
                    return FormulaType.Boolean;
                case "datetime":
                    return FormulaType.DateTime;
                default:
                    return FormulaType.Blank;
            }
        }
    }
}
