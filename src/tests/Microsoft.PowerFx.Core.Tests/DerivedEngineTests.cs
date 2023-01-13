// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Tests
{
    // Test various hooks deriving from Engine
    public class DerivedEngineTests
    {
        [Fact]
        public void Test()
        {
            Engine engineNormal = new Engine(new PowerFxConfig());
            Engine engine = new MyEngine();

            var result = engine.Check("1+2");
            Assert.True(result.IsSuccess);

            // PostCheck hook 
            var expr = "1+\"abc\"";
            result = engineNormal.Check(expr);
            Assert.True(result.IsSuccess);

            result = engine.Check(expr);
            Assert.False(result.IsSuccess);

            var errors = result.Errors.ToArray();
            Assert.Single(errors);
            Assert.Contains("Test: can't have string litera", errors[0].Message);
        }

        [Theory]
        [InlineData("1234+6789+1234")] // Valid parse
        [InlineData("1234+6789+++++")] // Invalid parse
        [InlineData("1234+6789+xxxx")] // valid parse, Invalid bind
        public void EngineMaxExpressionLength(string expr)
        {
            Engine engine = new MyEngine();
            
            // Since options are mutable, each call gets a new copy 
            var opt1 = engine.GetDefaultParserOptionsCopy();
            var opt2 = engine.GetDefaultParserOptionsCopy();
            Assert.NotSame(opt1, opt2);

            Assert.Equal(10, opt1.MaxExpressionLength);

            var parseResult = engine.Parse(expr);
            Assert.False(parseResult.IsSuccess);
            Assert.True(parseResult.HasError);

            var check = engine.Check(expr);
            Assert.False(check.IsSuccess);

            // Only 1 error for being too long.
            // Any other errors indicate additional work that we shouldn't have done. 
            var errors = check.Errors;
            Assert.Single(errors);
            Assert.Equal("Error 0-14: Expression can't be more than 10 characters. The expression is 14 characters.", errors.First().ToString());
        }

        private class MyEngine : Engine
        {
            public MyEngine()
                : base(new PowerFxConfig())
            {
            }

            public override ParserOptions GetDefaultParserOptionsCopy()
            {
                return new ParserOptions
                {
                     MaxExpressionLength = 10
                };
            }

            public int PostCheckCounter = 0;

            // Have a constrain - any string literal is an error. 
            private class MyVisitor : IdentityTexlVisitor
            {
                public List<ExpressionError> _errors = new List<ExpressionError>();

                public override void Visit(StrLitNode node)
                {
                    _errors.Add(new ExpressionError
                    {
                        Message = $"Test: can't have string literal: {node.Value}",
                        Span = node.GetCompleteSpan(),
                        Kind = ErrorKind.Unknown
                    });
                }
            }

            protected override IEnumerable<ExpressionError> PostCheck(CheckResult check)
            {
                PostCheckCounter++;

                var v = new MyVisitor();
                check.Parse.Root.Accept(v);

                return v._errors;
            }
        }

        [Fact]
        public void RuleScopeTest()
        {
            // Derived engine is providing custom rule scope and resolvers. 
            Engine engine = new RuleScopeEngine();
            
            var result = engine.Check("new_field * 2");

            Assert.True(result.IsSuccess);

            // RuleScope hook will enable ThisRecord binding config. 
            result = engine.Check("ThisRecord.Field + 5");
            Assert.True(result.IsSuccess);

            // Custom Lookup hook 
            result = engine.Check("extra = 5");
            Assert.True(result.IsSuccess);

            // Conversions 
            var expr = "ThisRecord.Field";
            var exprInvariant = engine.GetInvariantExpression(expr, null);
            Assert.Equal("ThisRecord.new_field", exprInvariant);

            var exprDisplay = engine.GetDisplayExpression(exprInvariant, (RecordType)null);
            Assert.Equal(expr, exprDisplay);
        }

        // Engine similar to what SQL engine does
        public class RuleScopeEngine : Engine
        {
            public RuleScopeEngine()
                : base(new PowerFxConfig())
            {
            }

            internal static readonly RecordType _ruleScope = RecordType.Empty()
                .Add("new_field", FormulaType.Number, "Field");

            private protected override RecordType GetRuleScope()
            {
                return _ruleScope;
            }

            [Obsolete]
            private protected override INameResolver CreateResolver()
            {
                var functionList = ReadOnlySymbolTable.Compose(Config.SymbolTable, this.SupportedFunctions);
                var resolver = new CustomResolver(functionList);
                return resolver;
            }

            private class CustomResolver : ComposedReadOnlySymbolTable
            {
                public CustomResolver(ReadOnlySymbolTable functions)
                    : base(functions)
                {
                }

                public override bool Lookup(DName name, out NameLookupInfo nameInfo, NameLookupPreferences preferences = NameLookupPreferences.None)
                {
                    if (name.Value == "extra")
                    {
                        nameInfo = new NameLookupInfo(BindKind.PowerFxResolvedObject,   DType.Number, DPath.Root, 0);
                        return true;                        
                    }

                    return base.Lookup(name, out nameInfo, preferences);
                }
            }
        }
    }
}
