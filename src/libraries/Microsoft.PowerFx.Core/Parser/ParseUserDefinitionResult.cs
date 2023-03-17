// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;

namespace Microsoft.PowerFx.Core.Parser
{
    internal sealed class ParseUserDefinitionResult
    {
        internal IEnumerable<UDF> UDFs { get; }

        internal IEnumerable<NamedFormula> NamedFormulas { get; }

        internal IEnumerable<TexlError> Errors { get; }

        internal bool HasErrors { get; }

        public ParseUserDefinitionResult(IEnumerable<NamedFormula> namedFormulas, IEnumerable<UDF> uDFs, IEnumerable<TexlError> errors)
        {
            NamedFormulas = namedFormulas;
            UDFs = uDFs;

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasErrors = true;
            }
        }
    }
}
