// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public enum ExternalTypeKind
    {
        Array,
        Object
    }

    public class ExternalType : FormulaType
    {
        public ExternalTypeKind Kind { get; }

        public ExternalType(ExternalTypeKind kind)
            : base(new DType(DKind.Unknown))
        {
            Kind = kind;
        }

        public override void Visit(ITypeVistor vistor)
        {
            throw new NotImplementedException();
        }
    }
}
