// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Types;

#pragma warning disable SA1117

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpTableResolver : ICdpTableResolver
    {
        public ConnectorLogger Logger { get; }

        public IEnumerable<OptionSet> OptionSets { get; private set; }

        private readonly CdpTable _tabularTable;

        private readonly HttpClient _httpClient;

        private readonly string _uriPrefix;

        private readonly bool _doubleEncoding;

        private readonly ConnectorSettings _connectorSettings;

        /// <summary>
        /// Reference to shared metadata cache for async deduplication of table metadata fetches.
        /// </summary>
        /// <remarks>
        /// Key format: {uriPrefix}/v2/$metadata.json/datasets/{encoded-dataset}/tables/{encoded-table}?api-version=2015-09-01[&amp;extractSensitivityLabel=True][&amp;purviewAccountName={account}]
        ///
        /// Caching strategy: Multiple concurrent requests for the same URI share a single task to prevent duplicate network calls.
        ///
        /// Tasks are cached with CancellationToken.None - individual callers can cancel their wait independently.
        /// Failed tasks are automatically removed from cache to allow retry.
        /// Cache is cleared when it reaches MaxCacheSize (1000 entries).
        /// </remarks>
        private readonly ConcurrentDictionary<string, Task<(ConnectorType, IEnumerable<OptionSet>)>> _tableMetadataCache;

        private int _clearInProgress = 0;

        private const int MaxCacheSize = 1000;

        public ConnectorSettings ConnectorSettings => _connectorSettings;

        public CdpTableResolver(CdpTable tabularTable, HttpClient httpClient, string uriPrefix, bool doubleEncoding, ConnectorSettings connectorSettings, ConcurrentDictionary<string, Task<(ConnectorType, IEnumerable<OptionSet>)>> tableMetadataCache, ConnectorLogger logger = null)
        {
            _tabularTable = tabularTable;
            _httpClient = httpClient;
            _uriPrefix = uriPrefix;
            _doubleEncoding = doubleEncoding;
            _connectorSettings = connectorSettings;
            _tableMetadataCache = tableMetadataCache ?? new ConcurrentDictionary<string, Task<(ConnectorType, IEnumerable<OptionSet>)>>();
            Logger = logger;
        }

        public async Task<ConnectorType> ResolveTableAsync(string logicalName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Convert display name to logical name if needed
            if (_tabularTable.Tables != null)
            {
                RawTable t = _tabularTable.Tables.FirstOrDefault(tbl => tbl.DisplayName == logicalName);
                if (t != null)
                {
                    logicalName = t.Name;
                }
            }

            // Build the URI that will be used for both caching and fetching
            string cacheKey = BuildTableMetadataUri(logicalName);

            // Try to get from cache using URI as key (async deduplication pattern)
            // Use CancellationToken.None for cached task to allow multiple callers to share
            var cachedTask = _tableMetadataCache.GetOrAdd(cacheKey, _ => FetchAndCacheTableMetadataAsync(cacheKey, CancellationToken.None));
            var (connectorType, optionSets) = await AwaitWithCancellation(cachedTask, cancellationToken).ConfigureAwait(false);
            OptionSets = optionSets;
            return connectorType;
        }

        /// <summary>
        /// Awaits a cached task while respecting a caller-specific cancellation token.
        /// This allows multiple callers to share a cached task while maintaining independent cancellation.
        /// </summary>
        /// <typeparam name="T">The task result type.</typeparam>
        /// <param name="task">The cached task to await.</param>
        /// <param name="cancellationToken">The caller's cancellation token.</param>
        /// <returns>The task result.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the caller's token is cancelled.</exception>
        private static async Task<T> AwaitWithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            // If token is already cancelled, throw immediately
            cancellationToken.ThrowIfCancellationRequested();

            // If task is already completed, return result directly
            if (task.IsCompleted)
            {
                return await task.ConfigureAwait(false);
            }

            // Race the cached task against cancellation
            var tcs = new TaskCompletionSource<T>();
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                return await completedTask.ConfigureAwait(false);
            }
        }

        private string BuildTableMetadataUri(string logicalName)
        {
            string dataset = _doubleEncoding ? CdpServiceBase.DoubleEncode(_tabularTable.DatasetName) : CdpServiceBase.SingleEncode(_tabularTable.DatasetName);
            string uri = (_uriPrefix ?? string.Empty) + (UseV2(_uriPrefix) ? "/v2" : string.Empty) + $"/$metadata.json/datasets/{dataset}/tables/{CdpServiceBase.DoubleEncode(logicalName)}?api-version=2015-09-01";

            if (_connectorSettings.ExtractSensitivityLabel)
            {
                uri += $"&extractSensitivityLabel=True";
                if (!string.IsNullOrEmpty(_connectorSettings.PurviewAccountName))
                {
                    uri += $"&purviewAccountName={_connectorSettings.PurviewAccountName}";
                }
            }

            return uri;
        }

        private async Task<(ConnectorType, IEnumerable<OptionSet>)> FetchAndCacheTableMetadataAsync(string uri, CancellationToken cancellationToken)
        {
            try
            {
                // Atomic check and clear - only one thread will clear
                if (_tableMetadataCache.Count >= MaxCacheSize)
                {
                    // CompareExchange returns 0 only if _clearInProgress was 0 (not in progress)
                    if (Interlocked.CompareExchange(ref _clearInProgress, 1, 0) == 0)
                    {
                        try
                        {
                            _tableMetadataCache.Clear();
                        }
                        finally
                        {
                            // Reset flag to allow future clears
                            Interlocked.Exchange(ref _clearInProgress, 0);
                        }
                    }
                }

                return await FetchTableMetadataAsync(uri, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Remove failed task from cache to allow retry on next call
                _tableMetadataCache.TryRemove(uri, out _);
                throw;
            }
        }

        private async Task<(ConnectorType, IEnumerable<OptionSet>)> FetchTableMetadataAsync(string uri, CancellationToken cancellationToken)
        {
            string text = await CdpServiceBase.GetObject(_httpClient, $"Get table metadata", uri, null, cancellationToken, Logger).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException($"{nameof(ResolveTableAsync)} didn't receive any response");
            }

            // We don't need SQL relationships as those are not equivalent as those we find in Dataverse or ServiceNow
            // Foreign Key constrainsts are not enough and equivalent.
            // Only keeping code for future use, if we'd need to get those relationships.
            //
            //List<SqlRelationship> sqlRelationships = null;
            //
            //// for SQL need to get relationships separately as they aren't included by CDP connector
            //if (IsSql(_uriPrefix))
            //{
            //    cancellationToken.ThrowIfCancellationRequested();
            //
            //    uri = (_uriPrefix ?? string.Empty) + $"/v2/datasets/{dataset}/query/sql";
            //    string body =
            //        @"{""query"":""SELECT fk.name AS FK_Name, '[' + sp.name + '].[' + tp.name + ']' AS Parent_Table, cp.name AS Parent_Column, '[' + sr.name + '].[' + tr.name + ']' AS Referenced_Table, cr.name AS Referenced_Column" +
            //          @" FROM sys.foreign_keys fk" +
            //          @" INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id" +
            //          @" INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id" +
            //          @" INNER JOIN sys.schemas sp on tp.schema_id = sp.schema_id" +
            //          @" INNER JOIN sys.schemas sr on tr.schema_id = sr.schema_id" +
            //          @" INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id" +
            //          @" INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id" +
            //          @" INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id" +
            //          @" WHERE '[' + sp.name + '].[' + tp.name + ']' = '" + tableName + "'" + @"""}";
            //
            //    string text2 = await CdpServiceBase.GetObject(_httpClient, $"Get SQL relationships", uri, body, cancellationToken, Logger).ConfigureAwait(false);
            //
            //    // Result should be cached
            //    sqlRelationships = GetSqlRelationships(text2);
            //}

            var parts = _uriPrefix.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string connectorName = (parts.Length > 1) ? parts[1] : string.Empty;

            ConnectorType connectorType = ConnectorFunction.GetCdpTableType(this, connectorName, _tabularTable.TableName, "Schema/Items", FormulaValue.New(text), _connectorSettings, _tabularTable.DatasetName,
                                                                            out TableDelegationInfo delegationInfo, out IEnumerable<OptionSet> optionSets);

            return (connectorType, optionSets);
        }

        internal static bool IsSql(string uriPrefix) => uriPrefix.Contains("/sql/");

        internal static bool UseV2(string uriPrefix) => uriPrefix.Contains("/sql/") ||
                                                        uriPrefix.Contains("/zendesk/");

        // Only keeping code for reference
        //
        //private List<SqlRelationship> GetSqlRelationships(string text)
        //{
        //    RelationshipResult r = JsonSerializer.Deserialize<RelationshipResult>(text);

        //    var relationships = r.ResultSets.Table1;
        //    if (relationships == null || relationships.Length == 0)
        //    {
        //        return new List<SqlRelationship>();
        //    }

        //    List<SqlRelationship> sqlRelationShips = new List<SqlRelationship>();

        //    foreach (var fk in relationships)
        //    {
        //        sqlRelationShips.Add(new SqlRelationship()
        //        {
        //            RelationshipName = fk.FK_Name,
        //            ParentTable = fk.Parent_Table,
        //            ColumnName = fk.Parent_Column,
        //            ReferencedTable = fk.Referenced_Table,
        //            ReferencedColumnName = fk.Referenced_Column
        //        });
        //    }

        //    return sqlRelationShips;
        //}
    }

#pragma warning disable SA1300 // Element should begin with upper case
#pragma warning disable SA1516 // Element should be separated by a blank line

    // Only keeping code for reference
    //
    //internal class SqlRelationship
    //{
    //    public string RelationshipName;
    //    public string ParentTable;
    //    public string ColumnName;
    //    public string ReferencedTable;
    //    public string ReferencedColumnName;
    //
    //    public override string ToString() => $"{RelationshipName}, {ParentTable}, {ColumnName}, {ReferencedTable}, {ReferencedColumnName}";
    //}

    internal class RelationshipResult
    {
        public RelationshipResultSets ResultSets { get; set; }
    }

    internal class RelationshipResultSets
    {
        public FKRelationship[] Table1 { get; set; }
    }

    internal class FKRelationship
    {
        public string FK_Name { get; set; }

        public string Parent_Table { get; set; }

        public string Parent_Column { get; set; }

        public string Referenced_Table { get; set; }

        public string Referenced_Column { get; set; }
    }

#pragma warning restore SA1516
#pragma warning restore SA1300
}
