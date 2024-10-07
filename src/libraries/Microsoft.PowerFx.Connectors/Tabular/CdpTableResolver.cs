// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    internal class CdpTableResolver : ICdpTableResolver
    {
        public ConnectorLogger Logger { get; }

        private readonly CdpTable _tabularTable;

        private readonly HttpClient _httpClient;

        private readonly string _uriPrefix;

        private readonly bool _doubleEncoding;

        private readonly JsonSerializerOptions _deserializationOptions = new ()
        {
            PropertyNameCaseInsensitive = true
        };

        // Temporary hack to generate ADS
        public bool GenerateADS { get; init; }

        public CdpTableResolver(CdpTable tabularTable, HttpClient httpClient, string uriPrefix, bool doubleEncoding, ConnectorLogger logger = null)
        {
            _tabularTable = tabularTable;
            _httpClient = httpClient;
            _uriPrefix = uriPrefix;
            _doubleEncoding = doubleEncoding;

            Logger = logger;
        }

        public async Task<CdpTableDescriptor> ResolveTableAsync(string tableName, CancellationToken cancellationToken)
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
            string uri = (_uriPrefix ?? string.Empty) + (IsSql() ? "/v2" : string.Empty) + $"/$metadata.json/datasets/{dataset}/tables/{CdpServiceBase.DoubleEncode(tableName)}?api-version=2015-09-01";

            string text = await CdpServiceBase.GetObject(_httpClient, $"Get table metadata", uri, null, cancellationToken, Logger).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(text))
            {
                List<SqlRelationship> sqlRelationships = null;

                // for SQL need to get relationships separately as they aren't included by CDP connector
                if (IsSql())
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
                else if (IsOracle())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    uri = (_uriPrefix ?? string.Empty) + $"/datasets/{dataset}/query/oracle";
                    string body =
                        @"{""query"":""SELECT TAB_CONS.CONSTRAINT_NAME AS FK_Name, '[' || TAB_CONS.OWNER || '].[' || TAB_CONS.TABLE_NAME || ']' AS Parent_Table, TAB_CONS_COLS.COLUMN_NAME AS Parent_Column, '[' || REF_CONS.OWNER || '].[' || REF_CONS.TABLE_NAME || ']' AS Referenced_Table, REF_CONS_COLS.COLUMN_NAME AS Referenced_Column" +
                          @" FROM ALL_CONSTRAINTS TAB_CONS" +
                          @" INNER JOIN ALL_CONS_COLUMNS TAB_CONS_COLS ON TAB_CONS.CONSTRAINT_NAME = TAB_CONS_COLS.CONSTRAINT_NAME AND TAB_CONS.OWNER = TAB_CONS_COLS.OWNER" +
                          @" INNER JOIN ALL_CONSTRAINTS REF_CONS ON TAB_CONS.R_CONSTRAINT_NAME = REF_CONS.CONSTRAINT_NAME" +
                          @" INNER JOIN ALL_CONS_COLUMNS REF_CONS_COLS ON REF_CONS.CONSTRAINT_NAME = REF_CONS_COLS.CONSTRAINT_NAME AND REF_CONS.OWNER = REF_CONS_COLS.OWNER" +
                          @" WHERE '[' || TAB_CONS.OWNER || '].[' || TAB_CONS.TABLE_NAME || ']' = '" + tableName + "'" + @"""}";

                    sqlRelationships = GetSqlRelationships(await CdpServiceBase.GetObject(_httpClient, $"Get Oracle relationships", uri, body, cancellationToken, Logger).ConfigureAwait(false));
                }

                string connectorName = _uriPrefix.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[1];
                ConnectorType ct = ConnectorFunction.GetConnectorTypeAndTableCapabilities(this, connectorName, "Schema/Items", FormulaValue.New(text), sqlRelationships, ConnectorCompatibility.CdpCompatibility, _tabularTable.DatasetName, out string name, out string displayName, out ServiceCapabilities tableCapabilities);

                return new CdpTableDescriptor() { ConnectorType = ct, Name = name, DisplayName = displayName, TableCapabilities = tableCapabilities };
            }

            return new CdpTableDescriptor();
        }

        private bool IsSql() => _uriPrefix.Contains("/sql/");

        private bool IsOracle() => _uriPrefix.Contains("/oracle/");

        private List<SqlRelationship> GetSqlRelationships(string text)
        {
            RelationshipResult r = JsonSerializer.Deserialize<RelationshipResult>(text, _deserializationOptions);

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
