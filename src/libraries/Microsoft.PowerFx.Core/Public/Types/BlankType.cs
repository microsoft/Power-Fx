// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// Represents a Blank (similar to Null) value. BlankType is compatible with other types. 
    /// </summary>
    public class BlankType : FormulaType
    {
        internal BlankType() : base(new DType(DKind.ObjNull))
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
