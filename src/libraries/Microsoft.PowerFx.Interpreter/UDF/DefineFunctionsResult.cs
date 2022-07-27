// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PowerFx.Interpreter.UDF
{
    public class DefineFunctionsResult : IOperationStatus
    {
        private readonly IEnumerable<ExpressionError> _expressionErrors;
        private readonly IEnumerable<FunctionInfo> _functionInfos;

        public IEnumerable<FunctionInfo> FunctionInfo => _functionInfos;
        
        public DefineFunctionsResult(IEnumerable<ExpressionError> expressionErrors, IEnumerable<FunctionInfo> functionInfos)
        {
            _expressionErrors = expressionErrors;
            _functionInfos = functionInfos;
        }

        public IEnumerable<ExpressionError> Errors => _expressionErrors;

        public bool IsSuccess => _expressionErrors.Any();
    }
}
