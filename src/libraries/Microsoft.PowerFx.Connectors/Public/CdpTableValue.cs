// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.PowerFx.Core.Functions.OData;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpTableValue : BaseTableValue
    {
        internal List<ODataCommand> ODataCommands { get; }

        internal CdpTableValue(TabularTableValue tableValue, IServiceProvider serviceProvider)
            : base(tableValue, serviceProvider, TabularProtocol.Cdp)
        {
            ODataCommands = new List<ODataCommand>();
        }

        public override IEnumerable<DValue<RecordValue>> Rows => _tabularService.GetItemsAsync(ServiceProvider, ODataCommands, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

        // Manages CDP capabilities, at runtime level
        // Returns true if the command is delegatage and was added, false otherwise
        public override bool TryAddODataCommand(ODataCommand command)
        {
            ODataCommands.Add(command);
            return true;            
        }
    }
}