// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
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
                // [START] TEMPORARY CODE TO HELP RESOLVE ISSUE #2325
                //                     
                // System.ArgumentException: An item with the same key has already been added.
                //   at System.ThrowHelper.ThrowArgumentException(ExceptionResource resource)
                //   at System.Collections.Generic.Dictionary`2.Insert(TKey key, TValue value, Boolean add)
                //   at Microsoft.PowerFx.Core.ExpressionLocalizationHelper.ConvertExpression(String expressionText, RecordType parameters, BindingConfig bindingConfig, INameResolver resolver, IBinderGlue binderGlue, ParserOptions options, Features flags, Boolean toDisplay)
                //   at Microsoft.PowerFx.Engine.GetDisplayExpression(String expressionText, ReadOnlySymbolTable symbolTable, CultureInfo culture)
                //   at Microsoft.PowerFx.LanguageServerProtocol.LanguageServer.HandleInitialFixupRequest(String id, String paramsJson)
                //   at Microsoft.PowerFx.LanguageServerProtocol.LanguageServer.OnDataReceived(String jsonRpcPayload)

                if (worklist.ContainsKey(token.Key.Span))
                {
                    throw new InvalidOperationException($"Expression: [{expressionText}], RecordType: {ToStringWithDisplayNames(parameters)}, Resolver:{DumpSymbolsWithDisplayNames(resolver)}, Culture: {GetCultureName(options?.Culture)}");
                }

                // [END] TEMPORARY CODE TO HELP RESOLVE ISSUE #2325

                worklist.Add(token.Key.Span, TexlLexer.EscapeName(token.Value));
            }

            return Span.ReplaceSpans(expressionText, worklist);
        }

        // [START] TEMPORARY CODE TO HELP RESOLVE ISSUE #2325

        private static string GetCultureName(CultureInfo ci)
        {
            if (ci == null)
            {
                return "<null>";
            }

            if (ci == CultureInfo.InvariantCulture)
            {
                return "Invariant";
            }

            return ci.Name;
        }

        private static string DumpSymbolsWithDisplayNames(INameResolver resolver)
        {
            if (resolver == null)
            {
                return "<null>";
            }

            if (resolver is ComposedReadOnlySymbolTable symbols)
            {
                StringBuilder sb = new StringBuilder();
                bool first = true;

                if (!symbols.GlobalSymbols.Any())
                {
                    return "<no symbol>";
                }

                foreach (KeyValuePair<string, NameLookupInfo> symbol in symbols.GlobalSymbols)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }

                    sb.Append($"{symbol.Key}");

                    if (!string.IsNullOrEmpty(symbol.Value.DisplayName))
                    {
                        sb.Append($"`{symbol.Value.DisplayName}");
                    }

                    sb.Append(":");
                    sb.Append(ToStringWithDisplayNames(symbol.Value.Type));

                    first = false;
                }

                return sb.ToString();
            }

            return $"<unknown type: {resolver.GetType().FullName}>";
        }

        private static string ToStringWithDisplayNames(FormulaType ftype)
        {
            if (ftype == null)
            {
                return "<null>";
            }

            return ToStringWithDisplayNames(ftype._type);
        }

        private static string ToStringWithDisplayNames(DType dtype)
        {
            if (dtype == null)
            {
                return "<null>";
            }

            var sb = new StringBuilder();
            AppendToWithDisplayNames(sb, dtype);
            return sb.ToString();
        }

        private static string AppendToWithDisplayNames(StringBuilder sb, DType dtype)
        {
            sb.Append(DType.MapKindToStr(dtype.Kind));

            switch (dtype.Kind)
            {
                case DKind.Record:
                case DKind.Table:
                    AppendAggregateType(sb, dtype.TypeTree, dtype.DisplayNameProvider);
                    break;
                case DKind.OptionSet:
                case DKind.View:
                    AppendOptionSetOrViewType(sb, dtype.TypeTree, dtype.DisplayNameProvider);
                    break;
                case DKind.Enum:
                    AppendEnumType(sb, dtype.ValueTree, dtype.EnumSuperkind, dtype.DisplayNameProvider);
                    break;
                case DKind.OptionSetValue:
                    AppendOptionSetValue(sb, dtype.OptionSetInfo);
                    break;
            }

            return sb.ToString();
        }

        private static void AppendOptionSetValue(StringBuilder sb, IExternalOptionSet optionSet)
        {
            if (optionSet is EnumSymbol es)
            {
                sb.Append('(');
                bool first = true;

                foreach (DName name in es.OptionNames)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;

                    sb.Append(TexlLexer.EscapeName(name.Value));
                    if (es.TryLookupValueByName(name.Value, out object value))
                    {
                        sb.Append('=');
                        sb.Append(value.ToString());
                    }
                }

                sb.Append(')');
            }
        }

        private static void AppendAggregateType(StringBuilder sb, TypeTree tree, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append("[");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value) && TexlLexer.EscapeName(kvp.Key) != TexlLexer.EscapeName(kvp2.Value.Value))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(kvp2.Value.Value));
                }

                sb.Append(":");
                AppendToWithDisplayNames(sb, kvp.Value);
                strPre = ", ";
            }

            sb.Append("]");
        }

        private static void AppendOptionSetOrViewType(StringBuilder sb, TypeTree tree, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append("{");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.Assert(kvp.Value.IsValid);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value) && TexlLexer.EscapeName(kvp.Key) != TexlLexer.EscapeName(kvp2.Value.Value))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(kvp2.Value.Value));
                }

                sb.Append(":");
                AppendToWithDisplayNames(sb, kvp.Value);
                strPre = ", ";
            }

            sb.Append("}");
        }

        private static void AppendEnumType(StringBuilder sb, ValueTree tree, DKind enumSuperkind, DisplayNameProvider nameProvider)
        {
            Contracts.AssertValue(sb);

            sb.Append(DType.MapKindToStr(enumSuperkind));
            sb.Append("[");

            var strPre = string.Empty;
            foreach (var kvp in tree.GetPairs())
            {
                Contracts.AssertNonEmpty(kvp.Key);
                Contracts.AssertValue(kvp.Value.Object);
                sb.Append(strPre);
                sb.Append(TexlLexer.EscapeName(kvp.Key));

                // check if we have a corresponding display name
                var kvp2 = nameProvider?.LogicalToDisplayPairs.FirstOrDefault(kvp2 => kvp2.Key == kvp.Key);
                if (!string.IsNullOrEmpty(kvp2?.Value.Value) && TexlLexer.EscapeName(kvp.Key) != TexlLexer.EscapeName(kvp2.Value.Value))
                {
                    sb.Append("`");
                    sb.Append(TexlLexer.EscapeName(kvp2.Value.Value));
                }

                sb.Append(":");
                kvp.Value.AppendTo(sb);
                strPre = ", ";
            }

            sb.Append("]");
        }

        // [END] TEMPORARY CODE TO HELP RESOLVE ISSUE #2325

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
