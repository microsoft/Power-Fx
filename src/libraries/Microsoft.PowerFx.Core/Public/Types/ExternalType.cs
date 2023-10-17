// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// This enum represents types which are not supported by PowerFx
    /// but which may be supported by external data formats.
    /// </summary>
    public enum ExternalTypeKind
    {
        Array,  // Power Fx only supports single-column tables
        Object, // Power Fx does not support schema-less objects
        ArrayAndObject, // Supports Array indexing and Property access
        UntypedNumber, // Could be mapped to either a Power Fx Float or a Decimal, it is open to interpretation
    }

    /// <summary>
    /// FormulaType that can be used by UntypedObject instances to
    /// indicate that the type of the data does not exist in PowerFx.
    /// </summary>
    public class ExternalType : FormulaType
    {
        public static readonly FormulaType ObjectType = new ExternalType(ExternalTypeKind.Object);
        public static readonly FormulaType ArrayType = new ExternalType(ExternalTypeKind.Array);
        public static readonly FormulaType ArrayAndObject = new ExternalType(ExternalTypeKind.ArrayAndObject);
        public static readonly FormulaType UntypedNumber = new ExternalType(ExternalTypeKind.UntypedNumber);

        public ExternalTypeKind Kind { get; }

        public ExternalType(ExternalTypeKind kind)
            : base(new DType(DKind.Unknown))
        {
            Kind = kind;
        }

        public override void Visit(ITypeVisitor vistor)
        {
            throw new NotImplementedException();
        }
    }
}
