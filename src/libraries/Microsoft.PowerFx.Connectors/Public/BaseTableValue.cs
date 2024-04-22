// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.PowerFx.Core.Functions.OData;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class BaseTableValue : TabularTableValue, ISupportsODataCommands
    {
        internal IServiceProvider ServiceProvider { get; }

        internal BaseTableValue(TabularTableValue tableValue, IServiceProvider serviceProvider, TabularProtocol protocol = TabularProtocol.None)
            : base(tableValue._tabularService, tableValue.IRContext.ResultType as TableType, protocol)
        {
            ServiceProvider = serviceProvider;
        }

        public override IEnumerable<DValue<RecordValue>> Rows => _tabularService.GetItemsAsync(ServiceProvider, null, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

        // No delegation supported by default
        public virtual bool TryAddODataCommand(ODataCommand command)
        {
            return false;
        }
    }
}