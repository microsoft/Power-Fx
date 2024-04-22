// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Created by TabularService.GetTableValue
    // Doesn't contain any ServiceProvider which is for runtime
    public class TabularTableValue : TableValue
    {
        protected internal readonly TabularService _tabularService;
        protected internal readonly TabularProtocol _protocol;

        public TabularTableValue(TabularService tabularService, TableType tableType, TabularProtocol protocol)
            : base(IRContext.NotInSource(new TabularTableType(tableType, protocol)))
        {
            _tabularService = tabularService;
            _protocol = protocol;
        }

        internal TabularTableValue(IRContext irContext)
            : base(irContext)
        {
        }

        public override IEnumerable<DValue<RecordValue>> Rows => throw new InvalidOperationException("No service context. Make sure to call engine.EnableTabularConnectors().");
    }
}
