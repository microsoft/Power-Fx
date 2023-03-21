// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Parser;

namespace Microsoft.PowerFx
{
    internal sealed class UserDefinitionResult
    {
        internal IEnumerable<UserDefinedFunction> UDFs { get; set; }

        internal IEnumerable<ExpressionError> Errors { get; set; }

        internal IEnumerable<NamedFormula> NamedFormulas { get; }

        internal bool HasErrors { get; }

        public UserDefinitionResult(IEnumerable<UserDefinedFunction> uDFs, IEnumerable<ExpressionError> errors, IEnumerable<NamedFormula> namedFormulas)
        {
            UDFs = uDFs;
            Errors = errors;
            NamedFormulas = namedFormulas;

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasErrors = true;
            }
        }
    }
}
