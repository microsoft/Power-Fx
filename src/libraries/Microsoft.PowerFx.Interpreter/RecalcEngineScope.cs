// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Web;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Texl.Intellisense;

namespace Microsoft.PowerFx
{
    public class RecalcEngineScope : IPowerFxScope
    {
        private readonly RecalcEngine _engine;

        private readonly FormulaType _contextType;

        private RecalcEngineScope(RecalcEngine engine, FormulaType contextType)
        {
            _engine = engine;
            _contextType = contextType;
        }

        public static RecalcEngineScope FromRecord(RecalcEngine engine, FormulaType type)
        {
            return new RecalcEngineScope(engine, type);
        }

        public static RecalcEngineScope FromUri(RecalcEngine engine, string uri)
        {
            var uriObj = new Uri(uri);
            var contextJson = HttpUtility.ParseQueryString(uriObj.Query).Get("context");
            if (contextJson == null)
            {
                contextJson = "{}";
            }

            return FromJson(engine, contextJson);
        }

        public static RecalcEngineScope FromJson(RecalcEngine engine, string json)
        {
            var context = FormulaValue.FromJson(json);
            return FromRecord(engine, context.Type);
        }

        public CheckResult Check(string expression)
        {
            return _engine.Check(expression, _contextType);
        }

        public IIntellisenseResult Suggest(string expression, int cursorPosition)
        {
            return _engine.Suggest(expression, _contextType, cursorPosition);
        }
    }
}
