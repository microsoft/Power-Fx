// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Binding
{
    internal readonly struct BinderCheckTypeResult
    {
        private readonly IReadOnlyCollection<BinderCoercionResult> _coercions;

        public bool Success { get; init; }

        // The binder has an imperative function called SetCoercedType,
        // which may be called multiple times in a given type checking function
        // This collection represents repeated calls to that function
        public IReadOnlyCollection<BinderCoercionResult> Coercions
        {
            get => _coercions ?? new List<BinderCoercionResult>();
            init => _coercions = value;
        }

        public TexlNode Node { get; init; }

        public DType NodeType { get; init; }
    }

    internal readonly struct BinderCoercionResult
    {
        public TexlNode Node { get; init; }

        public DType CoercedType { get; init; }
    }
}
