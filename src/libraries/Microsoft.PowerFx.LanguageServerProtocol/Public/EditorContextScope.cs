// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.Annotations;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Intellisense.IntellisenseData;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.LanguageServerProtocol.LanguageServer;

namespace Microsoft.PowerFx
{
    public static class EditorEngineExtensions
    {
        /// <summary>
        /// A scope is the context for a specific formula bar. This will accept the text from the formula bar (editor), use the additional parameters here, and then call Engine.Check(). 
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="parserOptions">Options to pass to Engine.Check.</param>
        /// <param name="symbols">Additonal "local" symbols to pass to Engine.Check.</param>
        /// <returns></returns>
        public static EditorContextScope CreateEditorScope(
            this Engine engine,
            ParserOptions parserOptions = null,
            ReadOnlySymbolTable symbols = null)
        {
            return new EditorContextScope(engine, parserOptions, symbols);
        }                  
    }

    /// <summary>
    /// Implement a<see cref="IPowerFxScope"/> for intellisense on top of an<see cref="Engine"/> instance.
    ///  A scope is the context for a specific formula bar. 
    ///  This includes helpers to aide in customizing the editing experience. 
    /// </summary>
    public sealed class EditorContextScope : IPowerFxScope, IPowerFxScopeFx2NL
    {
        private readonly GuardSingleThreaded _guard = new GuardSingleThreaded();

        private readonly IList<IExpressionRewriter> _initialFixupLSPExpressionRewriter = new List<IExpressionRewriter>();

        public void AddPostCheckExpressionRewriter(IExpressionRewriter expressionRewriter)
        {
            using var guard = _guard.Enter();
            _initialFixupLSPExpressionRewriter.Add(expressionRewriter);
        }

        // List of handlers to get code-fix suggestions. 
        // Key is CodeFixHandler.HandlerName
        private readonly Dictionary<string, CodeFixHandler> _handlers = new Dictionary<string, CodeFixHandler>();

        // Map string (from formula bar) to a CheckResult.
        // The checkResult has all other context (parser options, symbols, engine, etc)
        // This captures the critical invariant: EditorContextScope corresponds to a formula bar where the user just types text, all other context is provided; 
        private readonly Func<string, CheckResult> _getCheckResult;

        // Optional callback to check user defined functions.
        private readonly Func<string, DefinitionsCheckResult> _checkUserDefinedFunctions;

        // Host can set optional hints about where this expression is used. 
        // This can feed into Fx2NL and other help services. 
        public UsageHints UsageHints { get; init; }

        internal EditorContextScope(
            Engine engine,
            ParserOptions parserOptions,
            ReadOnlySymbolTable symbols)
            : this(
                  (string expr) => new CheckResult(engine)
                    .SetText(expr, parserOptions)
                    .SetBindingInfo(symbols), 
                  (string expr) => new DefinitionsCheckResult().SetText(expr, parserOptions).SetBindingInfo(ReadOnlySymbolTable.Compose(engine.UDFDefaultBindingSymbols, symbols)))
        {
        }

        public EditorContextScope(Func<string, CheckResult> getCheckResult)
        {
            _getCheckResult = getCheckResult ?? throw new ArgumentNullException(nameof(getCheckResult));
        }

        public EditorContextScope(Func<string, CheckResult> getCheckResult, Func<string, DefinitionsCheckResult> checkUserDefinedFunctions)
            : this(getCheckResult)
        {
            _checkUserDefinedFunctions = checkUserDefinedFunctions ?? throw new ArgumentNullException(nameof(checkUserDefinedFunctions));
        }

        #region IPowerFxScope

        // The editor scope has all other context needed to produce a CheckResult  from just the 
        // text in the editor. 
        public CheckResult Check(string expression)
        {
            var check = _getCheckResult(expression);

            // By default ...
            check.ApplyBindingInternal();
            check.ApplyErrors();
            check.ApplyDependencyAnalysis();

            return check;
        }

        string IPowerFxScope.ConvertToDisplay(string expression)
        {
            var check = _getCheckResult(expression);
            var symbols = check._symbols;
            var engine = check.Engine;
            foreach (var expressionConverter in _initialFixupLSPExpressionRewriter)
            {
                expression = expressionConverter.Process(check);
                check = _getCheckResult(expression);
            }

            return engine.GetDisplayExpression(expression, symbols, check.ParserCultureInfo);
        }

        IIntellisenseResult IPowerFxScope.Suggest(string expression, int cursorPosition)
        {
            // Suggestions just need the binding, not other things like Dependency Info or errors. 
            var check = _getCheckResult(expression);

            return check.Engine.Suggest(check, cursorPosition, this.Services);
        }

        #endregion

        /// <summary>
        /// Services that can be used within intellisense, like http factory for Dynamic intellisense.
        /// Optional, can be null.
        /// </summary>
        public IServiceProvider Services { get; set; }
         
        public void AddQuickFixHandlers(params CodeFixHandler[] codeFixHandlers)
        {
            this.AddQuickFixHandlers((IEnumerable<CodeFixHandler>)codeFixHandlers);
        }

        public void AddQuickFixHandlers(IEnumerable<CodeFixHandler> codeFixHandlers)
        {
            var list = codeFixHandlers ?? throw new ArgumentNullException(nameof(codeFixHandlers));
            foreach (var handler in list)
            {
                this.AddQuickFixHandler(handler);
            }
        }

        public void AddQuickFixHandler(CodeFixHandler codeFixHandler)
        {
            if (codeFixHandler == null)
            {
                throw new ArgumentNullException(nameof(codeFixHandler));
            }

            var handlerName = codeFixHandler.HandlerName;
            if (string.IsNullOrWhiteSpace(handlerName))
            {
                throw new ArgumentException($"Bad handler name for {codeFixHandler.GetType().FullName}");
            }

            if (_handlers.ContainsKey(handlerName))
            {
                throw new InvalidOperationException($"Handler '{handlerName}' already exists");
            }

            _handlers.Add(handlerName, codeFixHandler);
        }

        // Invoked by LSP to get suggestions. 
        internal CodeActionResult[] SuggestFixes(string expression, OnLogUnhandledExceptionHandler logUnhandledExceptionHandler)
        {
            var check = Check(expression);
            var engine = check.Engine;

            var list = new List<CodeActionResult>();

            // Show fixes for both warnings and errors. 
            foreach (var handler in _handlers)
            {
                try
                {
                    var task = handler.Value.SuggestFixesAsync(engine, check, CancellationToken.None);
                    var fixes = task.Result;

                    if (fixes != null)
                    {
                        var handlerName = handler.Key;
                        foreach (var fix in fixes)
                        {
                            list.Add(fix.New(handler.Key));
                        }
                    }
                }
                catch (Exception e)
                {
                    var e2 = new Exception($"Handler {handler.Key} threw {e.GetDetailedExceptionMessage()}", e);
                    
                        // Dont' let exceptions from a handler block other handlers. 
                    logUnhandledExceptionHandler?.Invoke(e2);
                    continue;
                }
            }

            return list.ToArray();
        }

        // Invoked by LSP when command is executed. 
        internal void OnCommandExecuted(CodeAction codeAction)
        {
            if (codeAction?.ActionResultContext == null)
            {
                return;
            }

            var handlerName = codeAction.ActionResultContext.HandlerName;
            if (string.IsNullOrWhiteSpace(handlerName))
            {
                return;
            }

            if (_handlers.TryGetValue(handlerName, out CodeFixHandler handler))
            {
                // Since we populated the _handlers dictionary, this check should never fail.
                if (handler.HandlerName != handlerName)
                {
                    throw new InvalidOperationException($"Handler name mismatch: {handlerName} vs. {handler.HandlerName}");
                }

                handler.OnCodeActionApplied(codeAction.ActionResultContext.ActionIdentifier);
            }            
        }

        public Fx2NLParameters GetFx2NLParameters()
        {
            return new Fx2NLParameters
            {
                 UsageHints = this.UsageHints
            };
        }

        public DefinitionsCheckResult CheckUserDefinedFunctions(string expression)
        {
            return _checkUserDefinedFunctions?.Invoke(expression) ?? throw new NotImplementedException();
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition, LSPExpressionMode mode)
        {
            return mode switch
            {
                LSPExpressionMode.Default => ((IPowerFxScope)this).Suggest(expression, cursorPosition),
                LSPExpressionMode.UserDefiniedFunction => SuggestForUdf(expression, cursorPosition),
                _ => throw new ArgumentException($"Unknown LSP mode {mode}")
            };
        }

        private IIntellisenseResult SuggestForUdf(string expression, int cursorPosition)
        {
            var checkUdf = CheckUserDefinedFunctions(expression);
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

                        var suggestions = BuildTypeSuggestionList(
                            nameResolver.NamedTypes,
                            t => !UserDefinitions.RestrictedParameterTypes.Contains(t._type));

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
                        var suggestions = BuildTypeSuggestionList(
                            nameResolver.NamedTypes,
                            t => !UserDefinitions.RestrictedTypes.Contains(t._type));

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
                    var formula = new Formula(expression, parsedUdf.Body, checkUdf.UDFParserOptions.Culture);
                    var binding = udf.BindBody(checkUdf.UDFBindingSymbols, new Glue2DocumentBinderGlue(), checkUdf.UDFBindingConfig);
                    var cr = _getCheckResult.Invoke(string.Empty);
                    return cr.Engine.Suggest(formula, binding, cursorPosition, this.Services);
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
            IEnumerable<KeyValuePair<DName, FormulaType>> namedTypes,
            Func<FormulaType, bool> includePredicate)
        {
            var list = new IntellisenseSuggestionList();

            foreach (var kvp in namedTypes)
            {
                var typeName = kvp.Key.Value;
                var val = kvp.Value;

                if (!includePredicate(val))
                {
                    continue;
                }

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

        ///// <summary>
        ///// Adds suggesstions for UDF return type & param types
        ///// </summary>
        ///// <param name="data"></param>
        //private void AddSuggestionsForUDFTypes(CanvasIntellisenseData data)
        //{
        //    Contracts.AssertValue(data);

        //    var userDefinitions = ParseUserDefinitions(data.Script, _doc.Properties);
        //    var pos = data.CursorPos;

        //    foreach (var udf in userDefinitions.UDFs)
        //    {
        //        foreach (var arg in udf.Args)
        //        {
        //            if ((arg.TypeIdent != null && pos >= arg.TypeIdent.Span.Min && pos <= arg.TypeIdent.Span.Lim)
        //                || (arg.ColonToken != null && pos >= arg.ColonToken.Span.Min && pos <= arg.ColonToken.Span.Lim))
        //            {
        //                AddSuggestionsForTypes(data);
        //                return;
        //            }
        //        }

        //        if ((udf.ReturnType != null && pos >= udf.ReturnType.Span.Min && pos <= udf.ReturnType.Span.Lim) ||
        //            (udf.ReturnTypeColonToken != null && pos >= udf.ReturnTypeColonToken.Span.Min && pos <= udf.ReturnTypeColonToken.Span.Lim))
        //        {
        //            AddSuggestionsForTypes(data);
        //        }
        //    }
        //}

        //private bool TryGetIntellisenseDataForUDT(ICanvasIntellisenseContext context, ControlProperty property, ControlInfo currentControlInfo, ControlInfo currentScreen, IEnumerable<DefinedType> udts, out CanvasIntellisenseData data)
        //{
        //    Contracts.AssertValue(context);
        //    Contracts.AssertValue(udts);
        //    Contracts.AssertAllValues(udts);

        //    var cursorPosition = context.CursorPosition;

        //    foreach (var udt in udts)
        //    {
        //        if (udt.Type == null || udt.Type.TypeRoot == null)
        //        {
        //            continue;
        //        }

        //        var span = udt.Type.TypeRoot.GetCompleteSpan();

        //        if (span.IsInRange(cursorPosition))
        //        {
        //            data = new CanvasIntellisenseData(context, _config, property, _doc, currentControlInfo, currentScreen, DType.Unknown, null, null, udt.Type, 0, 0, Helper.DefaultIsValidSuggestionFunc, null, Enumerable.Empty<CommentToken>().ToList(), false);
        //            return true;
        //        }
        //    }

        //    data = null;
        //    return false;
        //}

        //internal static bool IsUDFTypePosition(int cursorPos, UDF parsedUDF)
        //{
        //    Contracts.AssertValue(parsedUDF);

        //    foreach (var arg in parsedUDF.Args)
        //    {
        //        if ((arg.TypeIdent != null && arg.TypeIdent.Span.IsInRange(cursorPos))
        //            || (arg.ColonToken != null && arg.ColonToken.Span.IsInRange(cursorPos)))
        //        {
        //            return true;
        //        }
        //    }

        //    if ((parsedUDF.ReturnType != null && parsedUDF.ReturnType.Span.IsInRange(cursorPos)) ||
        //        (parsedUDF.ReturnTypeColonToken != null && parsedUDF.ReturnTypeColonToken.Span.IsInRange(cursorPos)))
        //    {
        //        return true;
        //    }

        //    return false;
        //}

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

        //private void AddSuggestionsForTypes(CanvasIntellisenseData data)
        //{
        //    Contracts.AssertValue(data);

        //    var types = _doc.NameResolver.NamedTypes;

        //    foreach (var type in types)
        //    {
        //        if (!UserDefinitions.RestrictedTypes.Contains(type.Value._type))
        //        {
        //            IntellisenseHelper.AddSuggestion(data, type.Key, SuggestionKind.KeyWord, SuggestionIconKind.Other, DType.String, requiresSuggestionEscaping: false);
        //        }
        //    }
        //}
    }
}
