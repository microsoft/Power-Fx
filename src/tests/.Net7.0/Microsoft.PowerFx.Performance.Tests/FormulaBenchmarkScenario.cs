// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Performance.Tests
{
    public enum FormulaBenchmarkExpectedOutcome
    {
        Success,
        ParseError,
        BindError
    }

    public sealed class FormulaBenchmarkScenario
    {
        public FormulaBenchmarkScenario(
            string name,
            string category,
            string source,
            string expression,
            ParserOptions parserOptions,
            ReadOnlySymbolTable symbols,
            FormulaBenchmarkExpectedOutcome expectedOutcome)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            ParserOptions = parserOptions ?? throw new ArgumentNullException(nameof(parserOptions));
            Symbols = symbols;
            ExpectedOutcome = expectedOutcome;
        }

        public string Name { get; }

        public string Category { get; }

        public string Source { get; }

        public string Expression { get; }

        public ParserOptions ParserOptions { get; }

        public ReadOnlySymbolTable Symbols { get; }

        public FormulaBenchmarkExpectedOutcome ExpectedOutcome { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class FormulaBenchmarkScenarios
    {
        private const int WideSwitchArmCount = 64;
        private const int DeepCallCount = 32;
        private const int DeepScopeCount = 12;
        private const int GlobalSymbolCount = 1000;
        private const int LongCommentCharCount = 100_000;

        public static IReadOnlyList<FormulaBenchmarkScenario> Create()
        {
            var parserOptions = new ParserOptions(new CultureInfo("en-US"));
            var typedTableSymbols = CreateTypedTableSymbols();
            var largeGlobalSymbols = CreateLargeGlobalSymbols();

            return new[]
            {
                new FormulaBenchmarkScenario(
                    "ArithmeticSmall",
                    "Baseline",
                    "ExpressionTestCases\\literals.txt; ExpressionTestCases\\arithmetic.txt",
                    "1 + 2 * 3 - 4 / 5",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "NestedInterpolation",
                    "Syntax",
                    "ExpressionTestCases\\StringInterpolate.txt",
                    "$\"Summary: {With({a:4,b:6},a*b)} / {$\"{true}\"}\"",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "DeepScopes",
                    "Scope",
                    "ExpressionTestCases\\With.txt",
                    CreateDeepScopesExpression(),
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "TablePipeline",
                    "Tables",
                    "ExpressionTestCases\\FilterFunctions.txt; ExpressionTestCases\\AddColumns_SupportColumnNamesAsIdentifiers.txt",
                    "Sum(AddColumns(Filter([1,2,3,4,5], Value > 2), Squared, Value * Value), Squared)",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "WideSwitch",
                    "Scale",
                    "ExpressionTestCases\\switch.txt",
                    CreateWideSwitchExpression(),
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "DeepCalls",
                    "Scale",
                    "Synthetic nested-call scaling case",
                    CreateDeepCallsExpression(),
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "LongComment",
                    "Scale",
                    "Synthetic 100k-character block comment case",
                    CreateLongCommentExpression(),
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "TypedTable",
                    "Symbols",
                    "ExpressionTestCases\\FilterFunctions.txt; ExpressionTestCases\\Sum.txt",
                    "Sum(Filter(Orders, Status = \"Open\"), Amount)",
                    parserOptions,
                    typedTableSymbols,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "LargeGlobals",
                    "Symbols",
                    "Synthetic 1,000-global host-scope case",
                    CreateLargeGlobalsExpression(),
                    parserOptions,
                    largeGlobalSymbols,
                    FormulaBenchmarkExpectedOutcome.Success),
                new FormulaBenchmarkScenario(
                    "InvalidIncomplete",
                    "Invalid",
                    "Synthetic editor-time incomplete formula",
                    "With({x: 1}, If(x > 0,",
                    parserOptions,
                    null,
                    FormulaBenchmarkExpectedOutcome.ParseError),
                new FormulaBenchmarkScenario(
                    "InvalidUnknownName",
                    "Invalid",
                    "ExpressionTestCases binding-error patterns",
                    "Sum(Filter(Orders, MissingAmount > 0), Amount)",
                    parserOptions,
                    typedTableSymbols,
                    FormulaBenchmarkExpectedOutcome.BindError)
            };
        }

        private static ReadOnlySymbolTable CreateTypedTableSymbols()
        {
            var ordersType = TableType.Empty()
                .Add("Amount", FormulaType.Decimal)
                .Add("Status", FormulaType.String)
                .Add("Category", FormulaType.String);

            var symbols = new SymbolTable();
            symbols.AddVariable("Orders", ordersType);
            return symbols;
        }

        private static ReadOnlySymbolTable CreateLargeGlobalSymbols()
        {
            var symbols = new SymbolTable();

            for (var i = 0; i < GlobalSymbolCount; i++)
            {
                var name = "Global" + i.ToString("0000", CultureInfo.InvariantCulture);
                symbols.AddVariable(name, FormulaType.Decimal);
            }

            return symbols;
        }

        private static string CreateLargeGlobalsExpression()
        {
            var builder = new StringBuilder();

            for (var i = GlobalSymbolCount - 1; i >= 0; i--)
            {
                if (builder.Length > 0)
                {
                    builder.Append(" + ");
                }

                builder.Append("Global");
                builder.Append(i.ToString("0000", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static string CreateWideSwitchExpression()
        {
            var builder = new StringBuilder("Switch(");
            builder.Append((WideSwitchArmCount - 1).ToString(CultureInfo.InvariantCulture));

            for (var i = 0; i < WideSwitchArmCount; i++)
            {
                builder.Append(", ");
                builder.Append(i.ToString(CultureInfo.InvariantCulture));
                builder.Append(", ");
                builder.Append((i * i).ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(", -1)");
            return builder.ToString();
        }

        private static string CreateDeepCallsExpression()
        {
            var builder = new StringBuilder();

            for (var i = 0; i < DeepCallCount; i++)
            {
                builder.Append("Abs(");
            }

            builder.Append("-1");
            builder.Append(')', DeepCallCount);
            return builder.ToString();
        }

        private static string CreateDeepScopesExpression()
        {
            var expression = "1";

            for (var i = 0; i < DeepScopeCount; i++)
            {
                var value = (i + 1).ToString(CultureInfo.InvariantCulture);
                expression = $"With({{x: {value}}}, x + ({expression}))";
            }

            return expression;
        }

        private static string CreateLongCommentExpression()
        {
            // A single block comment whose body is 100k+ characters, exercising the lexer's
            // comment scanning throughput. 'a' is used to avoid an embedded "*/" terminator.
            var builder = new StringBuilder(LongCommentCharCount + 16);
            builder.Append("/* ");
            builder.Append('a', LongCommentCharCount);
            builder.Append(" */ 1 + 2");
            return builder.ToString();
        }
    }
}
