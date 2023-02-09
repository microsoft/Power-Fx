// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
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
    public sealed class EditorContextScope : IPowerFxScope
    {        
        // List of handlers to get code-fix suggestions. 
        // Key is CodeFixHandler.HandlerName
        private readonly Dictionary<string, CodeFixHandler> _handlers = new Dictionary<string, CodeFixHandler>();

        // Map string (from formula bar) to a CheckResult.
        // The checkResult has all other context (parser options, symbols, engine, etc)
        // This captures the critical invariant: EditorContextScope corresponds to a formula bar where the user just types text, all other context is provided; 
        private readonly Func<string, CheckResult> _getCheckResult;

        internal EditorContextScope(
            Engine engine,
            ParserOptions parserOptions,
            ReadOnlySymbolTable symbols)
            : this((string expr) => new CheckResult(engine)
                    .SetText(expr, parserOptions)
                    .SetBindingInfo(symbols))
        {            
        }        

        public EditorContextScope(Func<string, CheckResult> getCheckResult)
        {
            _getCheckResult = getCheckResult ?? throw new ArgumentNullException(nameof(getCheckResult));
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

            return engine.GetDisplayExpression(expression, symbols);
        }

        IIntellisenseResult IPowerFxScope.Suggest(string expression, int cursorPosition)
        {
            // Suggestions just need the binding, not other things like Dependency Info or errors. 
            var check = _getCheckResult(expression);

            return check.Engine.Suggest(check, cursorPosition);
        }

        #endregion

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
                    var e2 = new Exception($"Handler {handler.Key} threw {e.Message}", e);
                    
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
    }
}
