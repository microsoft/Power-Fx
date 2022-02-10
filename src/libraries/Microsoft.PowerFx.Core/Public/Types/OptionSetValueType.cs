// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class OptionSetValueType : FormulaType
    {
        internal OptionSetValueType(IExternalOptionSet optionSet)
            : base(DType.CreateOptionSetValueType(optionSet))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSetValueType"/> class.
        /// Internal use only. Used by legacy clients to represent an un-backed option set, and should be removed.
        /// </summary>
        internal OptionSetValueType()
            : base(new DType(DKind.OptionSetValue))
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
