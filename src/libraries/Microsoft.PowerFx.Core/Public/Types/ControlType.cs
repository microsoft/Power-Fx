// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Representing UI control type.
    /// </summary>
    public class ControlType : AggregateType
    {
        internal ControlType(DType type)
            : base(type)
        {
        }

        public override void Visit(ITypeVistor visitor)
        {
            visitor.Visit(this);
        }
    }
}
