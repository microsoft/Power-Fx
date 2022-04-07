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
        Array, // PowerFx only supports single-column tables
        Object // PowerFx does not support schema-less objects
    }

    /// <summary>
    /// FormulaType that can be used by UntypedObject instances to
    /// indicate that the type of the data does not exist in PowerFx.
    /// </summary>
    public class ExternalType : FormulaType
    {
        public static readonly FormulaType ObjectType = new ExternalType(ExternalTypeKind.Object);
        public static readonly FormulaType ArrayType = new ExternalType(ExternalTypeKind.Array);

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
