// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Web;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Implement a <see cref="IPowerFxScope"/> for intellisense on top of an <see cref="Engine"/> instance.
    /// </summary>
    public class RecalcEngineScope : IPowerFxScope
    {
        private readonly Engine _engine;

        private readonly RecordType _contextType;

        private RecalcEngineScope(Engine engine, RecordType contextType)
        {
            _engine = engine;
            _contextType = contextType;
        }

        public static RecalcEngineScope FromRecord(Engine engine, RecordType type)
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
            return FromRecord(engine, (RecordType)context.Type);
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
