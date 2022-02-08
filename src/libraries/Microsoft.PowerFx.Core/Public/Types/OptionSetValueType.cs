// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class OptionSetValueType : FormulaType
    {
        internal OptionSetValueType(IExternalOptionSet optionSet)
            : base(DType.CreateOptionSetType(optionSet))
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }
    }
}
