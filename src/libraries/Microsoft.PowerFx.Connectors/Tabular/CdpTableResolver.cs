// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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

        public CdpTableResolver(CdpTable tabularTable, HttpClient httpClient, string uriPrefix, bool doubleEncoding, ConnectorLogger logger = null)
        {
            _tabularTable = tabularTable;
            _httpClient = httpClient;
            _uriPrefix = uriPrefix;
            _doubleEncoding = doubleEncoding;

            Logger = logger;
        }

        public async Task<ConnectorType> ResolveTableAsync(string tableName, CancellationToken cancellationToken)
        {
            // out string name, out string displayName, out ServiceCapabilities tableCapabilities
            cancellationToken.ThrowIfCancellationRequested();

            if (_tabularTable.Tables != null)
            {
                RawTable t = _tabularTable.Tables.FirstOrDefault(tbl => tbl.DisplayName == tableName);
                if (t != null)
                {
                    tableName = t.Name;
                }
            }

            string dataset = _doubleEncoding ? CdpServiceBase.DoubleEncode(_tabularTable.DatasetName) : _tabularTable.DatasetName;
            string uri = (_uriPrefix ?? string.Empty) + (UseV2(_uriPrefix) ? "/v2" : string.Empty) + $"/$metadata.json/datasets/{dataset}/tables/{CdpServiceBase.DoubleEncode(tableName)}?api-version=2015-09-01";

            string text = await CdpServiceBase.GetObject(_httpClient, $"Get table metadata", uri, null, cancellationToken, Logger).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            List<SqlRelationship> sqlRelationships = null;

            // for SQL need to get relationships separately as they aren't included by CDP connector
            if (IsSql(_uriPrefix))
            {
                cancellationToken.ThrowIfCancellationRequested();

                uri = (_uriPrefix ?? string.Empty) + $"/v2/datasets/{dataset}/query/sql";
                string body =
                    @"{""query"":""SELECT fk.name AS FK_Name, '[' + sp.name + '].[' + tp.name + ']' AS Parent_Table, cp.name AS Parent_Column, '[' + sr.name + '].[' + tr.name + ']' AS Referenced_Table, cr.name AS Referenced_Column" +
                      @" FROM sys.foreign_keys fk" +
                      @" INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id" +
                      @" INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id" +
                      @" INNER JOIN sys.schemas sp on tp.schema_id = sp.schema_id" +
                      @" INNER JOIN sys.schemas sr on tr.schema_id = sr.schema_id" +
                      @" INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id" +
                      @" INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id" +
                      @" INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id" +
                      @" WHERE '[' + sp.name + '].[' + tp.name + ']' = '" + tableName + "'" + @"""}";

                string text2 = await CdpServiceBase.GetObject(_httpClient, $"Get SQL relationships", uri, body, cancellationToken, Logger).ConfigureAwait(false);

                // Result should be cached
                sqlRelationships = GetSqlRelationships(text2);
            }

            var parts = _uriPrefix.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string connectorName = (parts.Length > 1) ? parts[1] : string.Empty;

            ConnectorType connectorType = ConnectorFunction.GetCdpTableType(this, connectorName, _tabularTable.TableName, "Schema/Items", FormulaValue.New(text), sqlRelationships, ConnectorCompatibility.CdpCompatibility, _tabularTable.DatasetName, 
                                                                            out string name, out string displayName, out TableDelegationInfo delegationInfo, out IEnumerable<OptionSet> optionSets);

            OptionSets = optionSets;

            return connectorType;
        }

        internal static bool IsSql(string uriPrefix) => uriPrefix.Contains("/sql/");

        internal static bool UseV2(string uriPrefix) => uriPrefix.Contains("/sql/") ||
                                                        uriPrefix.Contains("/zendesk/");

        private List<SqlRelationship> GetSqlRelationships(string text)
        {
            RelationshipResult r = JsonSerializer.Deserialize<RelationshipResult>(text);

            var relationships = r.ResultSets.Table1;
            if (relationships == null || relationships.Length == 0)
            {
                return new List<SqlRelationship>();
            }

            List<SqlRelationship> sqlRelationShips = new List<SqlRelationship>();

            foreach (var fk in relationships)
            {
                sqlRelationShips.Add(new SqlRelationship()
                {
                    RelationshipName = fk.FK_Name,
                    ParentTable = fk.Parent_Table,
                    ColumnName = fk.Parent_Column,
                    ReferencedTable = fk.Referenced_Table,
                    ReferencedColumnName = fk.Referenced_Column
                });
            }

            return sqlRelationShips;
        }
    }

#pragma warning disable SA1300 // Element should begin with upper case
#pragma warning disable SA1516 // Element should be separated by a blank line

    internal class SqlRelationship
    {
        public string RelationshipName;
        public string ParentTable;
        public string ColumnName;
        public string ReferencedTable;
        public string ReferencedColumnName;

        public override string ToString() => $"{RelationshipName}, {ParentTable}, {ColumnName}, {ReferencedTable}, {ReferencedColumnName}";
    }

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
