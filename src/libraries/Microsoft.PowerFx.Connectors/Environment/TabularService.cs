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
        public TableType TableType { get; private set; } = null;

        public bool IsInitialized => TableType != null;

        public virtual ConnectorTableValue GetTableValue()
        {
            return IsInitialized
                ? new ConnectorTableValue(this, TableType)
                : throw new InvalidOperationException("Tabular service is not initialized.");
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RecordType recordType = await GetSchemaAsync(cancellationToken).ConfigureAwait(false);
            TableType = recordType?.ToTable();
        }

        // TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        protected abstract Task<RecordType> GetSchemaAsync(CancellationToken cancellationToken);

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
