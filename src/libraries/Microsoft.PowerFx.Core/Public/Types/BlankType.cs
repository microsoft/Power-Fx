﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represents a Blank (similar to Null) value. BlankType is compatible with other types. 
    /// </summary>
    public class BlankType : FormulaType
    {
        internal BlankType()
            : base(new DType(DKind.ObjNull))
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        public override string ToString()
        {
            return "Blank";
        }

        internal override string DefaultExpressionValue()
        {
            return "Blank()";
        }
    }
}
