// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    public abstract class CdpService : CdpServiceBase
    {
        private const string NotInitialized = "Tabular service is not initialized.";

        public TableType TableType => RecordType?.ToTable();

        public RecordType RecordType { get; protected internal set; } = null;

        public bool IsInitialized => TableType != null;

        public abstract bool IsDelegable { get; }

        internal abstract IReadOnlyDictionary<string, Relationship> Relationships { get; }

        public abstract HttpClient HttpClient { get; }

        public abstract ConnectorSettings ConnectorSettings { get; }

        public virtual CdpTableValue GetTableValue()
        {
            return IsInitialized
                ? new CdpTableValue(this, Relationships)
                : throw new InvalidOperationException(NotInitialized);
        }

        // TABLE METADATA SERVICE
        // GET: /$metadata.json/datasets/{datasetName}/tables/{tableName}?api-version=2015-09-01
        // Implemented in InitAsync() in derived classes

        // TABLE DATA SERVICE - CREATE
        // POST: /datasets/{datasetName}/tables/{tableName}/items?api-version=2015-09-01

        // TABLE DATA SERVICE - READ
        // GET AN ITEM - GET: /datasets/{datasetName}/tables/{tableName}/items/{id}?api-version=2015-09-01

        // LIST ITEMS - GET: /datasets/{datasetName}/tables/{tableName}/items?$filter=’CreatedBy’ eq ‘john.doe’&$top=50&$orderby=’Priority’ asc, ’CreationDate’ desc
        public Task<IReadOnlyCollection<DValue<RecordValue>>> GetItemsAsync(IServiceProvider serviceProvider, DelegationParameters parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return IsInitialized
                ? GetItemsInternalAsync(serviceProvider, parameters, cancellationToken)
                : throw new InvalidOperationException(NotInitialized);
        }

        public Task<FormulaValue> ExecuteQueryAsync(IServiceProvider serviceProvider, DelegationParameters parameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return IsInitialized
                ? GetItemInternalAsync(serviceProvider, parameters, cancellationToken)
                : throw new InvalidOperationException(NotInitialized);
        }

        protected abstract Task<IReadOnlyCollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, DelegationParameters parameters, CancellationToken cancellationToken);

        protected abstract Task<FormulaValue> GetItemInternalAsync(IServiceProvider serviceProvider, DelegationParameters parameters, CancellationToken cancellationToken);

        // TABLE DATA SERVICE - UPDATE
        // PATCH: /datasets/{datasetName}/tables/{tableName}/items/{id}

        // TABLE DATA SERVICE - DELETE
        // DELETE: /datasets/{datasetName}/tables/{tableName}/items/{id}
    }
}
