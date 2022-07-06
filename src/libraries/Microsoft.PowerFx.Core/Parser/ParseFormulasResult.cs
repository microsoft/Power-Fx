// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Parser
{
    internal class ParseFormulasResult
    {
        internal IEnumerable<KeyValuePair<IdentToken, TexlNode>> NamedFormulas { get; }

        internal IEnumerable<TexlError> Errors { get; }

        internal bool HasError { get; }

        public ParseFormulasResult(IEnumerable<KeyValuePair<IdentToken, TexlNode>> namedFormulas, List<TexlError> errors)
        {
            Contracts.AssertValue(namedFormulas);

            if (errors?.Any() ?? false)
            {
                Errors = errors;
                HasError = true;
            }

            NamedFormulas = namedFormulas;
        }
    }
}
