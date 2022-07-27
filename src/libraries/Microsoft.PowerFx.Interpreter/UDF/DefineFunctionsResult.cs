// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Interpreter.UDF
{
    public class DefineFunctionsResult
    {
        public readonly IEnumerable<ExpressionError> ExpressionErrors;
        public readonly IEnumerable<FunctionInfo> FunctionInfos;
        
        public DefineFunctionsResult(IEnumerable<ExpressionError> expressionErrors, IEnumerable<FunctionInfo> functionInfos)
        {
            ExpressionErrors = expressionErrors;
            FunctionInfos = functionInfos;
        }
    }
}
