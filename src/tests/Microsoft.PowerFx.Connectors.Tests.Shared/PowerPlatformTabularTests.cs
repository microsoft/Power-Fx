// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class PowerPlatformTabularTests
    {
        private readonly ITestOutputHelper _output;

        public PowerPlatformTabularTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task SQL_CdpTabular_GetTables()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "c1a4e9f52ec94d55bb82f319b3e33a6a";
            string jwt = "eyJ0eXAiOiJKV1QiL...";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            testConnector.SetResponseFromFile(@"Responses\SQL GetDatasetsMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(dm);
            Assert.Null(dm.Blob);

            Assert.Equal("{server},{database}", dm.DatasetFormat);
            Assert.NotNull(dm.Tabular);
            Assert.Equal("dataset", dm.Tabular.DisplayName);
            Assert.Equal("mru", dm.Tabular.Source);
            Assert.Equal("Table", dm.Tabular.TableDisplayName);
            Assert.Equal("Tables", dm.Tabular.TablePluralName);
            Assert.Equal("single", dm.Tabular.UrlEncoding);
            Assert.NotNull(dm.Parameters);
            Assert.Equal(2, dm.Parameters.Count);

            Assert.Equal("Server name.", dm.Parameters.First().Description);
            Assert.Equal("server", dm.Parameters.First().Name);
            Assert.True(dm.Parameters.First().Required);
            Assert.Equal("string", dm.Parameters.First().Type);
            Assert.Equal("double", dm.Parameters.First().UrlEncoding);
            Assert.Null(dm.Parameters.First().XMsDynamicValues);
            Assert.Equal("Server name", dm.Parameters.First().XMsSummary);

            Assert.Equal("Database name.", dm.Parameters.Skip(1).First().Description);
            Assert.Equal("database", dm.Parameters.Skip(1).First().Name);
            Assert.True(dm.Parameters.Skip(1).First().Required);
            Assert.Equal("string", dm.Parameters.Skip(1).First().Type);
            Assert.Equal("double", dm.Parameters.Skip(1).First().UrlEncoding);
            Assert.NotNull(dm.Parameters.Skip(1).First().XMsDynamicValues);
            Assert.Equal("/v2/databases?server={server}", dm.Parameters.Skip(1).First().XMsDynamicValues.Path);
            Assert.Equal("value", dm.Parameters.Skip(1).First().XMsDynamicValues.ValueCollection);
            Assert.Equal("Name", dm.Parameters.Skip(1).First().XMsDynamicValues.ValuePath);
            Assert.Equal("DisplayName", dm.Parameters.Skip(1).First().XMsDynamicValues.ValueTitle);
            Assert.Equal("Database name", dm.Parameters.Skip(1).First().XMsSummary);

            CdpDataSource cds = new CdpDataSource("pfxdev-sql.database.windows.net,connectortest");

            testConnector.SetResponseFromFiles(@"Responses\SQL GetDatasetsMetadata.json", @"Responses\SQL GetTables.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(tables);
            Assert.Equal(4, tables.Count());
            Assert.Equal("[dbo].[Customers],[dbo].[Orders],[dbo].[Products],[sys].[database_firewall_rules]", string.Join(",", tables.Select(t => t.TableName)));
            Assert.Equal("Customers,Orders,Products,database_firewall_rules", string.Join(",", tables.Select(t => t.DisplayName)));

            CdpTable connectorTable = tables.First(t => t.DisplayName == "Customers");

            Assert.False(connectorTable.IsInitialized);
            Assert.Equal("Customers", connectorTable.DisplayName);

            testConnector.SetResponseFromFiles(@"Responses\SQL Server Load Customers DB.json", @"Responses\SQL GetRelationships SampleDB.json");
            await connectorTable.InitAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);
            Assert.True(connectorTable.IsInitialized);

            CdpTableValue sqlTable = connectorTable.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegable);
            Assert.Equal("*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s]", sqlTable.Type._type.ToString());

            HashSet<IExternalTabularDataSource> ads = sqlTable.Type._type.AssociatedDataSources;
            Assert.NotNull(ads);
            Assert.Single(ads);

            ExternalCdpDataSource tds = Assert.IsType<ExternalCdpDataSource>(ads.First());
            Assert.NotNull(tds);
            Assert.NotNull(tds.DataEntityMetadataProvider);

            CdpEntityMetadataProvider cemp = Assert.IsType<CdpEntityMetadataProvider>(tds.DataEntityMetadataProvider);
            Assert.True(cemp.TryGetEntityMetadata("Customers", out IDataEntityMetadata dem));

            CdpDataSourceMetadata tdsm = Assert.IsType<CdpDataSourceMetadata>(dem);
            Assert.Equal("pfxdev-sql.database.windows.net,connectortest", tdsm.DatasetName);
            Assert.Equal("Customers", tdsm.EntityName);

            Assert.Equal("Customers", tds.EntityName.Value);
            Assert.True(tds.IsDelegatable);
            Assert.True(tds.IsPageable);
            Assert.True(tds.IsRefreshable);
            Assert.True(tds.IsSelectable);
            Assert.True(tds.IsWritable);
            Assert.Equal(DataSourceKind.Connected, tds.Kind);
            Assert.Equal("Customers", tds.Name);
            Assert.True(tds.RequiresAsync);
            Assert.NotNull(tds.ServiceCapabilities);

            Assert.NotNull(sqlTable._connectorType);
            Assert.Null(sqlTable._connectorType.Relationships);

            SymbolValues symbolValues = new SymbolValues().Add("Customers", sqlTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Customers).Address";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue address = Assert.IsType<StringValue>(result);
            Assert.Equal("Juigné", address.Value);

            // Rows are not cached here as the cache is stored in CdpTableValue which is created by InjectServiceProviderFunction, itself added during Engine.Check
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            result = await engine.EvalAsync("Last(Customers).Phone", CancellationToken.None, runtimeConfig: rc);
            StringValue phone = Assert.IsType<StringValue>(result);
            Assert.Equal("+1-425-705-0000", phone.Value);
        }

        [Fact]
        public async Task SQL_CdpTabular_GetTables2()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "2cc03a388d38465fba53f05cd2c76181";
            string jwt = "eyJ0eXAiOiJKSuA...";
            using var client = new PowerPlatformConnectorClient("dac64a92-df6a-ee6e-a6a2-be41a923e371.15.common.tip1002.azure-apihub.net", "dac64a92-df6a-ee6e-a6a2-be41a923e371", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            string realTableName = "Product";
            string fxTableName = "Products";

            testConnector.SetResponseFromFile(@"Responses\SQL GetDatasetsMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(dm);
            Assert.Null(dm.Blob);

            CdpDataSource cds = new CdpDataSource("default,default");

            testConnector.SetResponseFromFiles(@"Responses\SQL GetDatasetsMetadata.json", @"Responses\SQL GetTables SampleDB.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(tables);
            Assert.Equal(17, tables.Count());
            Assert.Equal(
                "[dbo].[BuildVersion],[dbo].[ErrorLog],[dbo].[sysdiagrams],[SalesLT].[Address],[SalesLT].[Customer],[SalesLT].[CustomerAddress],[SalesLT].[Product],[SalesLT].[ProductCategory],[SalesLT].[ProductDescription]," +
                "[SalesLT].[ProductModel],[SalesLT].[ProductModelProductDescription],[SalesLT].[SalesOrderDetail],[SalesLT].[SalesOrderHeader],[SalesLT].[vGetAllCategories],[SalesLT].[vProductAndDescription]," +
                "[SalesLT].[vProductModelCatalogDescription],[sys].[database_firewall_rules]", string.Join(",", tables.Select(t => t.TableName)));
            Assert.Equal(
                "BuildVersion,ErrorLog,sysdiagrams,Address,Customer,CustomerAddress,Product,ProductCategory,ProductDescription,ProductModel,ProductModelProductDescription,SalesOrderDetail,SalesOrderHeader,vGetAllCategories,vProductAndDescription,vProductModelCatalogDescription,database_firewall_rules", string.Join(",", tables.Select(t => t.DisplayName)));

            CdpTable connectorTable = tables.First(t => t.DisplayName == realTableName);

            Assert.False(connectorTable.IsInitialized);
            Assert.Equal(realTableName, connectorTable.DisplayName);

            testConnector.SetResponseFromFile(@"Responses\SQL GetTables SampleDB.json");
            CdpTable table2 = await cds.GetTableAsync(client, $"/apim/sql/{connectionId}", realTableName, null /* logical or display name */, CancellationToken.None, logger);
            Assert.False(table2.IsInitialized);
            Assert.Equal(realTableName, table2.DisplayName);
            Assert.Equal("[SalesLT].[Product]", table2.TableName); // Logical Name

            testConnector.SetResponseFromFile(@"Responses\SQL GetTables SampleDB.json");
            table2 = await cds.GetTableAsync(client, $"/apim/sql/{connectionId}", realTableName, false /* display name only */, CancellationToken.None, logger);
            Assert.False(table2.IsInitialized);
            Assert.Equal(realTableName, table2.DisplayName);
            Assert.Equal("[SalesLT].[Product]", table2.TableName); // Logical Name

            testConnector.SetResponseFromFile(@"Responses\SQL GetTables SampleDB.json");
            table2 = await cds.GetTableAsync(client, $"/apim/sql/{connectionId}", "[SalesLT].[Product]", true /* logical name only */, CancellationToken.None, logger);
            Assert.False(table2.IsInitialized);
            Assert.Equal(realTableName, table2.DisplayName);
            Assert.Equal("[SalesLT].[Product]", table2.TableName); // Logical Name

            testConnector.SetResponseFromFile(@"Responses\SQL GetTables SampleDB.json");
            InvalidOperationException ioe = await Assert.ThrowsAsync<InvalidOperationException>(() => cds.GetTableAsync(client, $"/apim/sql/{connectionId}", "[SalesLT].[Product]", false /* display name only */, CancellationToken.None, logger));
            Assert.Equal("Cannot find any table with the specified name", ioe.Message);

            testConnector.SetResponseFromFiles(@"Responses\SQL GetSchema Products.json", @"Responses\SQL GetRelationships SampleDB.json");
            await connectorTable.InitAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);
            Assert.True(connectorTable.IsInitialized);

            CdpTableValue sqlTable = connectorTable.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegable);
            Assert.Equal("*[Color:s, DiscontinuedDate:d, ListPrice:w, ModifiedDate:d, Name:s, ProductCategoryID:w, ProductID:w, ProductModelID:w, ProductNumber:s, SellEndDate:d, SellStartDate:d, Size:s, StandardCost:w, ThumbNailPhoto:o, ThumbnailPhotoFileName:s, Weight:w, rowguid:s]", sqlTable.Type._type.ToString());

            HashSet<IExternalTabularDataSource> ads = sqlTable.Type._type.AssociatedDataSources;
            Assert.NotNull(ads);

            Assert.NotNull(sqlTable._connectorType);
            Assert.Null(sqlTable._connectorType.Relationships); // TO BE CHANGED, x-ms-relationships only for now

            SymbolValues symbolValues = new SymbolValues().Add(fxTableName, sqlTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @$"First({fxTableName}).Name";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SQL GetItems Products.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue address = Assert.IsType<StringValue>(result);
            Assert.Equal("HL Road Frame - Black, 58", address.Value);

            bool b = sqlTable.TabularRecordType.TryGetFieldExternalTableName("ProductModelID", out string externalTableName, out string foreignKey);
            Assert.True(b);
            Assert.Equal("ProductModel", externalTableName); // Display Name
            Assert.Equal("ProductModelID", foreignKey);

            testConnector.SetResponseFromFiles(@"Responses\SQL GetSchema ProductModel.json", @"Responses\SQL GetRelationships SampleDB.json");
            b = sqlTable.TabularRecordType.TryGetFieldType("ProductModelID", out FormulaType productModelID);

            Assert.True(b);
            CdpRecordType productModelRecordType = Assert.IsType<CdpRecordType>(productModelID);

            Assert.False(productModelRecordType is null);

            // External relationship table name
            Assert.Equal("[SalesLT].[ProductModel]", productModelRecordType.TableSymbolName);
            Assert.Equal("![CatalogDescription:s, ModifiedDate:d, Name:s, ProductModelID:w, rowguid:s]", productModelRecordType.ToStringWithDisplayNames()); // Logical Name
        }

        [Fact]
        public async Task SAP_CDP()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "66108f1684944d4994ea38b13d1ee70f";
            string jwt = "eyJ0eXAi...";
            using var client = new PowerPlatformConnectorClient("f5de196a-41e6-ee09-92cf-664b4f31a6b2.06.common.tip1002.azure-apihub.net", "f5de196a-41e6-ee09-92cf-664b4f31a6b2", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            testConnector.SetResponseFromFile(@"Responses\SAP GetDataSetMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, $"/apim/sapodata/{connectionId}", CancellationToken.None, logger);

            CdpDataSource cds = new CdpDataSource("http://sapecckerb.roomsofthehouse.com:8080/sap/opu/odata/sap/HRESS_TEAM_CALENDAR_SERVICE");
            testConnector.SetResponseFromFiles(@"Responses\SAP GetDataSetMetadata.json", @"Responses\SAP GetTables.json");
            CdpTable sapTable = await cds.GetTableAsync(client, $"/apim/sapodata/{connectionId}", "TeamCalendarCollection", null, CancellationToken.None, logger);

            testConnector.SetResponseFromFile(@"Responses\SAP GetTable Schema.json");
            await sapTable.InitAsync(client, $"/apim/sapodata/{connectionId}", CancellationToken.None, logger);

            Assert.True(sapTable.IsInitialized);

            CdpTableValue sapTableValue = sapTable.GetTableValue();
            Assert.Equal<object>("*[ALL_EMPLOYEES:s, APP_MODE:s, BEGIN_DATE:s, BEGIN_DATE_CHAR:s, COMMAND:s, DESCRIPTION:s, EMP_PERNR:s, END_DATE:s, END_DATE_CHAR:s, EVENT_NAME:s, FLAG:s, GetMessages:*[MESSAGE:s, PERNR:s], HIDE_PEERS:s, LEGEND:s, LEGENDID:s, LEGEND_TEXT:s, PERNR:s, PERNR_MEM_ID:s, TYPE:s]", sapTableValue.Type._type.ToString());
        }

        [Fact]
        public async Task SQL_CdpTabular()
        {
            using var testConnector = new LoggingTestServer(null, _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            using var httpClient = new HttpClient(testConnector);
            string connectionId = "18992e9477684930acd2cc5dc9bb94c2";
            string jwt = "eyJ0eXAiOiJK...";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", connectionId, () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            // Use of tabular connector
            // There is a network call here to retrieve the table's schema
            testConnector.SetResponseFromFile(@"Responses\SQL Server Load Customers DB.json");

            ConsoleLogger logger = new ConsoleLogger(_output);
            CdpTable tabularService = new CdpTable("pfxdev-sql.database.windows.net,connectortest", "Customers", tables: null);

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Customers", tabularService.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SQL GetDatasetsMetadata.json", @"Responses\SQL Server Load Customers DB.json", @"Responses\SQL GetRelationships SampleDB.json");
            await tabularService.InitAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);
            Assert.True(tabularService.IsInitialized);

            CdpTableValue sqlTable = tabularService.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegable);
            Assert.Equal("*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s]", sqlTable.Type._type.ToString());

            SymbolValues symbolValues = new SymbolValues().Add("Customers", sqlTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Customers).Address";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue address = Assert.IsType<StringValue>(result);
            Assert.Equal("Juigné", address.Value);

            // Rows are not cached here as the cache is stored in CdpTableValue which is created by InjectServiceProviderFunction, itself added during Engine.Check
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            result = await engine.EvalAsync("Last(Customers).Phone", CancellationToken.None, runtimeConfig: rc);
            StringValue phone = Assert.IsType<StringValue>(result);
            Assert.Equal("+1-425-705-0000", phone.Value);
        }

        [Fact]
        public async Task SP_CdpTabular_GetTables()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            using var httpClient = new HttpClient(testConnector);
            string connectionId = "3738993883dc406d86802d8a6a923d3e";
            string jwt = "eyJ0eXAiOiJK...";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            ConsoleLogger logger = new ConsoleLogger(_output);
            CdpDataSource cds = new CdpDataSource("https://microsofteur.sharepoint.com/teams/pfxtest");

            testConnector.SetResponseFromFiles(@"Responses\SP GetDatasetsMetadata.json", @"Responses\SP GetTables.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $"/apim/sharepointonline/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(cds.DatasetMetadata);

            Assert.NotNull(cds.DatasetMetadata.Blob);
            Assert.Equal("mru", cds.DatasetMetadata.Blob.Source);
            Assert.Equal("site", cds.DatasetMetadata.Blob.DisplayName);
            Assert.Equal("double", cds.DatasetMetadata.Blob.UrlEncoding);

            Assert.Null(cds.DatasetMetadata.DatasetFormat);
            Assert.Null(cds.DatasetMetadata.Parameters);

            Assert.NotNull(cds.DatasetMetadata.Tabular);
            Assert.Equal("site", cds.DatasetMetadata.Tabular.DisplayName);
            Assert.Equal("mru", cds.DatasetMetadata.Tabular.Source);
            Assert.Equal("list", cds.DatasetMetadata.Tabular.TableDisplayName);
            Assert.Equal("lists", cds.DatasetMetadata.Tabular.TablePluralName);
            Assert.Equal("double", cds.DatasetMetadata.Tabular.UrlEncoding);

            Assert.NotNull(tables);
            Assert.Equal(2, tables.Count());
            Assert.Equal("4bd37916-0026-4726-94e8-5a0cbc8e476a,5266fcd9-45ef-4b8f-8014-5d5c397db6f0", string.Join(",", tables.Select(t => t.TableName)));
            Assert.Equal("Documents,MikeTestList", string.Join(",", tables.Select(t => t.DisplayName)));

            CdpTable connectorTable = tables.First(t => t.DisplayName == "Documents");

            Assert.False(connectorTable.IsInitialized);
            Assert.Equal("4bd37916-0026-4726-94e8-5a0cbc8e476a", connectorTable.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SP GetTable.json");
            await connectorTable.InitAsync(client, $"/apim/sharepointonline/{connectionId}", CancellationToken.None, logger);
            Assert.True(connectorTable.IsInitialized);

            CdpTableValue spTable = connectorTable.GetTableValue();
            Assert.True(spTable._tabularService.IsInitialized);
            Assert.True(spTable.IsDelegable);

            Assert.Equal(
                "*[Author`'Created By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser`'Checked Out To':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ComplianceAssetId`'Compliance " +
                "Asset Id':s, Created:d, Editor`'Modified By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag`'Color Tag':s, OData__DisplayName`Sensitivity:s, " +
                "OData__ExtendedDescription`Description:s, OData__ip_UnifiedCompliancePolicyProperties`'Unified Compliance Policy Properties':s, Title:s, '{FilenameWithExtension}'`'File name with extension':s, '{FullPath}'`'Full " +
                "Path':s, '{Identifier}'`Identifier:s, '{IsCheckedOut}'`'Checked out':b, '{IsFolder}'`IsFolder:b, '{Link}'`'Link to item':s, '{ModerationComment}'`'Comments associated with the content approval of this " +
                "list item':s, '{ModerationStatus}'`'Content approval status':s, '{Name}'`Name:s, '{Path}'`'Folder path':s, '{Thumbnail}'`Thumbnail:![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}'`'Trigger Window " +
                "End Token':s, '{TriggerWindowStartToken}'`'Trigger Window Start Token':s, '{VersionNumber}'`'Version number':s]", spTable.Type.ToStringWithDisplayNames());

            HashSet<IExternalTabularDataSource> ads = spTable.Type._type.AssociatedDataSources;
            Assert.NotNull(ads);

            // Tests skipped as ConnectorType.AddDataSource is skipping the creation of AssociatedDataSources
#if false
            Assert.Single(ads);

            TabularDataSource tds = Assert.IsType<TabularDataSource>(ads.First());
            Assert.NotNull(tds);
            Assert.NotNull(tds.DataEntityMetadataProvider);

            CdpEntityMetadataProvider cemp = Assert.IsType<CdpEntityMetadataProvider>(tds.DataEntityMetadataProvider);
            Assert.True(cemp.TryGetEntityMetadata("Documents", out IDataEntityMetadata dem));

            TabularDataSourceMetadata tdsm = Assert.IsType<TabularDataSourceMetadata>(dem);
            Assert.Equal("https://microsofteur.sharepoint.com/teams/pfxtest", tdsm.DatasetName);
            Assert.Equal("Documents", tdsm.EntityName);

            Assert.Equal("Documents", tds.EntityName.Value);
            Assert.True(tds.IsDelegatable);
            Assert.True(tds.IsPageable);
            Assert.True(tds.IsRefreshable);
            Assert.False(tds.IsSelectable);
            Assert.True(tds.IsWritable);
            Assert.Equal(DataSourceKind.Connected, tds.Kind);
            Assert.Equal("Documents", tds.Name);
            Assert.True(tds.RequiresAsync);
            Assert.NotNull(tds.ServiceCapabilities);
#endif

            Assert.NotNull(spTable._connectorType);
            Assert.NotNull(spTable._connectorType.Relationships);
            Assert.Equal(3, spTable._connectorType.Relationships.Count);
            Assert.Equal("Editor, Author, CheckoutUser", string.Join(", ", spTable._connectorType.Relationships.Select(kvp => kvp.Key)));
            Assert.Equal("Editor, Author, CheckoutUser", string.Join(", ", spTable._connectorType.Relationships.Select(kvp => kvp.Value.TargetEntity)));
            Assert.Equal("Editor#Claims-Claims, Author#Claims-Claims, CheckoutUser#Claims-Claims", string.Join(", ", spTable._connectorType.Relationships.Select(kvp => string.Join("|", kvp.Value.ReferentialConstraints.Select(kvp2 => $"{kvp2.Key}-{kvp2.Value}")))));

            SymbolValues symbolValues = new SymbolValues().Add("Documents", spTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Documents).Name";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SP GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue docName = Assert.IsType<StringValue>(result);
            Assert.Equal("Document1", docName.Value);
        }

        [Fact]
        public async Task SP_CdpTabular()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "0b905132239e463a9d12f816be201da9";
            string jwt = "eyJ0eXAiOiJKV....";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", connectionId, () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832384"
            };

            CdpTable tabularService = new CdpTable("https://microsofteur.sharepoint.com/teams/pfxtest", "Documents", tables: null);

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Documents", tabularService.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SP GetDatasetsMetadata.json", @"Responses\SP GetTable.json");
            await tabularService.InitAsync(client, $"/apim/sharepointonline/{connectionId}", CancellationToken.None, logger);
            Assert.True(tabularService.IsInitialized);

            CdpTableValue spTable = tabularService.GetTableValue();
            Assert.True(spTable._tabularService.IsInitialized);
            Assert.True(spTable.IsDelegable);

            Assert.Equal(
                "*[Author`'Created By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser`'Checked Out To':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ComplianceAssetId`'Compliance " +
                "Asset Id':s, Created:d, Editor`'Modified By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag`'Color Tag':s, OData__DisplayName`Sensitivity:s, " +
                "OData__ExtendedDescription`Description:s, OData__ip_UnifiedCompliancePolicyProperties`'Unified Compliance Policy Properties':s, Title:s, '{FilenameWithExtension}'`'File name with extension':s, '{FullPath}'`'Full " +
                "Path':s, '{Identifier}'`Identifier:s, '{IsCheckedOut}'`'Checked out':b, '{IsFolder}'`IsFolder:b, '{Link}'`'Link to item':s, '{ModerationComment}'`'Comments associated with the content approval of this " +
                "list item':s, '{ModerationStatus}'`'Content approval status':s, '{Name}'`Name:s, '{Path}'`'Folder path':s, '{Thumbnail}'`Thumbnail:![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}'`'Trigger Window " +
                "End Token':s, '{TriggerWindowStartToken}'`'Trigger Window Start Token':s, '{VersionNumber}'`'Version number':s]", spTable.Type.ToStringWithDisplayNames());

            SymbolValues symbolValues = new SymbolValues().Add("Documents", spTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Documents).Name";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SP GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue docName = Assert.IsType<StringValue>(result);
            Assert.Equal("Document1", docName.Value);
        }

        [Fact]
        public async Task SF_CountRows()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "ba3b1db7bb854aedbad2058b66e36e83";
            string jwt = "eyJ0eXAiOiJK...";
            using var client = new PowerPlatformConnectorClient("tip1002-002.azure-apihub.net", "7526ddf1-6e97-eed6-86bb-8fd46790d670", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            CdpDataSource cds = new CdpDataSource("default");

            testConnector.SetResponseFromFiles(@"Responses\SF GetDatasetsMetadata.json", @"Responses\SF GetTables.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger);
            CdpTable connectorTable = tables.First(t => t.DisplayName == "Accounts");

            testConnector.SetResponseFromFile(@"Responses\SF GetSchema.json");
            await connectorTable.InitAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger);
            CdpTableValue sfTable = connectorTable.GetTableValue();

            SymbolValues symbolValues = new SymbolValues().Add("Accounts", sfTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"CountRows(Accounts)";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            testConnector.SetResponseFromFile(@"Responses\SF GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);
            Assert.Equal(6, ((DecimalValue)result).Value);
        }

        [Fact]
        public async Task SF_Filter()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "ba3b1db7bb854aedbad2058b66e36e83";
            string jwt = "eyJ0eXAiOi...";
            using var client = new PowerPlatformConnectorClient("tip1002-002.azure-apihub.net", "7526ddf1-6e97-eed6-86bb-8fd46790d670", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            CdpDataSource cds = new CdpDataSource("default");

            testConnector.SetResponseFromFiles(@"Responses\SF GetDatasetsMetadata.json", @"Responses\SF GetTables.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $" / apim/salesforce/{connectionId}", CancellationToken.None, logger);
            CdpTable connectorTable = tables.First(t => t.DisplayName == "Accounts");

            testConnector.SetResponseFromFile(@"Responses\SF GetSchema.json");
            await connectorTable.InitAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger);
            CdpTableValue sfTable = connectorTable.GetTableValue();

            SymbolValues symbolValues = new SymbolValues().Add("Accounts", sfTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Filter(Accounts, 'Account ID' = ""001DR00001Xlq74YAB"")).'Account Name'";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            testConnector.SetResponseFromFile(@"Responses\SF GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);
            Assert.Equal("Kutch and Sons", ((StringValue)result).Value);
        }

        [Fact]
        public async Task SF_CdpTabular_GetTables()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "3b997639fd9c4d808ecf723eb4b55c64";
            string jwt = "eyJ0eXAiOiJKV...";
            using var client = new PowerPlatformConnectorClient("tip1-shared.azure-apim.net", "e48a52f5-3dfe-e2f6-bc0b-155d32baa44c", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            testConnector.SetResponseFromFile(@"Responses\SF GetDatasetsMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(dm);
            Assert.Null(dm.Blob);
            Assert.Null(dm.DatasetFormat);
            Assert.Null(dm.Parameters);

            Assert.NotNull(dm.Tabular);
            Assert.Equal("dataset", dm.Tabular.DisplayName);
            Assert.Equal("singleton", dm.Tabular.Source);
            Assert.Equal("Table", dm.Tabular.TableDisplayName);
            Assert.Equal("Tables", dm.Tabular.TablePluralName);
            Assert.Equal("double", dm.Tabular.UrlEncoding);

            CdpDataSource cds = new CdpDataSource("default");

            // only one network call as we already read metadata
            testConnector.SetResponseFromFiles(@"Responses\SF GetDatasetsMetadata.json", @"Responses\SF GetTables.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger);

            Assert.NotNull(tables);
            Assert.Equal(569, tables.Count());

            CdpTable connectorTable = tables.First(t => t.DisplayName == "Accounts");
            Assert.Equal("Account", connectorTable.TableName);
            Assert.False(connectorTable.IsInitialized);

            testConnector.SetResponseFromFile(@"Responses\SF GetSchema.json");
            await connectorTable.InitAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger);
            Assert.True(connectorTable.IsInitialized);

            CdpTableValue sfTable = connectorTable.GetTableValue();
            Assert.True(sfTable._tabularService.IsInitialized);
            Assert.True(sfTable.IsDelegable);

            // Note relationships with external tables (logicalName`displayName[externalTable]:type)
            //   CreatedById`'Created By ID'[User]:s
            //   LastModifiedById`'Last Modified By ID'[User]:s
            //   Modified By ID'[User]:s
            //   MasterRecordId`'Master Record ID'[Account]:s
            //   OwnerId`'Owner ID'[User]:s
            //   ParentId`'Parent Account ID'[Account]:s
            Assert.Equal(
                "![AccountSource`'Account Source':l, BillingCity`'Billing City':s, BillingCountry`'Billing Country':s, BillingGeocodeAccuracy`'Billing Geocode Accuracy':l, BillingLatitude`'Billing Latitude':w, BillingLongitude`'Billing " +
                "Longitude':w, BillingPostalCode`'Billing Zip/Postal Code':s, BillingState`'Billing State/Province':s, BillingStreet`'Billing Street':s, CreatedById`'Created By ID'[User]:s, CreatedDate`'Created Date':d, " +
                "Description`'Account Description':s, Id`'Account ID':s, Industry:l, IsDeleted`Deleted:b, Jigsaw`'Data.com Key':s, JigsawCompanyId`'Jigsaw Company ID':s, LastActivityDate`'Last Activity':D, LastModifiedById`'Last " +
                "Modified By ID'[User]:s, LastModifiedDate`'Last Modified Date':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last Viewed Date':d, MasterRecordId`'Master Record ID'[Account]:s, Name`'Account " +
                "Name':s, NumberOfEmployees`Employees:w, OwnerId`'Owner ID'[User]:s, ParentId`'Parent Account ID'[Account]:s, Phone`'Account Phone':s, PhotoUrl`'Photo URL':s, ShippingCity`'Shipping City':s, ShippingCountry`'Shipping " +
                "Country':s, ShippingGeocodeAccuracy`'Shipping Geocode Accuracy':l, ShippingLatitude`'Shipping Latitude':w, ShippingLongitude`'Shipping Longitude':w, ShippingPostalCode`'Shipping Zip/Postal Code':s, ShippingState`'Shipping " +
                "State/Province':s, ShippingStreet`'Shipping Street':s, SicDesc`'SIC Description':s, SystemModstamp`'System Modstamp':d, Type`'Account Type':l, Website:s]", ((CdpRecordType)sfTable.TabularRecordType).ToStringWithDisplayNames());

            Assert.Equal("Account", sfTable.TabularRecordType.TableSymbolName);

            RecordType rt = sfTable.TabularRecordType;
            NamedFormulaType nft = rt.GetFieldTypes().First();

            Assert.Equal("AccountSource", nft.Name);
            Assert.Equal("Account Source", nft.DisplayName);

            HashSet<IExternalTabularDataSource> ads = sfTable.Type._type.AssociatedDataSources;
            Assert.NotNull(ads);

            // Tests skipped as ConnectorType.AddDataSource is skipping the creation of AssociatedDataSources
#if false
            Assert.Single(ads);

            TabularDataSource tds = Assert.IsType<TabularDataSource>(ads.First());
            Assert.NotNull(tds);
            Assert.NotNull(tds.DataEntityMetadataProvider);

            CdpEntityMetadataProvider cemp = Assert.IsType<CdpEntityMetadataProvider>(tds.DataEntityMetadataProvider);
            Assert.True(cemp.TryGetEntityMetadata("Account", out IDataEntityMetadata dem));

            TabularDataSourceMetadata tdsm = Assert.IsType<TabularDataSourceMetadata>(dem);
            Assert.Equal("default", tdsm.DatasetName);
            Assert.Equal("Account", tdsm.EntityName);

            Assert.Equal("Account", tds.EntityName.Value);
            Assert.True(tds.IsDelegatable);
            Assert.True(tds.IsPageable);
            Assert.True(tds.IsRefreshable);
            Assert.True(tds.IsSelectable);
            Assert.True(tds.IsWritable);
            Assert.Equal(DataSourceKind.Connected, tds.Kind);
            Assert.Equal("Account", tds.Name);
            Assert.True(tds.RequiresAsync);
            Assert.NotNull(tds.ServiceCapabilities);
#endif

            Assert.NotNull(sfTable._connectorType);

            // SF doesn't use x-ms-releationships extension
            Assert.Null(sfTable._connectorType.Relationships);

            // needs Microsoft.PowerFx.Connectors.CdpExtensions
            // this call does not make any network call
            bool b = sfTable.TabularRecordType.TryGetFieldExternalTableName("OwnerId", out string externalTableName, out string foreignKey);
            Assert.True(b);
            Assert.Equal("User", externalTableName);
            Assert.Null(foreignKey); // Always the case with SalesForce

            testConnector.SetResponseFromFile(@"Responses\SF GetSchema Users.json");
            b = sfTable.TabularRecordType.TryGetFieldType("OwnerId", out FormulaType ownerIdType);

            Assert.True(b);
            CdpRecordType userTable = Assert.IsType<CdpRecordType>(ownerIdType);

            Assert.False((CdpRecordType)sfTable.TabularRecordType is null);
            Assert.False(userTable is null);

            // External relationship table name
            Assert.Equal("User", userTable.TableSymbolName);

            Assert.Equal(
                "![AboutMe`'About Me':s, AccountId`'Account ID'[Account]:s, Alias:s, BadgeText`'User Photo badge text overlay':s, BannerPhotoUrl`'Url for banner photo':s, CallCenterId`'Call Center ID':s, City:s, CommunityNickname`Nickname:s, " +
                "CompanyName`'Company Name':s, ContactId`'Contact ID'[Contact]:s, Country:s, CreatedById`'Created By ID'[User]:s, CreatedDate`'Created Date':d, DefaultGroupNotificationFrequency`'Default Notification Frequency " +
                "when Joining Groups':l, DelegatedApproverId`'Delegated Approver ID':s, Department:s, DigestFrequency`'Chatter Email Highlights Frequency':l, Division:s, Email:s, EmailEncodingKey`'Email Encoding':l, EmailPreferencesAutoBcc`AutoBcc:b, " +
                "EmailPreferencesAutoBccStayInTouch`AutoBccStayInTouch:b, EmailPreferencesStayInTouchReminder`StayInTouchReminder:b, EmployeeNumber`'Employee Number':s, Extension:s, Fax:s, FederationIdentifier`'SAML Federation " +
                "ID':s, FirstName`'First Name':s, ForecastEnabled`'Allow Forecasting':b, FullPhotoUrl`'Url for full-sized Photo':s, GeocodeAccuracy`'Geocode Accuracy':l, Id`'User ID':s, IsActive`Active:b, IsExtIndicatorVisible`'Show " +
                "external indicator':b, IsProfilePhotoActive`'Has Profile Photo':b, LanguageLocaleKey`Language:l, LastLoginDate`'Last Login':d, LastModifiedById`'Last Modified By ID'[User]:s, LastModifiedDate`'Last Modified " +
                "Date':d, LastName`'Last Name':s, LastPasswordChangeDate`'Last Password Change or Reset':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last Viewed Date':d, Latitude:w, LocaleSidKey`Locale:l, " +
                "Longitude:w, ManagerId`'Manager ID'[User]:s, MediumBannerPhotoUrl`'Url for Android banner photo':s, MediumPhotoUrl`'Url for medium profile photo':s, MiddleName`'Middle Name':s, MobilePhone`Mobile:s, Name`'Full " +
                "Name':s, OfflinePdaTrialExpirationDate`'Sales Anywhere Trial Expiration Date':d, OfflineTrialExpirationDate`'Offline Edition Trial Expiration Date':d, OutOfOfficeMessage`'Out of office message':s, Phone:s, " +
                "PostalCode`'Zip/Postal Code':s, ProfileId`'Profile ID'[Profile]:s, ReceivesAdminInfoEmails`'Admin Info Emails':b, ReceivesInfoEmails`'Info Emails':b, SenderEmail`'Email Sender Address':s, SenderName`'Email " +
                "Sender Name':s, Signature`'Email Signature':s, SmallBannerPhotoUrl`'Url for IOS banner photo':s, SmallPhotoUrl`Photo:s, State`'State/Province':s, StayInTouchNote`'Stay-in-Touch Email Note':s, StayInTouchSignature`'Stay-in-Touch " +
                "Email Signature':s, StayInTouchSubject`'Stay-in-Touch Email Subject':s, Street:s, Suffix:s, SystemModstamp`'System Modstamp':d, TimeZoneSidKey`'Time Zone':l, Title:s, UserPermissionsAvantgoUser`'AvantGo " +
                "User':b, UserPermissionsCallCenterAutoLogin`'Auto-login To Call Center':b, UserPermissionsInteractionUser`'Flow User':b, UserPermissionsKnowledgeUser`'Knowledge User':b, UserPermissionsLiveAgentUser`'Chat " +
                "User':b, UserPermissionsMarketingUser`'Marketing User':b, UserPermissionsMobileUser`'Apex Mobile User':b, UserPermissionsOfflineUser`'Offline User':b, UserPermissionsSFContentUser`'Salesforce CRM Content " +
                "User':b, UserPermissionsSupportUser`'Service Cloud User':b, UserPreferencesActivityRemindersPopup`ActivityRemindersPopup:b, UserPreferencesApexPagesDeveloperMode`ApexPagesDeveloperMode:b, UserPreferencesCacheDiagnostics`CacheDiagnostics:b, " +
                "UserPreferencesCreateLEXAppsWTShown`CreateLEXAppsWTShown:b, UserPreferencesDisCommentAfterLikeEmail`DisCommentAfterLikeEmail:b, UserPreferencesDisMentionsCommentEmail`DisMentionsCommentEmail:b, UserPreferencesDisProfPostCommentEmail`DisProfP" +
                "ostCommentEmail:b, UserPreferencesDisableAllFeedsEmail`DisableAllFeedsEmail:b, UserPreferencesDisableBookmarkEmail`DisableBookmarkEmail:b, UserPreferencesDisableChangeCommentEmail`DisableChangeCommentEmail:b, " +
                "UserPreferencesDisableEndorsementEmail`DisableEndorsementEmail:b, UserPreferencesDisableFileShareNotificationsForApi`DisableFileShareNotificationsForApi:b, UserPreferencesDisableFollowersEmail`DisableFollowersEmail:b, " +
                "UserPreferencesDisableLaterCommentEmail`DisableLaterCommentEmail:b, UserPreferencesDisableLikeEmail`DisableLikeEmail:b, UserPreferencesDisableMentionsPostEmail`DisableMentionsPostEmail:b, UserPreferencesDisableMessageEmail`DisableMessageEmai" +
                "l:b, UserPreferencesDisableProfilePostEmail`DisableProfilePostEmail:b, UserPreferencesDisableSharePostEmail`DisableSharePostEmail:b, UserPreferencesEnableAutoSubForFeeds`EnableAutoSubForFeeds:b, UserPreferencesEventRemindersCheckboxDefault`E" +
                "ventRemindersCheckboxDefault:b, UserPreferencesExcludeMailAppAttachments`ExcludeMailAppAttachments:b, UserPreferencesFavoritesShowTopFavorites`FavoritesShowTopFavorites:b, UserPreferencesFavoritesWTShown`FavoritesWTShown:b, " +
                "UserPreferencesGlobalNavBarWTShown`GlobalNavBarWTShown:b, UserPreferencesGlobalNavGridMenuWTShown`GlobalNavGridMenuWTShown:b, UserPreferencesHideBiggerPhotoCallout`HideBiggerPhotoCallout:b, UserPreferencesHideCSNDesktopTask`HideCSNDesktopTas" +
                "k:b, UserPreferencesHideCSNGetChatterMobileTask`HideCSNGetChatterMobileTask:b, UserPreferencesHideChatterOnboardingSplash`HideChatterOnboardingSplash:b, UserPreferencesHideEndUserOnboardingAssistantModal`HideEndUserOnboardingAssistantModal:b" +
                ", UserPreferencesHideLightningMigrationModal`HideLightningMigrationModal:b, UserPreferencesHideS1BrowserUI`HideS1BrowserUI:b, UserPreferencesHideSecondChatterOnboardingSplash`HideSecondChatterOnboardingSplash:b, " +
                "UserPreferencesHideSfxWelcomeMat`HideSfxWelcomeMat:b, UserPreferencesLightningExperiencePreferred`LightningExperiencePreferred:b, UserPreferencesPathAssistantCollapsed`PathAssistantCollapsed:b, UserPreferencesPreviewLightning`PreviewLightnin" +
                "g:b, UserPreferencesRecordHomeReservedWTShown`RecordHomeReservedWTShown:b, UserPreferencesRecordHomeSectionCollapseWTShown`RecordHomeSectionCollapseWTShown:b, UserPreferencesReminderSoundOff`ReminderSoundOff:b, " +
                "UserPreferencesShowCityToExternalUsers`ShowCityToExternalUsers:b, UserPreferencesShowCityToGuestUsers`ShowCityToGuestUsers:b, UserPreferencesShowCountryToExternalUsers`ShowCountryToExternalUsers:b, UserPreferencesShowCountryToGuestUsers`Show" +
                "CountryToGuestUsers:b, UserPreferencesShowEmailToExternalUsers`ShowEmailToExternalUsers:b, UserPreferencesShowEmailToGuestUsers`ShowEmailToGuestUsers:b, UserPreferencesShowFaxToExternalUsers`ShowFaxToExternalUsers:b, " +
                "UserPreferencesShowFaxToGuestUsers`ShowFaxToGuestUsers:b, UserPreferencesShowManagerToExternalUsers`ShowManagerToExternalUsers:b, UserPreferencesShowManagerToGuestUsers`ShowManagerToGuestUsers:b, UserPreferencesShowMobilePhoneToExternalUsers" +
                "`ShowMobilePhoneToExternalUsers:b, UserPreferencesShowMobilePhoneToGuestUsers`ShowMobilePhoneToGuestUsers:b, UserPreferencesShowPostalCodeToExternalUsers`ShowPostalCodeToExternalUsers:b, UserPreferencesShowPostalCodeToGuestUsers`ShowPostalCo" +
                "deToGuestUsers:b, UserPreferencesShowProfilePicToGuestUsers`ShowProfilePicToGuestUsers:b, UserPreferencesShowStateToExternalUsers`ShowStateToExternalUsers:b, UserPreferencesShowStateToGuestUsers`ShowStateToGuestUsers:b, " +
                "UserPreferencesShowStreetAddressToExternalUsers`ShowStreetAddressToExternalUsers:b, UserPreferencesShowStreetAddressToGuestUsers`ShowStreetAddressToGuestUsers:b, UserPreferencesShowTitleToExternalUsers`ShowTitleToExternalUsers:b, " +
                "UserPreferencesShowTitleToGuestUsers`ShowTitleToGuestUsers:b, UserPreferencesShowWorkPhoneToExternalUsers`ShowWorkPhoneToExternalUsers:b, UserPreferencesShowWorkPhoneToGuestUsers`ShowWorkPhoneToGuestUsers:b, " +
                "UserPreferencesSortFeedByComment`SortFeedByComment:b, UserPreferencesTaskRemindersCheckboxDefault`TaskRemindersCheckboxDefault:b, UserRoleId`'Role ID'[UserRole]:s, UserType`'User Type':l, Username:s]", userTable.ToStringWithDisplayNames());

            // Missing field
            b = sfTable.TabularRecordType.TryGetFieldType("XYZ", out FormulaType xyzType);
            Assert.False(b);
            Assert.Null(xyzType);

            // Field with no relationship
            b = sfTable.TabularRecordType.TryGetFieldType("BillingCountry", out FormulaType billingCountryType);
            Assert.True(b);
            Assert.Equal("s", billingCountryType._type.ToString());

            SymbolValues symbolValues = new SymbolValues().Add("Accounts", sfTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Accounts).'Account ID'";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SF GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue accountId = Assert.IsType<StringValue>(result);
            Assert.Equal("001DR00001Xj1YmYAJ", accountId.Value);
        }

        [Fact]
        public async Task SF_CdpTabular()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "ec5fe6d1cad744a0a716fe4597a74b2e";
            string jwt = "eyJ0eXAiOiJ...";
            using var client = new PowerPlatformConnectorClient("tip2-001.azure-apihub.net", "53d7f409-4bce-e458-8245-5fa1346ec433", connectionId, () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832384"
            };

            CdpTable tabularService = new CdpTable("default", "Account", tables: null);

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Account", tabularService.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SF GetDatasetsMetadata.json", @"Responses\SF GetSchema.json");
            await tabularService.InitAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger);
            Assert.True(tabularService.IsInitialized);

            CdpTableValue sfTable = tabularService.GetTableValue();
            Assert.True(sfTable._tabularService.IsInitialized);
            Assert.True(sfTable.IsDelegable);

            Assert.Equal(
                "*[AccountSource`'Account Source':l, BillingCity`'Billing City':s, BillingCountry`'Billing Country':s, BillingGeocodeAccuracy`'Billing Geocode Accuracy':l, BillingLatitude`'Billing Latitude':w, BillingLongitude`'Billing " +
                "Longitude':w, BillingPostalCode`'Billing Zip/Postal Code':s, BillingState`'Billing State/Province':s, BillingStreet`'Billing Street':s, CreatedById`'Created By ID':s, CreatedDate`'Created Date':d, Description`'Account " +
                "Description':s, Id`'Account ID':s, Industry:l, IsDeleted`Deleted:b, Jigsaw`'Data.com Key':s, JigsawCompanyId`'Jigsaw Company ID':s, LastActivityDate`'Last Activity':D, LastModifiedById`'Last Modified By " +
                "ID':s, LastModifiedDate`'Last Modified Date':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last Viewed Date':d, MasterRecordId`'Master Record ID':s, Name`'Account Name':s, NumberOfEmployees`Employees:w, " +
                "OwnerId`'Owner ID':s, ParentId`'Parent Account ID':s, Phone`'Account Phone':s, PhotoUrl`'Photo URL':s, ShippingCity`'Shipping City':s, ShippingCountry`'Shipping Country':s, ShippingGeocodeAccuracy`'Shipping " +
                "Geocode Accuracy':l, ShippingLatitude`'Shipping Latitude':w, ShippingLongitude`'Shipping Longitude':w, ShippingPostalCode`'Shipping Zip/Postal Code':s, ShippingState`'Shipping State/Province':s, ShippingStreet`'Shipping " +
                "Street':s, SicDesc`'SIC Description':s, SystemModstamp`'System Modstamp':d, Type`'Account Type':l, Website:s]", sfTable.Type.ToStringWithDisplayNames());

            SymbolValues symbolValues = new SymbolValues().Add("Accounts", sfTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Accounts).'Account ID'";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SF GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue accountId = Assert.IsType<StringValue>(result);
            Assert.Equal("001DR00001Xj1YmYAJ", accountId.Value);
        }

        [Fact]
        public async Task ZD_CdpTabular_GetTables()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "7a82a84f1b454132920a2654b00d45be";
            string uriPrefix = $"/apim/zendesk/{connectionId}";
            string jwt = "eyJ0eXAiOiJK...";
            using var client = new PowerPlatformConnectorClient("tip1-shared.azure-apim.net", "e48a52f5-3dfe-e2f6-bc0b-155d32baa44c", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            testConnector.SetResponseFromFile(@"Responses\ZD GetDatasetsMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, uriPrefix, CancellationToken.None, logger);

            Assert.NotNull(dm);
            Assert.Null(dm.Blob);
            Assert.Null(dm.DatasetFormat);
            Assert.Null(dm.Parameters);

            Assert.NotNull(dm.Tabular);
            Assert.Equal("dataset", dm.Tabular.DisplayName);
            Assert.Equal("singleton", dm.Tabular.Source);
            Assert.Equal("table", dm.Tabular.TableDisplayName);
            Assert.Equal("tables", dm.Tabular.TablePluralName);
            Assert.Equal("double", dm.Tabular.UrlEncoding);

            CdpDataSource cds = new CdpDataSource("default");

            // only one network call as we already read metadata
            testConnector.SetResponseFromFiles(@"Responses\ZD GetDatasetsMetadata.json", @"Responses\ZD GetTables.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, uriPrefix, CancellationToken.None, logger);

            Assert.NotNull(tables);
            Assert.Equal(18, tables.Count());

            CdpTable connectorTable = tables.First(t => t.DisplayName == "Users");
            Assert.Equal("users", connectorTable.TableName);
            Assert.False(connectorTable.IsInitialized);

            testConnector.SetResponseFromFile(@"Responses\ZD Users GetSchema.json");
            await connectorTable.InitAsync(client, uriPrefix, CancellationToken.None, logger);
            Assert.True(connectorTable.IsInitialized);

            CdpTableValue zdTable = connectorTable.GetTableValue();
            Assert.True(zdTable._tabularService.IsInitialized);
            Assert.True(zdTable.IsDelegable);

            Assert.Equal(
                "![active:b, alias:s, created_at:d, custom_role_id:w, details:s, email:s, external_id:s, id:w, last_login_at:d, locale:s, locale_id:w, moderator:b, name:s, notes:s, only_private_comments:b, organization_id:w, " +
                "phone:s, photo:s, restricted_agent:b, role:s, shared:b, shared_agent:b, signature:s, suspended:b, tags:s, ticket_restriction:s, time_zone:s, updated_at:d, url:s, user_fields:s, verified:b]", ((CdpRecordType)zdTable.TabularRecordType).ToStringWithDisplayNames());

            SymbolValues symbolValues = new SymbolValues().Add("Users", zdTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Users).name";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\ZD Users GetRows.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue userName = Assert.IsType<StringValue>(result);
            Assert.Equal("Ram Sitwat", userName.Value);
        }
    }

    public static class Exts2
    {
        internal static string ToStringWithDisplayNames(this CdpRecordType trt)
        {
            string str = ((FormulaType)trt).ToStringWithDisplayNames();
            foreach (ConnectorType field in trt.ConnectorType.Fields.Where(ft => ft.ExternalTables != null && ft.ExternalTables.Any()))
            {
                string fn = field.Name;
                if (!string.IsNullOrEmpty(field.DisplayName))
                {
                    string dn = TexlLexer.EscapeName(field.DisplayName);
                    fn = $"{fn}`{dn}";
                }

                string fn2 = $"{fn}[{string.Join(",", field.ExternalTables)}]";
                str = str.Replace(fn, fn2);
            }

            return str;
        }
    }
}
