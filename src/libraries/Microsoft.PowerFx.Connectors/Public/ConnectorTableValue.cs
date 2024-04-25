// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Connectors.Tabular;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Created by TabularService.GetTableValue
    // Doesn't contain any ServiceProvider which is runtime only
    public class ConnectorTableValue : TableValue, IRefreshable
    {
        public bool IsDelegatable => _tabularService.IsDelegatable;

        protected internal readonly TabularService _tabularService;

        public ConnectorTableValue(TabularService tabularService)
            : base(IRContext.NotInSource(new ConnectorTableType(tabularService.TableType)))
        {
            _tabularService = tabularService;
        }

        internal ConnectorTableValue(IRContext irContext)
            : base(irContext)
        {
        }

        public override IEnumerable<DValue<RecordValue>> Rows => throw new InvalidOperationException("No service context. Make sure to call engine.EnableTabularConnectors().");

        public virtual void Refresh()
        {
        }
    }
}
