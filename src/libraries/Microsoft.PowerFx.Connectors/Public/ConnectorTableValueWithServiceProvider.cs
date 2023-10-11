// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public class ConnectorTableValueWithServiceProvider : ConnectorTableValue
    {
        internal IServiceProvider ServiceProvider { get; }

        public ConnectorTableValueWithServiceProvider(ConnectorTableValue tableValue, IServiceProvider serviceProvider)
            : base(tableValue.Name, tableValue._tabularFunctions, (tableValue.IRContext.ResultType as ConnectorTableType).RecordType)
        {
            ServiceProvider = serviceProvider;
        }

        private IEnumerable<DValue<RecordValue>> _cachedRows;

        protected override async Task<IEnumerable<DValue<RecordValue>>> GetRowsInternal()
        {
            if (_cachedRows != null)
            {
                return _cachedRows;
            }

            BaseRuntimeConnectorContext connectorContext = ServiceProvider.GetService<BaseRuntimeConnectorContext>();
            FormulaValue rows = await _getItems.InvokeAsync(Array.Empty<FormulaValue>(), connectorContext, CancellationToken.None).ConfigureAwait(false);

            RecordValue rv = rows as RecordValue;
            TableValue tv = rv.Fields.FirstOrDefault(field => field.Name == "value").Value as TableValue;

            _cachedRows = new InMemoryTableValue(IRContext.NotInSource(_tableType), tv.Rows).Rows;            
            return _cachedRows;
        }

        public override void Refresh()
        {
            _cachedRows = null;
        }
    }
}
