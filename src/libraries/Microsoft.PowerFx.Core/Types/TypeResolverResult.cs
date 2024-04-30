// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Types
{
    internal class TypeResolverResult
    {
        public IEnumerable<KeyValuePair<DName, FormulaType>> ResolvedTypes { get; }

        public IEnumerable<TexlError> Errors { get; }

        public TypeResolverResult(IEnumerable<KeyValuePair<DName, FormulaType>> resolvedTypes, IEnumerable<TexlError> errors)
        {
            this.ResolvedTypes = resolvedTypes;
            this.Errors = errors;
        }
    }
}
