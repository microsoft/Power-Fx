// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal sealed class UserDefinitionResult
    {
        internal IEnumerable<UserDefinedFunction> UDFs { get; set; }

        internal IEnumerable<TexlError> Errors { get; set; }

        internal IEnumerable<NamedFormula> NamedFormulas { get; }

        internal IEnumerable<KeyValuePair<string, FormulaType>> DefinedTypes { get; }

        internal bool HasErrors { get; }

        public UserDefinitionResult(IEnumerable<UserDefinedFunction> uDFs, IEnumerable<TexlError> errors, IEnumerable<NamedFormula> namedFormulas, IEnumerable<KeyValuePair<string, FormulaType>> definedTypes)
        {
            UDFs = uDFs;
            NamedFormulas = namedFormulas;
            DefinedTypes = definedTypes;

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasErrors = true;
            }
        }
    }
}
