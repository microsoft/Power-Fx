// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    internal class BinderCheckTypeResult
    {
        public bool Success { get; init; }

        public IReadOnlyCollection<BinderCoercionResult> Coercions { get; init; }
    }

    internal class BinderCoercionResult
    {
        public TexlNode Node { get; init; }

        public DType CoercedType { get; init; }
    }
}
