// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class TabularService
    {
        public RecordType RecordType { get; private set; } = null;

        public TableType TableType => _tableType ??= RecordType.ToTable();

        public bool IsInitialized => RecordType != null;

        private TableType _tableType;

        public virtual ConnectorTableValue GetTableValue()
        {
            return RecordType == null
                ? throw new InvalidOperationException("Tabular service is not initialized.")
                : new ConnectorTableValue(this, RecordType);
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();            
            RecordType = await GetSchemaAsync(cancellationToken).ConfigureAwait(false);
        }

        // TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        public abstract Task<RecordType> GetSchemaAsync(CancellationToken cancellationToken);

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        public abstract Task<ICollection<DValue<RecordValue>>> GetItemsAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}
    }
}
