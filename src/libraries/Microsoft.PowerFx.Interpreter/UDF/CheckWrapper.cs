// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.UDF
{
    internal class CheckWrapper
    {
        private readonly string _expressionText;
        private readonly RecordType _parameterType;
        private readonly RecalcEngine _engine;

        public CheckWrapper(RecalcEngine engine, string expressionText, RecordType parameterType = null)
        {
            _engine = engine;
            _expressionText = expressionText;
            _parameterType = parameterType;
        }

        public CheckResult Get() => _engine.Check(_expressionText, _parameterType);
    }
}
