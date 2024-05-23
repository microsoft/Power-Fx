// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorTableValueWithServiceProvider : ConnectorTableValue
    {
        internal IServiceProvider ServiceProvider { get; }

        internal ConnectorTableValueWithServiceProvider(ConnectorTableValue tableValue, IServiceProvider serviceProvider)
            : base(tableValue._tabularService, tableValue._connectorType)
        {
            ServiceProvider = serviceProvider;
        }

        private IReadOnlyCollection<DValue<RecordValue>> _cachedRows;

        public override IEnumerable<DValue<RecordValue>> Rows => GetRowsInternal().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task<IEnumerable<DValue<RecordValue>>> GetRowsInternal()
        {
            if (_cachedRows != null)
            {
                return _cachedRows;
            }

            _cachedRows = await _tabularService.GetItemsAsync(ServiceProvider, null /* no OData filtering */, CancellationToken.None).ConfigureAwait(false);

            return _cachedRows;
        }

        public override void Refresh()
        {
            _cachedRows = null;
        }
    }
}
