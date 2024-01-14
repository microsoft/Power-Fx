// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    internal class ExpressionLocalizationHelper
    {
        internal static string ConvertExpression(string expressionText, RecordType parameters, BindingConfig bindingConfig, INameResolver resolver, IBinderGlue binderGlue, ParserOptions options, Features flags, bool toDisplay)
        {
            var targetLexer = toDisplay ? TexlLexer.GetLocalizedInstance(options?.Culture ?? CultureInfo.InvariantCulture) : TexlLexer.InvariantLexer;
            var sourceLexer = toDisplay ? TexlLexer.InvariantLexer : TexlLexer.GetLocalizedInstance(options?.Culture ?? CultureInfo.InvariantCulture);

            var worklist = GetLocaleSpecificTokenConversions(expressionText, sourceLexer, targetLexer);

            var formula = new Formula(expressionText, toDisplay ? CultureInfo.InvariantCulture : options?.Culture ?? CultureInfo.InvariantCulture);

            formula.EnsureParsed(options.GetParserFlags());

            var binding = TexlBinding.Run(
                binderGlue,
                null,
                new Core.Entities.QueryOptions.DataSourceToQueryOptionsMap(),
                formula.ParseTree,
                resolver,
                bindingConfig,
                ruleScope: parameters?._type,
                updateDisplayNames: toDisplay,
                forceUpdateDisplayNames: toDisplay,
                features: flags);

            foreach (var token in binding.NodesToReplace)
            {
                worklist.Add(token.Key.Span, TexlLexer.EscapeName(token.Value));
            }

            return Span.ReplaceSpans(expressionText, worklist);
        }

        internal static string ConvertExpression(string expressionText, RecordType parameters, BindingConfig bindingConfig, INameResolver resolver, IBinderGlue binderGlue, CultureInfo culture, Features flags, bool toDisplay)
        {
            return ConvertExpression(expressionText, parameters, bindingConfig, resolver, binderGlue, new ParserOptions() { Culture = culture }, flags, toDisplay);
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
                    case TokKind.DecLit:
                        var numLit = token.Span.GetFragment(script);
                        var decimalSeparatorIndex = numLit.IndexOf(sourceDecimalSeparator, StringComparison.Ordinal);
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
