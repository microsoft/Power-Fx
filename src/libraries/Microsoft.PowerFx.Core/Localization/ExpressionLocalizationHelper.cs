// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal class ExpressionLocalizationHelper
    {
        internal static string ConvertExpression(string expressionText, BaseRecordType parameters, BindingConfig config, INameResolver resolver, IBinderGlue binderGlue, CultureInfo userCulture, bool toDisplay)
        {
            var targetLexer = toDisplay ? TexlLexer.GetLocalizedInstance(userCulture) : TexlLexer.InvariantLexer;
            var sourceLexer = toDisplay ? TexlLexer.InvariantLexer : TexlLexer.GetLocalizedInstance(userCulture);

            var worklist = GetLocaleSpecificTokenConversions(expressionText, sourceLexer, targetLexer);

            var formula = new Formula(expressionText, toDisplay ? CultureInfo.InvariantCulture : userCulture);
            formula.EnsureParsed(TexlParser.Flags.None);

            var binding = TexlBinding.Run(
                binderGlue,
                null,
                new Core.Entities.QueryOptions.DataSourceToQueryOptionsMap(),
                formula.ParseTree,
                resolver,
                config,
                ruleScope: parameters.DType,
                updateDisplayNames: toDisplay,
                forceUpdateDisplayNames: toDisplay);

            foreach (var token in binding.NodesToReplace)
            {
                worklist.Add(token.Key.Span, TexlLexer.EscapeName(token.Value));
            }

            return Span.ReplaceSpans(expressionText, worklist);
        }

        private static IDictionary<Span, string> GetLocaleSpecificTokenConversions(string script, TexlLexer sourceLexer, TexlLexer targetLexer)
        {
            var worklist = new Dictionary<Span, string>();

            if (sourceLexer.LocalizedPunctuatorDecimalSeparator == targetLexer.LocalizedPunctuatorDecimalSeparator)
            {
                // No token conversion required, locales use the same set of punctuators
                return worklist;
            }

            var sourceDecimalSeparator = sourceLexer.LocalizedPunctuatorDecimalSeparator;
            var tokens = sourceLexer.LexSource(script);

            foreach (var token in tokens)
            {
                var span = token.Span;
                string replacement;
                switch (token.Kind)
                {
                    case TokKind.Comma:
                        replacement = targetLexer.LocalizedPunctuatorListSeparator;
                        break;
                    case TokKind.Semicolon:
                        replacement = targetLexer.LocalizedPunctuatorChainingSeparator;
                        break;
                    case TokKind.NumLit:
                        var numLit = token.Span.GetFragment(script);
                        var decimalSeparatorIndex = numLit.IndexOf(sourceDecimalSeparator);
                        if (decimalSeparatorIndex >= 0)
                        {
                            var newMin = span.Min + decimalSeparatorIndex;
                            span = new Span(newMin, newMin + sourceDecimalSeparator.Length);
                            replacement = targetLexer.LocalizedPunctuatorDecimalSeparator;
                        }
                        else
                        {
                            replacement = null;
                        }

                        break;
                    default:
                        replacement = null;
                        break;
                }

                if (!string.IsNullOrEmpty(replacement))
                {
                    worklist.Add(span, replacement);
                }
            }

            return worklist;
        }
    }
}
