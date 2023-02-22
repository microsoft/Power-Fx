// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.PowerFx.PFxCodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PowerFxCodeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PowerFxCodeAnalyzer";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Security";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(CheckCancellationTokenInForLoop, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(CheckCancellationTokenInForEachLoop, SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(CheckCancellationTokenInWhileLoop, SyntaxKind.WhileStatement);
        }

        private void CheckCancellationTokenInForLoop(SyntaxNodeAnalysisContext context)
        {
            var forStatement = (ForStatementSyntax)context.Node;
            var statement = forStatement.Statement.ToString();
            int lineNumber = forStatement.SyntaxTree.GetLineSpan(forStatement.Span).StartLinePosition.Line;

            if (!statement.Contains("ThrowIfCancellationRequested") && !statement.Contains("CheckCancel()"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), forStatement.GetLocation().ToString() + " - line " + lineNumber));
            }
        }

        private void CheckCancellationTokenInForEachLoop(SyntaxNodeAnalysisContext context)
        {
            var foreachStatement = (ForEachStatementSyntax)context.Node;
            var statement = foreachStatement.Statement.ToString();
            int lineNumber = foreachStatement.SyntaxTree.GetLineSpan(foreachStatement.Span).StartLinePosition.Line;

            if (!statement.Contains("ThrowIfCancellationRequested") && !statement.Contains("CheckCancel()"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), foreachStatement.GetLocation().ToString() + " - line " + lineNumber));
            }            
        }

        private void CheckCancellationTokenInWhileLoop(SyntaxNodeAnalysisContext context)
        {
            var whileStatement = (WhileStatementSyntax)context.Node;
            var statement = whileStatement.Statement.ToString();
            int lineNumber = whileStatement.SyntaxTree.GetLineSpan(whileStatement.Span).StartLinePosition.Line;

            if (!statement.Contains("ThrowIfCancellationRequested") && !statement.Contains("CheckCancel()"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), whileStatement.GetLocation().ToString() + " - line " + lineNumber));
            }            
        }
    }
}
