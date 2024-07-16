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

                    // We can't execute a query like below for unknown reasons so we'll have to do it in retrieving each table's data
                    // and doing the joins manually (in GetSqlRelationships)
                    // --
                    //    SELECT fk.name 'FK Name', tp.name 'Parent table', cp.name, tr.name 'Refrenced table', cr.name
                    //    FROM sys.foreign_keys fk
                    //    INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
                    //    INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
                    //    INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                    //    INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
                    //    INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id
                    //    ORDER BY tp.name, cp.column_id
                    // --

                    uri = (_uriPrefix ?? string.Empty) + $"/v2/datasets/{dataset}/query/sql";
                    string body =
                        @"{""query"":""select name, object_id, parent_object_id, referenced_object_id from sys.foreign_keys; " +
                        @"select object_id, name from sys.tables; " +
                        @"select constraint_object_id, parent_column_id, parent_object_id, referenced_column_id, referenced_object_id from sys.foreign_key_columns; " +
                        @"select name, object_id, column_id from sys.columns""}";

                    string text2 = await CdpServiceBase.GetObject(_httpClient, $"Get SQL relationships", uri, body, cancellationToken, Logger).ConfigureAwait(false);

                    // Result should be cached
                    sqlRelationships = GetSqlRelationships(text2);

                    // Filter on ParentTable
                    string tbl = tableName.Split('.').Last().Replace("[", string.Empty).Replace("]", string.Empty);
                    sqlRelationships = sqlRelationships.Where(sr => sr.ParentTable == tbl).ToList();
                }

                string connectorName = _uriPrefix.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[1];
                ConnectorType ct = ConnectorFunction.GetConnectorTypeAndTableCapabilities(this, connectorName, "Schema/Items", FormulaValue.New(text), sqlRelationships, ConnectorCompatibility.SwaggerCompatibility, _tabularTable.DatasetName, out string name, out string displayName, out ServiceCapabilities tableCapabilities);

                return new CdpTableDescriptor() { ConnectorType = ct, Name = name, DisplayName = displayName, TableCapabilities = tableCapabilities };
            }

            return new CdpTableDescriptor();
        }

        private bool IsSql() => _uriPrefix.Contains("/sql/");

        private List<SqlRelationship> GetSqlRelationships(string text)
        {
            JsonElement root = JsonDocument.Parse(text).RootElement;

            Result r = root.Deserialize<Result>();

            SqlForeignKey[] fkt = r.ResultSets.Table1;
            SqlTable[] tt = r.ResultSets.Table2;
            SqlForeignKeyColumn[] fkct = r.ResultSets.Table3;
            SqlColumn[] ct = r.ResultSets.Table4;

            List<SqlRelationship> sqlRelationShips = new List<SqlRelationship>();

            foreach (SqlForeignKey fk in fkt)
            {
                foreach (SqlTable tp in tt.Where(tp => fk.parent_object_id == tp.object_id))
                {
                    foreach (SqlTable tr in tt.Where(tr => fk.referenced_object_id == tr.object_id))
                    {
                        foreach (SqlForeignKeyColumn fkc in fkct.Where(fkc => fkc.constraint_object_id == fk.object_id))
                        {
                            foreach (SqlColumn cp in ct.Where(cp => fkc.parent_column_id == cp.column_id && fkc.parent_object_id == cp.object_id))
                            {
                                foreach (SqlColumn cr in ct.Where(cr => fkc.referenced_column_id == cr.column_id && fkc.referenced_object_id == cr.object_id))
                                {
                                    sqlRelationShips.Add(new SqlRelationship()
                                    {
                                        RelationshipName = fk.name,
                                        ParentTable = tp.name,
                                        ColumnName = cp.name,
                                        ReferencedTable = tr.name,
                                        ReferencedColumnName = cr.name,
                                        ColumnId = cp.column_id
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return sqlRelationShips.OrderBy(sr => sr.ParentTable).ThenBy(sr => sr.ColumnId).ToList();
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
        public long ColumnId;

        public override string ToString() => $"{RelationshipName}, {ParentTable}, {ColumnName}, {ReferencedTable}, {ReferencedColumnName}";
    }

    internal class Result
    {
        public ResultSets ResultSets { get; set; }
    }

    internal class ResultSets
    {
        public SqlForeignKey[] Table1 { get; set; }
        public SqlTable[] Table2 { get; set; }
        public SqlForeignKeyColumn[] Table3 { get; set; }
        public SqlColumn[] Table4 { get; set; }
    }

    internal class SqlForeignKey
    {
        public string name { get; set; }
        public long object_id { get; set; }
        public long parent_object_id { get; set; }
        public long referenced_object_id { get; set; }
    }

    internal class SqlTable
    {
        public long object_id { get; set; }
        public string name { get; set; }
    }

    internal class SqlForeignKeyColumn
    {
        public long constraint_object_id { get; set; }
        public long parent_column_id { get; set; }
        public long parent_object_id { get; set; }
        public long referenced_column_id { get; set; }
        public long referenced_object_id { get; set; }
    }

    internal class SqlColumn
    {
        public string name { get; set; }
        public long object_id { get; set; }
        public long column_id { get; set; }
    }

#pragma warning restore SA1516
#pragma warning restore SA1300
}
