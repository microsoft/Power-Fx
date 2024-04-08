// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Implements CDP protocol for Tabular connectors
    public abstract class TabularService
    {
        public string TableName { get; }

        public RecordType RecordType { get; private set; } = null;

        public TableType TableType => _tableType ??= RecordType.ToTable();

        public virtual string Namespace => $"_tbl_{TableName}";

        public bool IsInitialized => RecordType != null;

        private TableType _tableType;

        protected TabularService(IReadOnlyDictionary<string, FormulaValue> globalValues)
        {
            if (!globalValues.TryGetValue("table", out FormulaValue tableName))
            {
                throw new PowerFxConnectorException("Missing 'table' parameter in the global values");
            }

            if (tableName is not StringValue sv)
            {
                throw new PowerFxConnectorException("'table' global value is not of type StringValue");
            }

            TableName = sv.Value;
        }

        public async Task<RecordType> InitAsync(PowerFxConfig config, string tableName, BaseRuntimeConnectorContext runtimeConnectorContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            RecordType = await InitInternalAsync(config, tableName, runtimeConnectorContext, cancellationToken).ConfigureAwait(false);

            return RecordType;
        }

        internal abstract Task<RecordType> InitInternalAsync(PowerFxConfig config, string tableName, BaseRuntimeConnectorContext runtimeConnectorContext, CancellationToken cancellationToken);

        // TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        internal abstract Task<RecordType> GetSchemaAsync(BaseRuntimeConnectorContext runtimeConnectorContext, CancellationToken cancellationToken);

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        public abstract Task<IEnumerable<DValue<RecordValue>>> GetItemsAsync(BaseRuntimeConnectorContext runtimeConnectorContext, CancellationToken cancellationToken);

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}
    }
}
