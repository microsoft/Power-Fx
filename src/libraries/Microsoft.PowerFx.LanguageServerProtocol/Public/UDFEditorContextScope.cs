// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Intellisense.IntellisenseData;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    internal class UDFEditorContextScope : EditorContextScope
    {
        private readonly Func<string, DefinitionsCheckResult> _getUDFResult;

        internal UDFEditorContextScope(
            Engine engine,
            CultureInfo cultureInfo,
            ReadOnlySymbolTable symbols = null,
            bool allowSideEffects = false)
            : this(
                  engine,
                  SymbolTable.GetUDFParserOptions(cultureInfo, allowSideEffects),
                  ReadOnlySymbolTable.Compose(engine.UDFDefaultBindingSymbols, symbols))
        {
        }

        private UDFEditorContextScope(
            Engine engine,
            ParserOptions parserOptions,
            ReadOnlySymbolTable bindingSymbols)
            : this(
                  (expr) => new CheckResult(engine)
                      .SetText(expr, parserOptions)
                      .SetBindingInfo(bindingSymbols),
                  (expr) => new DefinitionsCheckResult()
                    .SetText(expr, parserOptions)
                    .SetBindingInfo(bindingSymbols))
        {
        }

        internal UDFEditorContextScope(Func<string, CheckResult> getCheckResult, Func<string, DefinitionsCheckResult> getUDFResult)
            : base(getCheckResult)
        {
            _getUDFResult = getUDFResult;
        }

        public new IEnumerable<ExpressionError> GetErrors(string expression)
        {
            var checkUdf = _getUDFResult(expression);
            checkUdf.ApplyErrors();
            return checkUdf.Errors;
        }

        public override IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            var checkUdf = _getUDFResult(expression);
            var parsedUdfs = checkUdf.ApplyParse();

            foreach (var parsedUdf in parsedUdfs.UDFs)
            {
                // Maybe cache per-UDF if this is on a hot path.
                var udf = UserDefinedFunction.CreatePartialFunction(parsedUdf, checkUdf.UDFBindingSymbols, out _);
                var nameResolver = udf.GetUDFNameResolver(checkUdf.UDFBindingSymbols);

                // No body: only type suggestions (arg types OR return type)
                if (parsedUdf.Body == null)
                {
                    // Parameter type position: foo(x: |
                    foreach (var arg in parsedUdf.Args)
                    {
                        if (!IsUDFArgTypePosition(cursorPosition, arg))
                        {
                            continue;
                        }

                        var suggestions = BuildTypeSuggestionList(nameResolver.NamedTypes, includeVoid: false);

                        // start right at the type ident if present, otherwise just after the colon
                        var startIndex = arg.TypeIdent?.Span.Min ?? (arg.ColonToken.Span.Lim + 1);
                        var length = SafeLengthFrom(startIndex, cursorPosition);

                        return MakeTypeSuggestionResult(
                            expression,
                            checkUdf.UDFParserOptions.Culture,
                            suggestions,
                            startIndex,
                            length);
                    }

                    // Return type position: Foo(...): |
                    if (IsUDFReturnTypePosition(cursorPosition, parsedUdf))
                    {
                        var suggestions = BuildTypeSuggestionList(nameResolver.NamedTypes, includeVoid: true);

                        // If a return type ident already exists, replace from its start; else from after the ':' of return type.
                        var rtStart = parsedUdf.ReturnType?.Span.Min
                                      ?? (parsedUdf.ReturnTypeColonToken?.Span.Lim + 1)
                                      ?? cursorPosition; // fallback safety

                        var length = SafeLengthFrom(rtStart, cursorPosition);

                        return MakeTypeSuggestionResult(
                            expression,
                            checkUdf.UDFParserOptions.Culture,
                            suggestions,
                            rtStart,
                            length);
                    }

                    // Not in a typeable spot within a body-less UDF; check the next UDF.
                    continue;
                }

                // Has body: delegate to engine if cursor is inside body span
                var bodySpan = parsedUdf.Body.GetCompleteSpan();
                if (cursorPosition > bodySpan.Min && cursorPosition <= bodySpan.Lim)
                {
                    var cr = base.Check(string.Empty);
                    var engine = cr.Engine;
                    var formula = new Formula(expression, parsedUdf.Body, checkUdf.UDFParserOptions.Culture);

                    // ApplyCreateUserDefinedFunctions must be called to populate the engine with the all UDF before calling Suggest, so another UDF can be suggested from inside this UDF.
                    checkUdf.ApplyCreateUserDefinedFunctions(); 
                    var binding = udf.BindBody(checkUdf.UDFBindingSymbols, new Glue2DocumentBinderGlue(), checkUdf.UDFBindingConfig);
                    return engine.Suggest(formula, binding, cursorPosition, base.Services);
                }
            }

            return MakeTypeSuggestionResult(
                script: expression,
                culture: checkUdf.UDFParserOptions.Culture, // or use default parser culture
                suggestions: new IntellisenseSuggestionList(),
                replacementStartIndex: cursorPosition,
                replacementLength: 0);
        }

        private static int SafeLengthFrom(int startIndex, int cursorPosition)
        {
            var len = cursorPosition - startIndex;
            return len < 0 ? 0 : len;
        }

        private static IntellisenseSuggestionList BuildTypeSuggestionList(
            IEnumerable<KeyValuePair<DName, FormulaType>> namedTypes, bool includeVoid)
        {
            var list = new IntellisenseSuggestionList();

            foreach (var kvp in namedTypes)
            {
                var typeName = kvp.Key.Value;
                var val = kvp.Value;

                list.Add(new IntellisenseSuggestion(
                    new UIString(typeName),         // UI text
                    SuggestionKind.Type,             // Kind
                    SuggestionIconKind.Other,        // Icon
                    val._type,                       // DType
                    typeName,                       // exact match
                    -1,                              // argCount (N/A for types)
                    string.Empty,                    // description
                    string.Empty));                  // help
            }

            if (includeVoid)
            {
                list.Add(new IntellisenseSuggestion(
                    new UIString(BuiltInTypeNames.Void.Value),         // UI text
                    SuggestionKind.Type,                               // Kind
                    SuggestionIconKind.Other,                          // Icon
                    FormulaType.Void._type,                            // DType
                    BuiltInTypeNames.Void,                             // exact match
                    -1,                                                // argCount (N/A for types)
                    string.Empty,                                      // description
                    string.Empty));                                    // help
            }

            return list;
        }

        private static IIntellisenseResult MakeTypeSuggestionResult(
            string script,
            CultureInfo culture,
            IntellisenseSuggestionList suggestions,
            int replacementStartIndex,
            int replacementLength)
        {
            var data = new UDFIntellisenseData(
                replacementStartIndex: replacementStartIndex,
                replacementLength: replacementLength,
                0,
                0,
                script: script,
                locale: culture);

            suggestions.Sort(culture);
            IList<IntellisenseSuggestion> list = suggestions;

            return new IntellisenseResult(
                data,
                (IReadOnlyList<IntellisenseSuggestion>)list,
                Enumerable.Empty<TexlFunction>());
        }

        internal static bool IsUDFArgTypePosition(int cursorPos, UDFArg arg)
        {
            Contracts.AssertValue(arg);
            return (arg.TypeIdent != null && arg.TypeIdent.Span.IsInRange(cursorPos)) ||
                   (arg.ColonToken != null && arg.ColonToken.Span.IsInRange(cursorPos)) ||
                   (arg.TypeIdent == null && arg.ColonToken != null && cursorPos > arg.ColonToken.Span.Lim);
        }

        internal static bool IsUDFReturnTypePosition(int cursorPos, UDF parsedUDF)
        {
            Contracts.AssertValue(parsedUDF);

            return (parsedUDF.ReturnType != null && parsedUDF.ReturnType.Span.IsInRange(cursorPos)) ||
                   (parsedUDF.ReturnTypeColonToken != null && parsedUDF.ReturnTypeColonToken.Span.IsInRange(cursorPos)) ||
                   (parsedUDF.ReturnType == null && parsedUDF.ReturnTypeColonToken != null && cursorPos > parsedUDF.ReturnTypeColonToken.Span.Lim);
        }
    }
}
