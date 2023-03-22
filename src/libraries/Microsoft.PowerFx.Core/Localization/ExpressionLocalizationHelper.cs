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
        [Obsolete("Use ConvertExpression with PowerFxConfig parameter instead of CultureInfo", false)]
        internal static string ConvertExpression(string expressionText, RecordType parameters, BindingConfig bindingConfig, INameResolver resolver, IBinderGlue binderGlue, CultureInfo culture, bool toDisplay)
        {
            return ConvertExpression(expressionText, parameters, bindingConfig, resolver, binderGlue, new PowerFxConfig(culture), toDisplay);
        }

        internal static string ConvertExpression(string expressionText, RecordType parameters, BindingConfig bindingConfig, INameResolver resolver, IBinderGlue binderGlue, PowerFxConfig fxConfig, bool toDisplay)
        {
            return ConvertExpression(expressionText, parameters, bindingConfig, resolver, binderGlue, fxConfig.CultureInfo, fxConfig.Features, toDisplay);
        }

        internal static string ConvertExpression(string expressionText, RecordType parameters, BindingConfig bindingConfig, INameResolver resolver, IBinderGlue binderGlue, CultureInfo culture, Features flags, bool toDisplay)
        {
            var targetLexer = toDisplay ? TexlLexer.GetLocalizedInstance(culture) : TexlLexer.InvariantLexer;
            var sourceLexer = toDisplay ? TexlLexer.InvariantLexer : TexlLexer.GetLocalizedInstance(culture);

            var worklist = GetLocaleSpecificTokenConversions(expressionText, sourceLexer, targetLexer);

            var formula = new Formula(expressionText, toDisplay ? CultureInfo.InvariantCulture : culture);
            formula.EnsureParsed(TexlParser.Flags.None);

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
