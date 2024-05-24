// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Used in TabularIRVisitor
    internal class TabularDType : DType
    {
        internal TableType TableType;

        internal TabularDType(TableType tableType)
            : base(DKind.Table, tableType._type.TypeTree, null, tableType._type.DisplayNameProvider)
        {
            TableType = tableType;

            if (tableType._type.AssociatedDataSources != null)
            {
                foreach (IExternalTabularDataSource ds in tableType._type.AssociatedDataSources)
                {
                    AssociatedDataSources.Add(ds);
                }
            }
        }
    }
}
