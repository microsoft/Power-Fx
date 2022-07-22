// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Interpreter
{
    internal class UDFLazyBinder
    {
        private readonly UserDefinedTexlFunction _function;
        
        private readonly List<ExpressionError> _expressionError = new List<ExpressionError>();

        public readonly string Name;

        public UDFLazyBinder(UserDefinedTexlFunction texlFunction, string name)
        { 
            _function = texlFunction;
            Name = name;
        }

        // Used when trying to define a function, there is an error before binding, such as the function name already being defined.
        public UDFLazyBinder(ExpressionError expressionError, string name)
        {
            _expressionError.Add(expressionError);
            Name = name;
        }

        public IEnumerable<ExpressionError> Bind()
        {
            if (_expressionError.Any())
            {
                return _expressionError;
            }

            return _function.Bind();
        } 
    }
}
