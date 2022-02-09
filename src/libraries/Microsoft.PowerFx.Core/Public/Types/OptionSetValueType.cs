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

        // Constructor for dummy option set values, don't use for valid option sets
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
