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

            // Notice that there is no paging here, just get 1 page
            // Use WithRawResults to ignore _getItems return type which is in the form of ![value:*[dynamicProperties:![]]] (ie. without the actual type)
            FormulaValue rowsRaw = await _getItems.InvokeAsync(Array.Empty<FormulaValue>(), connectorContext.WithRawResults(), CancellationToken.None).ConfigureAwait(false);

            if (rowsRaw is ErrorValue ev)
            {
                return Enumerable.Empty<DValue<RecordValue>>();
            }

            StringValue rowsStr = rowsRaw as StringValue;            

            // $$$ Is this always this type?
            RecordValue rv = FormulaValueJSON.FromJson(rowsStr.Value, RecordType.Empty().Add("value", _tableType)) as RecordValue;
            TableValue tv = rv.Fields.FirstOrDefault(field => field.Name == "value").Value as TableValue;

            // The call we make contains more fields and we want to remove them here ('@odata.etag')
            _cachedRows = new InMemoryTableValue(IRContext.NotInSource(_tableType), tv.Rows).Rows;
            return _cachedRows;
        }

        public override void Refresh()
        {
            _cachedRows = null;
        }
    }
}
