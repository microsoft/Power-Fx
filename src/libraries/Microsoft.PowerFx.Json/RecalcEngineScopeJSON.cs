// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Web;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public class RecalcEngineScopeJSON
    {
#pragma warning disable CA1041
        [Obsolete]
        public static RecalcEngineScope FromUri(Engine engine, string uri, ParserOptions options = null)
        {
            var uriObj = new Uri(uri);
            var contextJson = HttpUtility.ParseQueryString(uriObj.Query).Get("context");
            contextJson ??= "{}";

            return FromJson(engine, contextJson, options);
        }

        [Obsolete]
        public static RecalcEngineScope FromJson(Engine engine, string json, ParserOptions options = null)
        {
            var context = FormulaValueJSON.FromJson(json);
            return FromRecord(engine, (RecordType)context.Type, options);
        }

        [Obsolete]
        public static RecalcEngineScope FromRecord(Engine engine, RecordType type, ParserOptions options = null)
        {
            return new RecalcEngineScope(engine, type, options);
        }

#pragma warning restore CA1041
    }
}
