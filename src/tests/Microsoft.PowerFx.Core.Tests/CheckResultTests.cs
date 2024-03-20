// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

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

        [Theory]

        [InlineData("\"Something\"", "Something", true)]
        [InlineData("1", 1d, true)]
        [InlineData("true", true, true)]
        [InlineData("GUID(\"ac98f780-0df8-427d-8c09-50f09b5f9cf5\")", "ac98f780-0df8-427d-8c09-50f09b5f9cf5", true)]
        [InlineData("GUID()", null, false)]
        [InlineData("Abs(2)", null, false)]
        public void GetParseLiteralsTests(string expression, object expected, bool canGetAsLiteral)
        {
            var check = new CheckResult(new Engine());
            var parseResult = Engine.Parse(expression, options: new PowerFx.ParserOptions() { NumberIsFloat = true });

            check.SetText(parseResult);
            check.ApplyParse();

            var gotLiteral = check.TryGetAsLiteral(out var value);

            Assert.Equal(canGetAsLiteral, gotLiteral);

            if (gotLiteral)
            {
                if (expression.StartsWith("GUID("))
                {
                    expected = Guid.Parse((string)expected);
                }

                Assert.Equal(expected, value);
            }
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

        [Fact]
        public void GetFunctionInfoTest()
        {
            var check = new CheckResult(new Engine())
                .SetText("Power(3,2)")
                .SetBindingInfo();

            var node = (CallNode)check.ApplyParse().Root;

            var info = check.GetFunctionInfo(node);
            
            Assert.Equal("Power", info.Name);
            var sigs = info.Signatures.Single();
            Assert.Equal("Power(base, exponent)", sigs.DebugToString());
        }

        [Fact]
        public void GetFunctionInfoTestNull()
        {
            var check = new CheckResult(new Engine())
                .SetText("Power(3,2)")
                .SetBindingInfo();

            Assert.Throws<ArgumentNullException>(() => check.GetFunctionInfo(null));
        }

        [Fact]
        public void GetFunctionMissingTest()
        {
            var check = new CheckResult(new Engine())
                .SetText("Missing(3,2)")
                .SetBindingInfo();

            var node = (CallNode)check.ApplyParse().Root;

            // No function Info.
            var info = check.GetFunctionInfo(node);
            Assert.Null(info);
        }

        [Theory]
        
        // ****When Coercion is not allowed.****
        [InlineData("\"test string\"", false, true, "s", "")]

        // (even with coercion not allowed, we coerce numerics.)
        [InlineData("Float(12)", false, true, "w", "")]
        [InlineData("Float(12)", false, true, "n", "")]
        [InlineData("Decimal(12)", false, true, "w", "")]
        [InlineData("Decimal(12)", false, true, "n", "")]
        [InlineData("{a:12, b:15}", false, false, "n", "Type mismatch between source and target types. Expected Number; Found Record.")]

        // (even with coercion not allowed, we coerce numerics.)
        [InlineData("{a:Float(12), b:Float(15)}", false, true, "![a:w,b:n]", "")]
        [InlineData("{a:Decimal(12), b:Decimal(15)}", false, true, "![a:w,b:n]", "")]

        // missing field
        [InlineData("{a:Decimal(12), b:Decimal(15)}", false, false, "![a:w,b:n,c:s]", "Type mismatch between source and target record types. Given type has missing fields: c.")]
        
        // extra field
        [InlineData("{a:Decimal(12), b:Decimal(15)}", false, false, "![a:w]", "Type mismatch between source and target record types. Given type has extra fields: b.")]
        [InlineData("{a:12, b:15}", false, false, "*[a:w,b:w]", "Type mismatch between source and target types. Expected Table; Found Record.")]
        [InlineData("{a:12, b:15}", false, false, "![a:w,b:s]", "Type mismatch between source and target record types. Field name: b Expected Text; Found Decimal.")]

        // ****When Coercion is allowed****
        [InlineData("\"test string\"", true, true, "s", "")]
        [InlineData("\"test string\"", true, true, "n", "")]
        [InlineData("12", true, true, "w", "")]
        [InlineData("12", true, true, "n", "")]
        [InlineData("{a:12, b:15}", true, false, "n", "Given Record type cannot be coerced to source type Number.")]
        [InlineData("{a:12, b:15}", true, true, "![a:n,b:n]", "")]
        [InlineData("{a:12, b:15}", true, false, "*[a:w,b:n]", "Type mismatch between source and target types. Expected Table; Found Record.")]
        [InlineData("{a:Float(12), b:Float(15)}", true, true, "![a:w,b:n]", "")]
        [InlineData("{a:Decimal(12), b:Decimal(15)}", true, true, "![a:w,b:n]", "")]

        // (with coercion allowed, aggregate's field also coerces)
        [InlineData("{a:12, b:15}", true, true, "![a:w,b:s]", "")]

        // missing field
        [InlineData("{a:Decimal(12), b:Decimal(15)}", true, false, "![a:w,b:n,c:s]", "Type mismatch between source and target record types. Given type has missing fields: c.")]
        
        // extra field
        [InlineData("{a:Decimal(12), b:Decimal(15)}", true, false, "![a:w]", "Type mismatch between source and target record types. Given type has extra fields: b.")]
        public void CheckResultExpectedReturnValueString(string inputExpr, bool allowCoerceTo, bool isSuccess, string expectedType, string errorMsg)
        {
            var expectedFormulaType = FormulaType.Build(TestUtils.DT(expectedType));
            CheckResultExpectedReturnValue(inputExpr, allowCoerceTo, isSuccess, errorMsg, expectedFormulaType);
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

        // Unkown public function are PII
        [InlineData("MadeUpFunction(1)", "#$function$#(#$decimal$#)", true)]
        [InlineData("Power(MadeUpFunction(1))", "Power(#$function$#(#$decimal$#))", true)]
        [InlineData("Power(Clear(1))", "Power(Clear(#$decimal$#))", true)]
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

        [Fact]
        public void TestSummary()
        {
            var check = new CheckResult(new Engine());

            var r1 = RecordType.Empty()
              .Add(new NamedFormulaType("new_field", FormulaType.Number, "Field"));

            check.SetText("1", new PowerFx.ParserOptions { AllowsSideEffects = true });
            check.SetBindingInfo(r1);

            var summary = check.ApplyGetContextSummary();

            Assert.True(summary.AllowsSideEffects);
            Assert.False(summary.IsPreV1Semantics);
            Assert.Null(summary.ExpectedReturnType);
            Assert.Single(summary.SuggestedSymbols);

            var sym1 = summary.SuggestedSymbols.First();

            Assert.Equal("Field", sym1.DisplayName);
            Assert.Equal("Field", sym1.BestName);
            Assert.Equal("new_field", sym1.Name);
            Assert.Equal(FormulaType.Number, sym1.Type);
            Assert.False(sym1.Properties.CanSet);
            Assert.False(sym1.Properties.CanMutate);

            var type1 = sym1.Slot.Owner.GetTypeFromSlot(sym1.Slot);
            Assert.Equal(FormulaType.Number, type1);
        }

        private void CheckResultExpectedReturnValue(string inputExpr, bool allowCoerceTo, bool isSuccess, string errorMsg, FormulaType expectedReturnType)
        {
            var check = new CheckResult(PrimitiveValueConversionsTests.GetEngineWithFeatureGatedFunctions())
                .SetText(inputExpr)
                .SetBindingInfo()
                .SetExpectedReturnValue(expectedReturnType, allowCoerceTo);

            check.ApplyBinding();

            CheckExpectedReturn(check, isSuccess, errorMsg, expectedReturnType);
        }

        private void CheckResultExpectedReturnTypes(string inputExpr, bool isSuccess, string errorMsg, FormulaType[] returnTypes, FormulaType expectedType)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var check = new CheckResult(new Engine())
                .SetText(inputExpr)
                .SetBindingInfo()
                .SetExpectedReturnValue(returnTypes);
#pragma warning restore CS0618 // Type or member is obsolete

            check.ApplyBinding();

            CheckExpectedReturn(check, isSuccess, errorMsg, expectedType);
        }

        private void CheckExpectedReturn(CheckResult check, bool isSuccess, string errorMsg, FormulaType expectedType)
        {
            if (isSuccess)
            {
                Assert.True(check.IsSuccess);
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

                Assert.True(errorMsg.Contains(exMsg), exMsg);
            }
        }
    }
}
