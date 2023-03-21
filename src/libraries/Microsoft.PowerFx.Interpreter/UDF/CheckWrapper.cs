// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.UDF
{
     /// <summary>
     /// CheckWrapper delays the evaluation of the body to the Get call while taking in all the parameters needed to make the Check call.
     /// </summary>
    internal class CheckWrapper
    {
        private readonly ParseResult _parseResult;
        private readonly RecordType _parameterType;
        private readonly RecalcEngine _engine;
        public readonly ParserOptions ParserOptions;

        public CheckWrapper(RecalcEngine engine, ParseResult parseResult, RecordType parameterType = null, bool isImperative = false, bool numberIsFloat = false)
        {
            _engine = engine;
            _parseResult = parseResult;
            _parameterType = parameterType;

            ParserOptions = new ParserOptions()
            {
                Culture = _engine.Config.CultureInfo,
                AllowsSideEffects = isImperative,
                NumberIsFloat = numberIsFloat,
            };

            _parseResult.Options = ParserOptions;
        }

        public CheckResult Get() => _engine.Check(_parseResult, _parameterType);
    }
}
