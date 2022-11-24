// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx
{
    public static class EditorEngineExtensions
    {
        // A scope is the context for a specific formula bar. 
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
    public sealed class EditorContextScope : IPowerFxScope, IPowerFxScopeQuickFix
    {
        internal readonly Engine _engine;
        internal readonly ParserOptions _parserOptions;
        internal readonly ReadOnlySymbolTable _symbols;

        // List of handlers to get code-fix suggestions. 
        private readonly List<ICodeFixHandler> _handlers = new List<ICodeFixHandler>();

        internal EditorContextScope(
            Engine engine,
            ParserOptions parserOptions,
            ReadOnlySymbolTable symbols)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _parserOptions = parserOptions ?? new ParserOptions();
            _symbols = symbols ?? new SymbolTable();
        }

        #region IPowerFxScope

        // The editor scope has all other context needed to produce a CheckResult  from just the 
        // text in the editor. 
        public CheckResult Check(string expression)
        {
            return _engine.Check(expression, _parserOptions, _symbols);
        }

        string IPowerFxScope.ConvertToDisplay(string expression)
        {
            return _engine.GetDisplayExpression(expression, _symbols);
        }

        IIntellisenseResult IPowerFxScope.Suggest(string expression, int cursorPosition)
        {
            var check = Check(expression);
            return _engine.Suggest(check, cursorPosition);
        }

        #endregion

        public void AddQuickFixHandler(ICodeFixHandler codeFixHandler)
        {
            _handlers.Add(codeFixHandler ?? throw new ArgumentNullException(nameof(codeFixHandler)));
        }

        CodeActionResult[] IPowerFxScopeQuickFix.Suggest(string expression)
        {
            var check = Check(expression);

            var list = new List<CodeActionResult>();
            
            // Show fixes for both warnings and errors. 
            foreach (var handler in _handlers)
            {
                var fixes = handler.SuggestFixesAsync(_engine, check, CancellationToken.None).Result;
                if (fixes != null)
                {
                    list.AddRange(fixes);
                }
            }            

            return list.ToArray();
        }
    }
}
