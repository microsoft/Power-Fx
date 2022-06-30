// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// This enum represents types which are not supported by PowerFx
    /// but which may be supported by external data formats.
    /// </summary>
    [SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification="n/a")]
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

        public override void Visit(ITypeVisitor visitor)
        {
            throw new NotImplementedException();
        }
    }
}
