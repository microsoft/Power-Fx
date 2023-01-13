// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class CheckResultTests
    {
        [Fact]
        public void Ctors()
        {
            // CheckResult must have an engine so that it can invoke operations.
            Assert.Throws<ArgumentNullException>(() => new CheckResult((Engine)null));
        }

        [Fact]
        public void Errors()
        {
            Assert.Throws<ArgumentNullException>(() => new CheckResult((IEnumerable<ExpressionError>)null));

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
        public void ParseResult()
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

            // Can still try to bind even with parse errors. 
            // But some information like Returntype isn't computed.
            check.SetBindingInfo();
            check.ApplyBinding();
            Assert.NotNull(check.Binding);
            Assert.Null(check.ReturnType);

            // Still assign some types
            var node = ((BinaryOpNode)parse.Root).Left;
            var type = check.GetNodeType(node);
            Assert.Equal(FormulaType.Number, type);
        }

        // Ensure we can pass in ParserOptions. 
        [Fact]
        public void ParserOptions()
        {
            var check = new CheckResult(new Engine());
            var opts = new ParserOptions
            {
                Culture = new System.Globalization.CultureInfo("fr-FR")
            };
            check.SetText("1,234", opts); // , is decimal separate for fr-FR.
            
            var parse = check.ApplyParse();
            Assert.Same(opts, parse.Options);

            Assert.True(check.IsSuccess);
            var value = ((NumLitNode)parse.Root).ActualNumValue;
            Assert.Equal(1.234, value);
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
        public void TestIR()
        {
            var check = new CheckResult(new Engine());

            Assert.Throws<InvalidOperationException>(() => check.ApplyIR());
            check.SetText("1+2");
            check.SetBindingInfo();

            var ir = check.ApplyIR();
            Assert.NotNull(ir);
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
    }
}
