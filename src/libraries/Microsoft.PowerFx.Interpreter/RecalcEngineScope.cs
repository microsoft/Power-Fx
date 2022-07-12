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

        private readonly BaseRecordType _contextType;

        private RecalcEngineScope(Engine engine, BaseRecordType contextType)
        {
            _engine = engine;
            _contextType = contextType;
        }

        public static RecalcEngineScope FromRecord(Engine engine, BaseRecordType type)
        {
            return new RecalcEngineScope(engine, type);
        }

        public static RecalcEngineScope FromUri(Engine engine, string uri)
        {
            var uriObj = new Uri(uri);
            var contextJson = HttpUtility.ParseQueryString(uriObj.Query).Get("context");
            if (contextJson == null)
            {
                contextJson = "{}";
            }

            return FromJson(engine, contextJson);
        }

        public static RecalcEngineScope FromJson(Engine engine, string json)
        {
            var context = FormulaValue.FromJson(json);
            return FromRecord(engine, (BaseRecordType)context.Type);
        }

        public CheckResult Check(string expression)
        {
            return _engine.Check(expression, _contextType);
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            return _engine.Suggest(expression, _contextType, cursorPosition);
        }

        public string ConvertToDisplay(string expression)
        {
            return _engine.GetDisplayExpression(expression, _contextType);
        }
    }
}
