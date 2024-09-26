// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Entities
{
    public partial class TableCapabilities
    {
        public TableCapabilities(DName name, ServiceCapabilities2 serviceCapabilities2, bool isReadOnly, FormulaType type, string datasetName)
        {
            DataSource = new InternalTableCapabilities(name, serviceCapabilities2, isReadOnly, type._type, datasetName);
        }

        internal IExternalTabularDataSource DataSource;
    }
}
