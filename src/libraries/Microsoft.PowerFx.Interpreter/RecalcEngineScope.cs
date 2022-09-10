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
    public class RecalcEngineScope : IPowerFxScope
    {
        private readonly Engine _engine;
        private readonly RecordType _contextType;
        private readonly ParserOptions _parserOptions;
        
        private RecalcEngineScope(Engine engine, RecordType contextType, ParserOptions options)
        {
            _engine = engine;
            _contextType = contextType;
            _parserOptions = options;
        }

        public static RecalcEngineScope FromRecord(Engine engine, RecordType type, ParserOptions options = null)
        {
            return new RecalcEngineScope(engine, type, options);
        }

        public static RecalcEngineScope FromUri(Engine engine, string uri, ParserOptions options = null)
        {
            var uriObj = new Uri(uri);
            var contextJson = HttpUtility.ParseQueryString(uriObj.Query).Get("context");
            if (contextJson == null)
            {
                contextJson = "{}";
            }

            return FromJson(engine, contextJson, options);
        }

        public static RecalcEngineScope FromJson(Engine engine, string json, ParserOptions options = null)
        {
            var context = FormulaValue.FromJson(json);
            return FromRecord(engine, (RecordType)context.Type, options);
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
