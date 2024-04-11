// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// Contains helper methods for managing diagnostics.
    /// </summary>
    internal static class DiagnosticsHelper
    {
        /// <summary>
        /// Writes a diagnostics notification to the builder.
        /// </summary>
        /// <param name="builder">Output Builder.</param>
        /// <param name="uri">Document Uri.</param>
        /// <param name="expression">Expression.</param>
        /// <param name="errors">Errors.</param>
        public static void WriteDiagnosticsNotification(this LanguageServerOutputBuilder builder, string uri, string expression, ExpressionError[] errors)
        {
            builder.AddNotification(TextDocumentNames.PublishDiagnostics, CreateDiagnosticsNotification(uri, expression, errors));
        }

        /// <summary>
        /// Creates a diagnostics notification from the given information.
        /// </summary>
        /// <param name="uri">Document Uri.</param>
        /// <param name="expression">Expression.</param>
        /// <param name="errors">Errors.</param>
        /// <returns>Diagnostics Notification.</returns>
        public static PublishDiagnosticsParams CreateDiagnosticsNotification(string uri, string expression, ExpressionError[] errors)
        {
            Contracts.AssertNonEmpty(uri);
            Contracts.AssertValue(expression);

            var diagnostics = new List<Diagnostic>();
            if (errors != null)
            {
                foreach (var item in errors)
                {
                    var span = item.Span ?? new Span(0, 0);
                    diagnostics.Add(new Diagnostic()
                    {
                        Range = span.ConvertSpanToRange(expression),
                        Message = item.Message,
                        Severity = DocumentSeverityToDiagnosticSeverityMap(item.Severity)
                    });
                }
            }

            return new PublishDiagnosticsParams()
            {
                Uri = uri,
                Diagnostics = diagnostics.ToArray()
            };
        }

        /// <summary>
        /// PowerFx classifies diagnostics by <see cref="DocumentErrorSeverity"/>, LSP classifies them by
        /// <see cref="DiagnosticSeverity"/>. This method maps the former to the latter.
        /// </summary>
        /// <param name="severity">
        /// <see cref="DocumentErrorSeverity"/> which will be mapped to the LSP eequivalent.
        /// </param>
        /// <returns>
        /// <see cref="DiagnosticSeverity"/> equivalent to <see cref="DocumentErrorSeverity"/>.
        /// </returns>
        private static DiagnosticSeverity DocumentSeverityToDiagnosticSeverityMap(ErrorSeverity severity) => severity switch
        {
            ErrorSeverity.Critical => DiagnosticSeverity.Error,
            ErrorSeverity.Severe => DiagnosticSeverity.Error,
            ErrorSeverity.Moderate => DiagnosticSeverity.Error,
            ErrorSeverity.Warning => DiagnosticSeverity.Warning,
            ErrorSeverity.Suggestion => DiagnosticSeverity.Hint,
            ErrorSeverity.Verbose => DiagnosticSeverity.Information,
            _ => DiagnosticSeverity.Information
        };
    }
}
