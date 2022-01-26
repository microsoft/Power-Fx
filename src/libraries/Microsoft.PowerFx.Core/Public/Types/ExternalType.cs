// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// This enum represents types which are not supported by PowerFx
    /// but which may be supported by external data formats.
    /// </summary>
    public enum ExternalTypeKind
    {
        Array,
        Object
    }

    /// <summary>
    /// FormulaType that can be used by CustomObject instances to
    /// indicate that the type of the data does not exist in PowerFx,
    /// such as Arrays (PowerFx only supports Tables) or schema-less
    /// objects.
    /// </summary>
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
