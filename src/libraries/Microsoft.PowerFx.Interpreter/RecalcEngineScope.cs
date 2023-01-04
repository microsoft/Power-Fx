// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Web;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Implement a <see cref="IPowerFxScope"/> for intellisense on top of an <see cref="Engine"/> instance.
    /// </summary>
    [Obsolete("Use Engine.CreateEditorScope() instead.")]
    public class RecalcEngineScope : IPowerFxScope
    {
        // Obsoleting this because:
        // - it creates an unnecssary dependy on RecalcEngine, instead of just working with Engine class.
        // - hence lives in interpreter, when it should be in LSP.
        // - superseded by EditorContextScope, which has more editor customizations. 
        // - encourages a strong coupling on FromJson; but context is not json. 

        private readonly Engine _engine;
        private readonly RecordType _contextType;
        private readonly ParserOptions _parserOptions;
        
        public RecalcEngineScope(Engine engine, RecordType contextType, ParserOptions options)
        {
            _engine = engine;
            _contextType = contextType;
            _parserOptions = options;
        }

        public CheckResult Check(string expression)
        {
            return _engine.Check(expression, _contextType, _parserOptions);
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {            
            return _engine.Suggest(Check(expression), cursorPosition);
        }

        public string ConvertToDisplay(string expression)
        {
            return _engine.GetDisplayExpression(expression, _contextType);
        }
    }
}
