// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed class EditorContextScope : IPowerFxScopeV2, IPowerFxScopeFx2NL
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

        public IIntellisenseResult Suggest(string expression, int cursorPosition, LSPMode mode)
        {
            if (mode == LSPMode.Default)
            {
                return ((IPowerFxScope)this).Suggest(expression, cursorPosition);
            }
            else if (mode == LSPMode.UserDefiniedFunction)
            {
                var checkUDF = CheckUserDefinedFunctions(expression);

                var parsedUDFs = checkUDF.ApplyParse();

                foreach (var parsedUDF in parsedUDFs.UDFs)
                {
                    // $$$ maybe cache binding in the other usage.
                    var udf = UserDefinedFunction.CreatePartialFunction(parsedUDF, checkUDF.UDFBindingSymbols, out var functions);
                    var nameResolver = udf.GetUDFNameResolver(checkUDF.UDFBindingSymbols);
                    if (parsedUDF.Body == null)
                    {
                        // No body, so no suggestions, implement this for types.
                        UDFArg cursorArg = null;
                        foreach (var arg in parsedUDF.Args)
                        {
                            // If the cursor is after the colon, then we can suggest types.
                            if (IsUDFArgTypePosition(cursorPosition, arg))
                            {
                                cursorArg = arg;
                                var suggestions = new IntellisenseSuggestionList();
                                foreach (var kvp in nameResolver.NamedTypes)
                                {
                                    if (!UserDefinitions.RestrictedParameterTypes.Contains(kvp.Value._type))
                                    {
                                        var cursor = arg.ColonToken.Span.Lim + 1;         // right after the colon
                                        var typedPrefix = kvp.Key.Value;

                                        var suggestion = new IntellisenseSuggestion(
                                            new UIString(kvp.Key.Value),           // what shows up in UI
                                            SuggestionKind.Type,                   // or Function/Other depending on context
                                            SuggestionIconKind.Other,               // choose appropriate icon kind
                                            kvp.Value._type,                       // DType
                                            kvp.Key.Value,                         // exact match string
                                            -1,                                    // argCount (not relevant here)
                                            string.Empty,            // definition or description
                                            string.Empty);

                                        suggestions.Add(suggestion);
                                    }
                                }

                                var startIndex = arg.TypeIdent?.Span.Min ?? arg.ColonToken.Span.Lim + 1; // if TypeIdent is null, then we are after the colon
                                var data = new UDFIntellisenseData(
                                    replacementStartIndex: startIndex,
                                    replacementLength: cursorPosition,
                                    0,
                                    0,
                                    script: expression,
                                    locale: checkUDF.UDFParserOptions.Culture);

                                suggestions.Sort(checkUDF.UDFParserOptions.Culture);
                                IList<IntellisenseSuggestion> suggestionList = suggestions;
                                var result = new IntellisenseResult(
                                        data,
                                        (IReadOnlyList<IntellisenseSuggestion>)suggestionList,
                                        Enumerable.Empty<TexlFunction>());
                                return result;
                            }
                            else if (IsUDFReturnTypePosition(cursorPosition, parsedUDF))
                            {
                                cursorArg = arg;
                                foreach (var kvp in nameResolver.NamedTypes)
                                {
                                    if (!UserDefinitions.RestrictedTypes.Contains(kvp.Value._type))
                                    {
                                        // add suggestion.
                                        // create IIntellisenseResult below and add name of the kvp as the suggestion.
                                    }

                                    throw new NotImplementedException($"Suggesting types for UDF return type is not implemented yet. Type: {kvp.Key}, Value: {kvp.Value}");
                                }

                                break;
                            }
                        }

                        continue;
                    }

                    var udfBinding = udf.BindBody(
                        checkUDF.UDFBindingSymbols,
                        new Glue2DocumentBinderGlue(),
                        checkUDF.UDFBindingConfig);

                    var bodySpan = parsedUDF.Body.GetCompleteSpan();
                    if (cursorPosition > bodySpan.Min && cursorPosition <= bodySpan.Lim)
                    {
                        // parsedUDF.Body.ToString()
                        var formula = new Formula(expression, parsedUDF.Body, checkUDF.UDFParserOptions.Culture);

                        var cr = _getCheckResult.Invoke(string.Empty);
                        var newCursorPosition = cursorPosition; // - bodySpan.Min;
                        return cr.Engine.Suggest(formula, udfBinding, newCursorPosition, this.Services);
                    }
                }

                throw new ArgumentException($"Cursor position {cursorPosition} is not within any user-defined function in the expression."); // intead of throwing return empty.
            }
            else
            {
                throw new ArgumentException($"Unknown LSP mode {mode}");
            }
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
