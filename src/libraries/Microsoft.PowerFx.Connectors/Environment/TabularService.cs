// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions.OData;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class TabularService
    {
        private const string NotInitialized = "Tabular service is not initialized.";

        public TableType TableType { get; private set; } = null;

        public bool IsInitialized => TableType != null;

        public abstract TabularProtocol Protocol { get; }

        public virtual TabularTableValue GetTableValue()
        {
            return IsInitialized
                ? new TabularTableValue(this, TableType, Protocol)
                : throw new InvalidOperationException(NotInitialized);
        }

        protected void SetTableType(RecordType recordType)
        {
            TableType = recordType?.ToTable();
        }

        // TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        // Implemented in InitAsync() in derived classes

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        public Task<ICollection<DValue<RecordValue>>> GetItemsAsync(IServiceProvider serviceProvider, IList<ODataCommand> oDataCommands, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return IsInitialized
                ? GetItemsInternalAsync(serviceProvider, oDataCommands, cancellationToken)
                : throw new InvalidOperationException(NotInitialized);
        }

        protected abstract Task<ICollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, IList<ODataCommand> oDataCommands, CancellationToken cancellationToken);

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}
    }
}
