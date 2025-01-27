// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable SA1116

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
            Assert.Equal("r*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s]", sqlTable.Type.ToStringWithDisplayNames());

            HashSet<IExternalTabularDataSource> ads = sqlTable.Type._type.AssociatedDataSources;
            Assert.NotNull(ads);
            Assert.Single(ads);

            DataSourceInfo dataSourceInfo = Assert.IsType<DataSourceInfo>(ads.First());
            Assert.NotNull(dataSourceInfo);

            Assert.Equal("Customers", dataSourceInfo.EntityName.Value);
            Assert.True(dataSourceInfo.IsDelegatable);
            Assert.True(dataSourceInfo.IsPageable);
            Assert.True(dataSourceInfo.IsRefreshable);
            Assert.True(dataSourceInfo.IsSelectable);
            Assert.True(dataSourceInfo.IsWritable);

            Assert.Equal("Customers", dataSourceInfo.Name);
            Assert.True(dataSourceInfo.RequiresAsync);

            Assert.Null(sqlTable.Relationships);

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

            // Note relationships to ProductCategory and ProductModel with ~ notation
            Assert.Equal<object>("r*[Color:s, DiscontinuedDate:d, ListPrice:w, ModifiedDate:d, Name:s, ProductCategoryID:w, ProductID:w, ProductModelID:w, ProductNumber:s, SellEndDate:d, SellStartDate:d, Size:s, StandardCost:w, " +
                                 "ThumbNailPhoto:o, ThumbnailPhotoFileName:s, Weight:w, rowguid:s]", sqlTable.Type.ToStringWithDisplayNames());

            HashSet<IExternalTabularDataSource> ads = sqlTable.Type._type.AssociatedDataSources;
            Assert.NotNull(ads);

            Assert.Null(sqlTable.Relationships); // TO BE CHANGED, x-ms-relationships only for now

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

            // For SQL we don't have relationships
            bool b = sqlTable.RecordType.TryGetFieldExternalTableName("ProductModelID", out string externalTableName, out string foreignKey);
            Assert.False(b);
            
            testConnector.SetResponseFromFiles(@"Responses\SQL GetSchema ProductModel.json");
            b = sqlTable.RecordType.TryGetFieldType("ProductModelID", out FormulaType productModelID);

            Assert.True(b);
            DecimalType productModelId = Assert.IsType<DecimalType>(productModelID);
            Assert.False(productModelId is null);

            Assert.Equal("ProductID", string.Join("|", GetPrimaryKeyNames(sqlTable.RecordType)));
        }

        [Fact]
        public async Task SQL_CdpTabular_JoinCapabilityTest()
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
           
            CdpDataSource cds = new CdpDataSource("default,default");

            testConnector.SetResponseFromFiles(@"Responses\SQL GetDatasetsMetadata.json", @"Responses\SQL GetTables SampleDB.json");
            IEnumerable<CdpTable> tables = await cds.GetTablesAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);
            
            CdpTable table = tables.First(t => t.DisplayName == realTableName);
                       
            testConnector.SetResponseFromFiles(@"Responses\SQL GetSchema Products v2.json");
            await table.InitAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger);
            Assert.True(table.IsInitialized);

            CdpTableValue sqlTable = table.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegable);
            
            HashSet<IExternalTabularDataSource> ads = sqlTable.Type._type.AssociatedDataSources;
            Assert.NotNull(ads);
            Assert.Single(ads);

            DataSourceInfo dsi = Assert.IsType<DataSourceInfo>(ads.First());
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.True(dsi.DelegationInfo.SupportsJoinFunction);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public async Task SAP_CDP()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "1e702ce4f10c482684cee1465e686764";
            string jwt = "eyJ0eXAi...";
            using var client = new PowerPlatformConnectorClient("066d5714-1ffc-e316-90bd-affc61d8e6fd.18.common.tip2.azure-apihub.net", "066d5714-1ffc-e316-90bd-affc61d8e6fd", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            testConnector.SetResponseFromFile(@"Responses\SAP GetDataSetMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, $"/apim/sapodata/{connectionId}", CancellationToken.None, logger);

            CdpDataSource cds = new CdpDataSource("http://sapecckerb.roomsofthehouse.com:8080/sap/opu/odata/sap/HRESS_TEAM_CALENDAR_SERVICE");
            testConnector.SetResponseFromFiles(@"Responses\SAP GetDataSetMetadata.json", @"Responses\SAP GetTables.json");
            CdpTable sapTable = await cds.GetTableAsync(client, $"/apim/sapodata/{connectionId}", "TeamCalendarCollection", null, CancellationToken.None, logger);

            testConnector.SetResponseFromFile(@"Responses\SAP GetTable Schema.json");
            await sapTable.InitAsync(client, $"/apim/sapodata/{connectionId}", CancellationToken.None, logger);

            Assert.True(sapTable.IsInitialized);

            CdpTableValue sapTableValue = sapTable.GetTableValue();
            Assert.Equal<object>(
                "r*[ALL_EMPLOYEES:s, APP_MODE:s, BEGIN_DATE:s, BEGIN_DATE_CHAR:s, COMMAND:s, DESCRIPTION:s, EMP_PERNR:s, END_DATE:s, END_DATE_CHAR:s, EVENT_NAME:s, FLAG:s, GetMessages:*[MESSAGE:s, PERNR:s], " +
                "HIDE_PEERS:s, LEGEND:s, LEGENDID:s, LEGEND_TEXT:s, PERNR:s, PERNR_MEM_ID:s, TYPE:s]", sapTableValue.Type.ToStringWithDisplayNames());

            string expr = "First(TeamCalendarCollection).LEGEND_TEXT";

            SymbolValues symbolValues = new SymbolValues().Add("TeamCalendarCollection", sapTableValue);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            testConnector.SetResponseFromFile(@"Responses\SAP GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            StringValue sv = Assert.IsType<StringValue>(result);
            Assert.Equal("Holiday", sv.Value);

            // Not defined for SAP
            Assert.Equal(string.Empty, string.Join("|", GetPrimaryKeyNames(sapTableValue.RecordType)));
        }

        [Fact]
        [Obsolete("Using Join function")]
        public async Task SAP_CDP2()
        {            
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);            
            config.EnableJoinFunction();
            var engine = new RecalcEngine(config);            

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "b5097592f2ae498ea32458b1035634a9";
            string jwt = "eyJ0eXA...";
            using var client = new PowerPlatformConnectorClient("49970107-0806-e5a7-be5e-7c60e2750f01.12.common.firstrelease.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            testConnector.SetResponseFromFile(@"Responses\SAP GetDataSetMetadata.json");
            DatasetMetadata dm = await CdpDataSource.GetDatasetsMetadataAsync(client, $"/apim/sapodata/{connectionId}", CancellationToken.None, logger);

            CdpDataSource cds = new CdpDataSource("https://sapes5.sapdevcenter.com/sap/opu/odata/iwbep/GWSAMPLE_BASIC/");
            
            testConnector.SetResponseFromFiles(@"Responses\SAP GetDataSetMetadata.json", @"Responses\SAP GetTables 2.json");
            CdpTable sapTableProductSet = await cds.GetTableAsync(client, $"/apim/sapodata/{connectionId}", "ProductSet", null, CancellationToken.None, logger);

            testConnector.SetResponseFromFiles(@"Responses\SAP GetTables 2.json");
            CdpTable sapTableBusinessPartnerSet = await cds.GetTableAsync(client, $"/apim/sapodata/{connectionId}", "BusinessPartnerSet", null, CancellationToken.None, logger);

            testConnector.SetResponseFromFile(@"Responses\SAP_ProductSet_Schema.json");
            await sapTableProductSet.InitAsync(client, $"/apim/sapodata/{connectionId}", CancellationToken.None, logger);

            testConnector.SetResponseFromFile(@"Responses\SAP_BusinessPartnerSet_Schema.json");
            await sapTableBusinessPartnerSet.InitAsync(client, $"/apim/sapodata/{connectionId}", CancellationToken.None, logger);

            Assert.True(sapTableProductSet.IsInitialized);
            Assert.True(sapTableBusinessPartnerSet.IsInitialized);

            CdpTableValue sapTableValueProductSet = sapTableProductSet.GetTableValue();
            CdpTableValue sapTableValueBusinessPartnerSet = sapTableBusinessPartnerSet.GetTableValue();
            Assert.Equal<object>(
                "r*[Category:s, ChangedAt:s, CreatedAt:s, CurrencyCode:s, Depth:w, Description:s, DescriptionLanguage:s, DimUnit:s, Height:w, MeasureUnit:s, Name:s, " +
                "NameLanguage:s, Price:w, ProductID:s, SupplierID:s, SupplierName:s, TaxTarifCode:w, ToSalesOrderLineItems:~SalesOrderLineItemSet:![], ToSupplier:~BusinessPartnerSet:![], " +
                "TypeCode:s, WeightMeasure:w, WeightUnit:s, Width:w]", sapTableValueProductSet.Type.ToStringWithDisplayNames());
            
            string expr = "Join(ProductSet, BusinessPartnerSet, LeftRecord.SupplierID = RightRecord.BusinessPartnerID, JoinType.Left, RightRecord.EmailAddress As Email)";

            SymbolValues symbolValues = new SymbolValues()
                                               .Add("ProductSet", sapTableValueProductSet)
                                               .Add("BusinessPartnerSet", sapTableValueBusinessPartnerSet);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Calls to resolve ToXX navigation properties
            testConnector.SetResponseFromFiles(@"Responses\SAP_BusinessPartnerSet_Schema.json", @"Responses\SAP_SalesOrderLineItemSet_Schema.json");
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess, string.Join("\r\n", check.Errors.Select((er, i) => $"{i:00}: {er.Message}")));
            
            // Lazy Join leads to many network calls so this line is commented out
            // We'd need to investigate / add a cache in tests
            //FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            /*              
              Non-delegated result = Table(
                {Category:"Notebooks",ChangedAt:"2025-01-24T07:22:48.920808",CreatedAt:"2025-01-24T07:22:48.920808",CurrencyCode:"USD",Depth:Decimal(18),Description:"Notebook Basic",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(3),MeasureUnit:"EA",Name:"Notebook Basic 15",NameLanguage:"EN",Price:Decimal(956),ProductID:"GEAR",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.2),WeightUnit:"KG",Width:Decimal(30)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(18),Description:"Notebook Basic 15 with 2,80 GHz quad core, 15"" LCD, 4 GB DDR3 RAM, 500 GB Hard Disc, Windows 8 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-do.not.reply@sap.com",Height:Decimal(3),MeasureUnit:"EA",Name:"Notebook Basic 15",NameLanguage:"EN",Price:Decimal(956),ProductID:"HT-1000",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.2),WeightUnit:"KG",Width:Decimal(30)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(17),Description:"Notebook Basic 17 with 2,80 GHz quad core, 17"" LCD, 4 GB DDR3 RAM, 500 GB Hard Disc, Windows 8 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-dagmar.schulze@beckerberlin.de",Height:Decimal(3.1),MeasureUnit:"EA",Name:"Notebook Basic 17",NameLanguage:"EN",Price:Decimal(1249),ProductID:"HT-1001",SupplierID:"0100000047",SupplierName:"Becker Berlin",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.5),WeightUnit:"KG",Width:Decimal(29)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(19),Description:"Notebook Basic 18 with 2,80 GHz quad core, 18"" LCD, 8 GB DDR3 RAM, 1000 GB Hard Disc, Windows 8 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-maria.brown@delbont.com",Height:Decimal(2.5),MeasureUnit:"EA",Name:"Notebook Basic 18",NameLanguage:"EN",Price:Decimal(1570),ProductID:"HT-1002",SupplierID:"0100000048",SupplierName:"DelBont Industries",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.2),WeightUnit:"KG",Width:Decimal(28)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(21),Description:"Notebook Basic 19 with 2,80 GHz quad core, 19"" LCD, 8 GB DDR3 RAM, 1000 GB Hard Disc, Windows 8 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-saskia.sommer@talpa-hannover.de",Height:Decimal(4),MeasureUnit:"EA",Name:"Notebook Basic 19",NameLanguage:"EN",Price:Decimal(1650),ProductID:"HT-1003",SupplierID:"0100000049",SupplierName:"Talpa",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.2),WeightUnit:"KG",Width:Decimal(32)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(22),Description:"Digital Organizer with State-of-the-Art Storage Encryption",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-bob.buyer@panorama-studios.biz",Height:Decimal(3),MeasureUnit:"EA",Name:"ITelO Vault",NameLanguage:"EN",Price:Decimal(299),ProductID:"HT-1007",SupplierID:"0100000050",SupplierName:"Panorama Studios",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.2),WeightUnit:"KG",Width:Decimal(32)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(20),Description:"Notebook Professional 15 with 2,80 GHz quad core, 15"" Multitouch LCD, 8 GB DDR3 RAM, 500 GB SSD - DVD-Writer (DVD-R/+R/-RW/-RAM),Windows 8 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-bart.koenig@tecum-ag.de",Height:Decimal(3),MeasureUnit:"EA",Name:"Notebook Professional 15",NameLanguage:"EN",Price:Decimal(1999),ProductID:"HT-1010",SupplierID:"0100000051",SupplierName:"TECUM",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.3),WeightUnit:"KG",Width:Decimal(33)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(23),Description:"Notebook Professional 17 with 2,80 GHz quad core, 17"" Multitouch LCD, 8 GB DDR3 RAM, 500 GB SSD - DVD-Writer (DVD-R/+R/-RW/-RAM),Windows 8 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-yoko.nakamura@asia-ht.com",Height:Decimal(2),MeasureUnit:"EA",Name:"Notebook Professional 17",NameLanguage:"EN",Price:Decimal(2299),ProductID:"HT-1011",SupplierID:"0100000052",SupplierName:"Asia High tech",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.1),WeightUnit:"KG",Width:Decimal(33)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(1.8),Description:"Digital Organizer with State-of-the-Art Encryption for Storage and Network Communications",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sophie.ribery@laurent-paris.com",Height:Decimal(17),MeasureUnit:"EA",Name:"ITelO Vault Net",NameLanguage:"EN",Price:Decimal(459),ProductID:"HT-1020",SupplierID:"0100000053",SupplierName:"Laurent",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.16),WeightUnit:"KG",Width:Decimal(10)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(1.7),Description:"Digital Organizer with State-of-the-Art Encryption for Storage and Secure Stellite Link",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-victor.sanchez@avantel.com",Height:Decimal(18),MeasureUnit:"EA",Name:"ITelO Vault SAT",NameLanguage:"EN",Price:Decimal(149),ProductID:"HT-1021",SupplierID:"0100000054",SupplierName:"AVANTEL",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.18),WeightUnit:"KG",Width:Decimal(11)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(1.5),Description:"32 GB Digital Assistant with high-resolution color screen",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jorge.velez@telecomunicacionesstar.com",Height:Decimal(14),MeasureUnit:"EA",Name:"Comfort Easy",NameLanguage:"EN",Price:Decimal(1679),ProductID:"HT-1022",SupplierID:"0100000055",SupplierName:"Telecomunicaciones Star",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.2),WeightUnit:"KG",Width:Decimal(84)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(1.6),Description:"64 GB Digital Assistant with high-resolution color screen and synthesized voice output",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-franklin.jones@pear-computing.com",Height:Decimal(13),MeasureUnit:"EA",Name:"Comfort Senior",NameLanguage:"EN",Price:Decimal(512),ProductID:"HT-1023",SupplierID:"0100000056",SupplierName:"Pear Computing Services",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.8),WeightUnit:"KG",Width:Decimal(80)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(12),Description:"Optimum Hi-Resolution max. 1920 x 1080 @ 85Hz, Dot Pitch: 0.27mm",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-joseph_gschwandtner@alp-systems.at",Height:Decimal(36),MeasureUnit:"EA",Name:"Ergo Screen E-I",NameLanguage:"EN",Price:Decimal(230),ProductID:"HT-1030",SupplierID:"0100000057",SupplierName:"Alpine Systems",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(21),WeightUnit:"KG",Width:Decimal(37)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(19),Description:"Optimum Hi-Resolution max. 1920 x 1200 @ 85Hz, Dot Pitch: 0.26mm",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-george_d_grant@newlinedesign.co.uk",Height:Decimal(43),MeasureUnit:"EA",Name:"Ergo Screen E-II",NameLanguage:"EN",Price:Decimal(285),ProductID:"HT-1031",SupplierID:"0100000058",SupplierName:"New Line Design",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(21),WeightUnit:"KG",Width:Decimal(40.8)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(19),Description:"Optimum Hi-Resolution max. 2560 x 1440 @ 85Hz, Dot Pitch: 0.25mm",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sarah.schwind@hepa-tec.de",Height:Decimal(43),MeasureUnit:"EA",Name:"Ergo Screen E-III",NameLanguage:"EN",Price:Decimal(345),ProductID:"HT-1032",SupplierID:"0100000059",SupplierName:"HEPA Tec",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(21),WeightUnit:"KG",Width:Decimal(40.8)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(20),Description:"Optimum Hi-Resolution max. 1600 x 1200 @ 85Hz, Dot Pitch: 0.24mm",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-theodor.monathy@anavideon.com",Height:Decimal(41),MeasureUnit:"EA",Name:"Flat Basic",NameLanguage:"EN",Price:Decimal(399),ProductID:"HT-1035",SupplierID:"0100000060",SupplierName:"Anav Ideon",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(14),WeightUnit:"KG",Width:Decimal(39)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(26),Description:"Optimum Hi-Resolution max. 2048 x 1080 @ 85Hz, Dot Pitch: 0.26mm",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-robert_brown@rb-entertainment.ca",Height:Decimal(46),MeasureUnit:"EA",Name:"Flat Future",NameLanguage:"EN",Price:Decimal(430),ProductID:"HT-1036",SupplierID:"0100000061",SupplierName:"Robert Brown Entertainment",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(15),WeightUnit:"KG",Width:Decimal(45)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(22.1),Description:"Optimum Hi-Resolution max. 2016 x 1512 @ 85Hz, Dot Pitch: 0.24mm",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jorgemontalban@motc.mx",Height:Decimal(39.1),MeasureUnit:"EA",Name:"Flat XL",NameLanguage:"EN",Price:Decimal(1230),ProductID:"HT-1037",SupplierID:"0100000062",SupplierName:"Mexican Oil Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(17),WeightUnit:"KG",Width:Decimal(54.5)},
                {Category:"Laser Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(46),Description:"Print 2400 dpi image quality color documents at speeds of up to 32 ppm (color) or 36 ppm (monochrome), letter/A4. Powerful 500 MHz processor, 512MB of memory",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-johanna.esther@meliva.de",Height:Decimal(30),MeasureUnit:"EA",Name:"Laser Professional Eco",NameLanguage:"EN",Price:Decimal(830),ProductID:"HT-1040",SupplierID:"0100000063",SupplierName:"Meliva",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(32),WeightUnit:"KG",Width:Decimal(51)},
                {Category:"Laser Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(42),Description:"Up to 22 ppm color or 24 ppm monochrome A4/letter, powerful 500 MHz processor and 128MB of memory",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-miguel.luengo@compostela.ar",Height:Decimal(26),MeasureUnit:"EA",Name:"Laser Basic",NameLanguage:"EN",Price:Decimal(490),ProductID:"HT-1041",SupplierID:"0100000064",SupplierName:"Compostela",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(23),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Laser Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(50),Description:"Print up to 25 ppm letter and 24 ppm A4 color or monochrome, with a first-page-out-time of less than 13 seconds for monochrome and less than 15 seconds for color",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-isabel.nemours@pateu.fr",Height:Decimal(65),MeasureUnit:"EA",Name:"Laser Allround",NameLanguage:"EN",Price:Decimal(349),ProductID:"HT-1042",SupplierID:"0100000065",SupplierName:"Pateu",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(17),WeightUnit:"KG",Width:Decimal(53)},
                {Category:"Ink Jet Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(41),Description:"4800 dpi x 1200 dpi - up to 35 ppm (mono) / up to 34 ppm (color) - capacity: 250 sheets - Hi-Speed USB, Ethernet",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-igor.tarassow@retc.ru",Height:Decimal(28),MeasureUnit:"EA",Name:"Ultra Jet Super Color",NameLanguage:"EN",Price:Decimal(139),ProductID:"HT-1050",SupplierID:"0100000066",SupplierName:"Russian Electronic Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3),WeightUnit:"KG",Width:Decimal(41)},
                {Category:"Ink Jet Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(32),Description:"1000 dpi x 1000 dpi - up to 35 ppm (mono) / up to 34 ppm (color) - capacity: 250 sheets - Hi-Speed USB - excellent dimensions for the small office",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-alexis.harper@flor-hc.com",Height:Decimal(25),MeasureUnit:"EA",Name:"Ultra Jet Mobile",NameLanguage:"EN",Price:Decimal(99),ProductID:"HT-1051",SupplierID:"0100000067",SupplierName:"Florida Holiday Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.9),WeightUnit:"KG",Width:Decimal(46)},
                {Category:"Ink Jet Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(41),Description:"4800 dpi x 1200 dpi - up to 35 ppm (mono) / up to 34 ppm (color) - capacity: 250 sheets - Hi-Speed USB2.0, Ethernet",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-c.alfaro@quimica-madrilenos.es",Height:Decimal(28),MeasureUnit:"EA",Name:"Ultra Jet Super Highspeed",NameLanguage:"EN",Price:Decimal(170),ProductID:"HT-1052",SupplierID:"0100000068",SupplierName:"Quimica Madrilenos",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(18),WeightUnit:"KG",Width:Decimal(41)},
                {Category:"Multifunction Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(45),Description:"1000 dpi x 1000 dpi - up to 16 ppm (mono) / up to 15 ppm (color)- capacity 80 sheets - scanner (216 x 297 mm, 1200dpi x 2400dpi)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sven.j@getraenke-janssen.de",Height:Decimal(29),MeasureUnit:"EA",Name:"Multi Print",NameLanguage:"EN",Price:Decimal(99),ProductID:"HT-1055",SupplierID:"0100000069",SupplierName:"Getränkegroßhandel Janssen",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(6.3),WeightUnit:"KG",Width:Decimal(55)},
                {Category:"Multifunction Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(41.3),Description:"1200 dpi x 1200 dpi - up to 25 ppm (mono) / up to 24 ppm (color)- capacity 80 sheets - scanner (216 x 297 mm, 2400dpi x 4800dpi, high resolution)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-yoshiko.kakuji@jateco.jp",Height:Decimal(22),MeasureUnit:"EA",Name:"Multi Color",NameLanguage:"EN",Price:Decimal(119),ProductID:"HT-1056",SupplierID:"0100000070",SupplierName:"JaTeCo",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.3),WeightUnit:"KG",Width:Decimal(51)},
                {Category:"Mice",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(14.5),Description:"Cordless Optical USB Mice, Laptop, Color: Black, Plug&Play",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-alessio.galasso@tcdr.it",Height:Decimal(3.5),MeasureUnit:"EA",Name:"Cordless Mouse",NameLanguage:"EN",Price:Decimal(9),ProductID:"HT-1060",SupplierID:"0100000071",SupplierName:"Tessile Casa Di Roma",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.09),WeightUnit:"KG",Width:Decimal(6)},
                {Category:"Mice",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(15),Description:"Optical USB, PS/2 Mouse, Color: Blue, 3-button-functionality (incl. Scroll wheel)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-romain.le_mason@verdo.fr",Height:Decimal(3.1),MeasureUnit:"EA",Name:"Speed Mouse",NameLanguage:"EN",Price:Decimal(7),ProductID:"HT-1061",SupplierID:"0100000072",SupplierName:"Vente Et Réparation de Ordinateur",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.09),WeightUnit:"KG",Width:Decimal(7)},
                {Category:"Mice",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(7),Description:"Optical USB Mouse, Color: Red, 5-button-functionality(incl. Scroll wheel), Plug&Play",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-martha.calcagno@dpg.ar",Height:Decimal(4),MeasureUnit:"EA",Name:"Track Mouse",NameLanguage:"EN",Price:Decimal(11),ProductID:"HT-1062",SupplierID:"0100000073",SupplierName:"Developement Para O Governo",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.03),WeightUnit:"KG",Width:Decimal(3)},
                {Category:"Keyboards",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(21),Description:"Ergonomic USB Keyboard for Desktop, Plug&Play",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-beatriz.da_silva@brazil-tec.br",Height:Decimal(3.5),MeasureUnit:"EA",Name:"Ergonomic Keyboard",NameLanguage:"EN",Price:Decimal(14),ProductID:"HT-1063",SupplierID:"0100000074",SupplierName:"Brazil Technologies",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.1),WeightUnit:"KG",Width:Decimal(50)},
                {Category:"Keyboards",ChangedAt:"2025-01-24T10:40:00.226274",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(25),Description:"Corded Keyboard with special keys for Internet Usability, USB",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-anthony.lebouef@crtu.ca",Height:Decimal(3),MeasureUnit:"EA",Name:"Internet Keyboard",NameLanguage:"EN",Price:Decimal(18),ProductID:"HT-1064",SupplierID:"0100000075",SupplierName:"C.R.T.U.",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.8),WeightUnit:"KG",Width:Decimal(52)},
                {Category:"Keyboards",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(23),Description:"Corded Ergonomic Keyboard with special keys for Media Usability, USB",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-lisa.felske@jologa.ch",Height:Decimal(4),MeasureUnit:"EA",Name:"Media Keyboard",NameLanguage:"EN",Price:Decimal(26),ProductID:"HT-1065",SupplierID:"0100000076",SupplierName:"Jologa",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.3),WeightUnit:"KG",Width:Decimal(51.4)},
                {Category:"Mousepads",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6),Description:"Nice mouse pad with ITelO Logo",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jonathan.d.mason@baleda.com",Height:Decimal(0.2),MeasureUnit:"EA",Name:"Mousepad",NameLanguage:"EN",Price:Decimal(6.99),ProductID:"HT-1066",SupplierID:"0100000077",SupplierName:"Baleda",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(80),WeightUnit:"G",Width:Decimal(15)},
                {Category:"Mousepads",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6),Description:"Ergonomic mouse pad with ITelO Logo",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-amelie.troyat@angere.fr",Height:Decimal(0.2),MeasureUnit:"EA",Name:"Ergo Mousepad",NameLanguage:"EN",Price:Decimal(8.99),ProductID:"HT-1067",SupplierID:"0100000078",SupplierName:"Angeré",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(80),WeightUnit:"G",Width:Decimal(15)},
                {Category:"Mousepads",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(24),Description:"ITelO Mousepad Special Edition",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-pete_waltham@pc-gym-tec.com",Height:Decimal(0.6),MeasureUnit:"EA",Name:"Designer Mousepad",NameLanguage:"EN",Price:Decimal(12.99),ProductID:"HT-1068",SupplierID:"0100000079",SupplierName:"PC Gym Tec",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(90),WeightUnit:"G",Width:Decimal(24)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6),Description:"Universal card reader",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-ryu.toshiro@jip.jp",Height:Decimal(3),MeasureUnit:"EA",Name:"Universal card reader",NameLanguage:"EN",Price:Decimal(14),ProductID:"HT-1069",SupplierID:"0100000080",SupplierName:"Japan Insurance Partner",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(45),WeightUnit:"G",Width:Decimal(6)},
                {Category:"Graphic Cards",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(35),Description:"Proctra X: PCI-E GDDR5 3072MB",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jose-lopez@en-ar.ar",Height:Decimal(17),MeasureUnit:"EA",Name:"Proctra X",NameLanguage:"EN",Price:Decimal(70.9),ProductID:"HT-1070",SupplierID:"0100000081",SupplierName:"Entertainment Argentinia",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.255),WeightUnit:"KG",Width:Decimal(22)},
                {Category:"Graphic Cards",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(35),Description:"Gladiator XLN: PCI-E GDDR5 3072MB DVI Out, TV Out low-noise",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-dahoma.lawla@agadc.co.za",Height:Decimal(17),MeasureUnit:"EA",Name:"Gladiator MX",NameLanguage:"EN",Price:Decimal(81.7),ProductID:"HT-1071",SupplierID:"0100000082",SupplierName:"African Gold And Diamond Corporation",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.3),WeightUnit:"KG",Width:Decimal(22)},
                {Category:"Graphic Cards",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(35),Description:"Hurricane GX: PCI-E 691 GFLOPS game-optimized",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jefferson.parker@pico-bit.com",Height:Decimal(17),MeasureUnit:"EA",Name:"Hurricane GX",NameLanguage:"EN",Price:Decimal(101.2),ProductID:"HT-1072",SupplierID:"0100000083",SupplierName:"PicoBit",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.4),WeightUnit:"KG",Width:Decimal(22)},
                {Category:"Graphic Cards",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(35),Description:"Hurricane GX/LN: PCI-E 691 GFLOPS game-optimized, low-noise.",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-tamara.flaig@brl-ag.de",Height:Decimal(17),MeasureUnit:"EA",Name:"Hurricane GX/LN",NameLanguage:"EN",Price:Decimal(139.99),ProductID:"HT-1073",SupplierID:"0100000084",SupplierName:"Bionic Research Lab",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.4),WeightUnit:"KG",Width:Decimal(22)},
                {Category:"Scanners",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(48),Description:"Flatbed scanner - 9.600 × 9.600 dpi - 216 x 297 mm - Hi-Speed USB - Bluetooth",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sunita-kapoor@it-trade.in",Height:Decimal(5),MeasureUnit:"EA",Name:"Photo Scan",NameLanguage:"EN",Price:Decimal(129),ProductID:"HT-1080",SupplierID:"0100000085",SupplierName:"Indian IT Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.3),WeightUnit:"KG",Width:Decimal(34)},
                {Category:"Scanners",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(43),Description:"Flatbed scanner - 9.600 × 9.600 dpi - 216 x 297 mm - SCSI for backward compatibility",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-pawel-lewandoski@catf.pl",Height:Decimal(7),MeasureUnit:"EA",Name:"Power Scan",NameLanguage:"EN",Price:Decimal(89),ProductID:"HT-1081",SupplierID:"0100000086",SupplierName:"Chemia A Technicznie Fabryka",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.4),WeightUnit:"KG",Width:Decimal(31)},
                {Category:"Scanners",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(41),Description:"Flatbed scanner - Letter - 2400 dpi x 2400 dpi - 216 x 297 mm - add-on module",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-laura.campillo@saitc.ar",Height:Decimal(12),MeasureUnit:"EA",Name:"Jet Scan Professional",NameLanguage:"EN",Price:Decimal(169),ProductID:"HT-1082",SupplierID:"0100000087",SupplierName:"South American IT Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.2),WeightUnit:"KG",Width:Decimal(33)},
                {Category:"Scanners",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(40),Description:"Flatbed scanner - A4 - 2400 dpi x 2400 dpi - 216 x 297 mm - add-on module",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jian.si@siwusha.cn",Height:Decimal(10),MeasureUnit:"EA",Name:"Jet Scan Professional",NameLanguage:"EN",Price:Decimal(189),ProductID:"HT-1083",SupplierID:"0100000088",SupplierName:"Siwusha",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.2),WeightUnit:"KG",Width:Decimal(35)},
                {Category:"Multifunction Printers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(42),Description:"Copymaster",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-frederik.christensen@dftc.dk",Height:Decimal(22),MeasureUnit:"EA",Name:"Copymaster",NameLanguage:"EN",Price:Decimal(1499),ProductID:"HT-1085",SupplierID:"0100000089",SupplierName:"Danish Fish Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(23.2),WeightUnit:"KG",Width:Decimal(45)},
                {Category:"Speakers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(10),Description:"PC multimedia speakers - 5 Watt (Total)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-mirjam.schmidt@sorali.de",Height:Decimal(16),MeasureUnit:"EA",Name:"Surround Sound",NameLanguage:"EN",Price:Decimal(39),ProductID:"HT-1090",SupplierID:"0100000090",SupplierName:"Sorali",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3),WeightUnit:"KG",Width:Decimal(12)},
                {Category:"Speakers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(11),Description:"PC multimedia speakers - 10 Watt (Total) - 2-way",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-do.not.reply@sap.com",Height:Decimal(17.5),MeasureUnit:"EA",Name:"Blaster Extreme",NameLanguage:"EN",Price:Decimal(26),ProductID:"HT-1091",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.4),WeightUnit:"KG",Width:Decimal(13)},
                {Category:"Speakers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(10.4),Description:"PC multimedia speakers - optimized for Blutooth/A2DP",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-dagmar.schulze@beckerberlin.de",Height:Decimal(18.1),MeasureUnit:"EA",Name:"Sound Booster",NameLanguage:"EN",Price:Decimal(45),ProductID:"HT-1092",SupplierID:"0100000047",SupplierName:"Becker Berlin",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.1),WeightUnit:"KG",Width:Decimal(12.4)},
                {Category:"Headsets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(19),Description:"5.1 Headset, 40 Hz-20 kHz, Wireless",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-pete_waltham@pc-gym-tec.com",Height:Decimal(23),MeasureUnit:"EA",Name:"Lovely Sound 5.1 Wireless",NameLanguage:"EN",Price:Decimal(49),ProductID:"HT-1095",SupplierID:"0100000079",SupplierName:"PC Gym Tec",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(80),WeightUnit:"G",Width:Decimal(24)},
                {Category:"Headsets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(17),Description:"5.1 Headset, 40 Hz-20 kHz, 3m cable",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-ryu.toshiro@jip.jp",Height:Decimal(19),MeasureUnit:"EA",Name:"Lovely Sound 5.1",NameLanguage:"EN",Price:Decimal(39),ProductID:"HT-1096",SupplierID:"0100000080",SupplierName:"Japan Insurance Partner",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(130),WeightUnit:"G",Width:Decimal(25)},
                {Category:"Headsets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(2.4),Description:"5.1 Headset, 40 Hz-20 kHz, 1m cable",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jose-lopez@en-ar.ar",Height:Decimal(19.7),MeasureUnit:"EA",Name:"Lovely Sound Stereo",NameLanguage:"EN",Price:Decimal(29),ProductID:"HT-1097",SupplierID:"0100000081",SupplierName:"Entertainment Argentinia",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(60),WeightUnit:"G",Width:Decimal(21.3)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6.5),Description:"Complete package, 1 User, Office Applications (word processing, spreadsheet, presentations)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-maria.brown@delbont.com",Height:Decimal(2.1),MeasureUnit:"EA",Name:"Smart Office",NameLanguage:"EN",Price:Decimal(89.9),ProductID:"HT-1100",SupplierID:"0100000048",SupplierName:"DelBont Industries",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.2),WeightUnit:"KG",Width:Decimal(15)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6.7),Description:"Complete package, 1 User, Image editing, processing",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-saskia.sommer@talpa-hannover.de",Height:Decimal(24),MeasureUnit:"EA",Name:"Smart Design",NameLanguage:"EN",Price:Decimal(79.9),ProductID:"HT-1101",SupplierID:"0100000049",SupplierName:"Talpa",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.8),WeightUnit:"KG",Width:Decimal(14)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6),Description:"Complete package, 1 User, Network Software Utilities, Useful Applications and Documentation",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-bob.buyer@panorama-studios.biz",Height:Decimal(27),MeasureUnit:"EA",Name:"Smart Network",NameLanguage:"EN",Price:Decimal(69),ProductID:"HT-1102",SupplierID:"0100000050",SupplierName:"Panorama Studios",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.8),WeightUnit:"KG",Width:Decimal(16)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(3.4),Description:"Complete package, 1 User, different Multimedia applications, playing music, watching DVDs, only with this Smart package",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-bart.koenig@tecum-ag.de",Height:Decimal(22),MeasureUnit:"EA",Name:"Smart Multimedia",NameLanguage:"EN",Price:Decimal(77),ProductID:"HT-1103",SupplierID:"0100000051",SupplierName:"TECUM",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.8),WeightUnit:"KG",Width:Decimal(11)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(3),Description:"Complete package, 1 User, various games for amusement, logic, action, jump&run",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-yoko.nakamura@asia-ht.com",Height:Decimal(30),MeasureUnit:"EA",Name:"Smart Games",NameLanguage:"EN",Price:Decimal(55),ProductID:"HT-1104",SupplierID:"0100000052",SupplierName:"Asia High tech",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.1),WeightUnit:"KG",Width:Decimal(10)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(4),Description:"Complete package, 1 User, highly recommended for internet users as anti-virus protection",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sophie.ribery@laurent-paris.com",Height:Decimal(21),MeasureUnit:"EA",Name:"Smart Internet Antivirus",NameLanguage:"EN",Price:Decimal(29),ProductID:"HT-1105",SupplierID:"0100000053",SupplierName:"Laurent",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.7),WeightUnit:"KG",Width:Decimal(16)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(4.2),Description:"Complete package, 1 User, recommended for internet users, protect your PC against cyber-crime",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-victor.sanchez@avantel.com",Height:Decimal(23.1),MeasureUnit:"EA",Name:"Smart Firewall",NameLanguage:"EN",Price:Decimal(34),ProductID:"HT-1106",SupplierID:"0100000054",SupplierName:"AVANTEL",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.9),WeightUnit:"KG",Width:Decimal(17.9)},
                {Category:"Software",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(1.5),Description:"Complete package, 1 User, bring your money in your mind, see what you have and what you want",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jorge.velez@telecomunicacionesstar.com",Height:Decimal(19),MeasureUnit:"EA",Name:"Smart Money",NameLanguage:"EN",Price:Decimal(29.9),ProductID:"HT-1107",SupplierID:"0100000055",SupplierName:"Telecomunicaciones Star",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.5),WeightUnit:"KG",Width:Decimal(12)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(8),Description:"Robust 3m anti-burglary protection for your laptop computer",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-franklin.jones@pear-computing.com",Height:Decimal(4.3),MeasureUnit:"EA",Name:"PC Lock",NameLanguage:"EN",Price:Decimal(8.9),ProductID:"HT-1110",SupplierID:"0100000056",SupplierName:"Pear Computing Services",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.03),WeightUnit:"KG",Width:Decimal(20)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(9),Description:"Robust 1m anti-burglary protection for your desktop computer",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-joseph_gschwandtner@alp-systems.at",Height:Decimal(7),MeasureUnit:"EA",Name:"Notebook Lock",NameLanguage:"EN",Price:Decimal(6.9),ProductID:"HT-1111",SupplierID:"0100000057",SupplierName:"Alpine Systems",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.02),WeightUnit:"KG",Width:Decimal(31)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(8.2),Description:"Color webcam, color, High-Speed USB",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-george_d_grant@newlinedesign.co.uk",Height:Decimal(1.3),MeasureUnit:"EA",Name:"Web cam reality",NameLanguage:"EN",Price:Decimal(39),ProductID:"HT-1112",SupplierID:"0100000058",SupplierName:"New Line Design",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.075),WeightUnit:"KG",Width:Decimal(9)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(2),Description:"10 separately packed screen wipes",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sarah.schwind@hepa-tec.de",Height:Decimal(0.1),MeasureUnit:"EA",Name:"Screen clean",NameLanguage:"EN",Price:Decimal(2.3),ProductID:"HT-1113",SupplierID:"0100000059",SupplierName:"HEPA Tec",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.05),WeightUnit:"KG",Width:Decimal(2)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(32),Description:"Notebook bag, plenty of room for stationery and writing materials",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-theodor.monathy@anavideon.com",Height:Decimal(7),MeasureUnit:"EA",Name:"Fabric bag professional",NameLanguage:"EN",Price:Decimal(31),ProductID:"HT-1114",SupplierID:"0100000060",SupplierName:"Anav Ideon",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.8),WeightUnit:"KG",Width:Decimal(42)},
                {Category:"Telecommunications",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(18),Description:"Wireless DSL Router (available in blue, black and silver)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-robert_brown@rb-entertainment.ca",Height:Decimal(5),MeasureUnit:"EA",Name:"Wireless DSL Router",NameLanguage:"EN",Price:Decimal(49),ProductID:"HT-1115",SupplierID:"0100000061",SupplierName:"Robert Brown Entertainment",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.45),WeightUnit:"KG",Width:Decimal(19.3)},
                {Category:"Telecommunications",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(18),Description:"Wireless DSL Router / Repeater (available in blue, black and silver)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jorgemontalban@motc.mx",Height:Decimal(5),MeasureUnit:"EA",Name:"Wireless DSL Router / Repeater",NameLanguage:"EN",Price:Decimal(59),ProductID:"HT-1116",SupplierID:"0100000062",SupplierName:"Mexican Oil Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.45),WeightUnit:"KG",Width:Decimal(19.3)},
                {Category:"Telecommunications",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(18),Description:"Wireless DSL Router / Repeater and Print Server (available in blue, black and silver)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-johanna.esther@meliva.de",Height:Decimal(5),MeasureUnit:"EA",Name:"Wireless DSL Router / Repeater and Print Server",NameLanguage:"EN",Price:Decimal(69),ProductID:"HT-1117",SupplierID:"0100000063",SupplierName:"Meliva",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.45),WeightUnit:"KG",Width:Decimal(19.3)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(8.7),Description:"USB 2.0 High-Speed 64 GB",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-miguel.luengo@compostela.ar",Height:Decimal(1.2),MeasureUnit:"EA",Name:"USB Stick",NameLanguage:"EN",Price:Decimal(35),ProductID:"HT-1118",SupplierID:"0100000064",SupplierName:"Compostela",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.015),WeightUnit:"KG",Width:Decimal(1.5)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(3.1),Description:"Universal Travel Adapter",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-franklin.jones@pear-computing.com",Height:Decimal(3.9),MeasureUnit:"EA",Name:"Travel Adapter",NameLanguage:"EN",Price:Decimal(79),ProductID:"HT-1119",SupplierID:"0100000056",SupplierName:"Pear Computing Services",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(88),WeightUnit:"G",Width:Decimal(2)},
                {Category:"Keyboards",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(23),Description:"Cordless Bluetooth Keyboard with English keys",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-isabel.nemours@pateu.fr",Height:Decimal(4),MeasureUnit:"EA",Name:"Cordless Bluetooth Keyboard, english international",NameLanguage:"EN",Price:Decimal(29),ProductID:"HT-1120",SupplierID:"0100000065",SupplierName:"Pateu",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1),WeightUnit:"KG",Width:Decimal(51.4)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(22),Description:"Optimum Hi-Resolution max. 2048 × 1536 @ 85Hz, Dot Pitch: 0.24mm",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-igor.tarassow@retc.ru",Height:Decimal(38),MeasureUnit:"EA",Name:"Flat XXL",NameLanguage:"EN",Price:Decimal(1430),ProductID:"HT-1137",SupplierID:"0100000066",SupplierName:"Russian Electronic Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(18),WeightUnit:"KG",Width:Decimal(54)},
                {Category:"Mice",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(0.5),Description:"Portable pocket Mouse with retracting cord",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-alexis.harper@flor-hc.com",Height:Decimal(1),MeasureUnit:"EA",Name:"Pocket Mouse",NameLanguage:"EN",Price:Decimal(23),ProductID:"HT-1138",SupplierID:"0100000067",SupplierName:"Florida Holiday Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.02),WeightUnit:"KG",Width:Decimal(0.3)},
                {Category:"PCs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"PC Power Station with 3,4 Ghz quad-core, 32 GB DDR3 SDRAM, feels like a PC, Windows 8 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-c.alfaro@quimica-madrilenos.es",Height:Decimal(43),MeasureUnit:"EA",Name:"PC Power Station",NameLanguage:"EN",Price:Decimal(2399),ProductID:"HT-1210",SupplierID:"0100000068",SupplierName:"Quimica Madrilenos",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.3),WeightUnit:"KG",Width:Decimal(28)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(18),Description:"Flexible Laptop with 2,5 GHz Quad Core, 15"" HD TN, 16 GB DDR SDRAM, 256 GB SSD, Windows 10 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-alessio.galasso@tcdr.it",Height:Decimal(3),MeasureUnit:"EA",Name:"Astro Laptop 1516",NameLanguage:"EN",Price:Decimal(989),ProductID:"HT-1251",SupplierID:"0100000071",SupplierName:"Tessile Casa Di Roma",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.2),WeightUnit:"KG",Width:Decimal(30)},
                {Category:"Smartphones",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6),Description:"6 inch 1280x800 HD display (216 ppi), Quad-core processor, 8 GB internal storage (actual formatted capacity will be less), 3050 mAh battery (Up to 8 hours of active use), grey or black",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-romain.le_mason@verdo.fr",Height:Decimal(1.5),MeasureUnit:"EA",Name:"Astro Phone 6",NameLanguage:"EN",Price:Decimal(649),ProductID:"HT-1252",SupplierID:"0100000072",SupplierName:"Vente Et Réparation de Ordinateur",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.75),WeightUnit:"KG",Width:Decimal(8)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(18),Description:"Flexible Laptop with 2,5 GHz Dual Core, 14"" HD+ TN, 8 GB DDR SDRAM, 324 GB SSD, Windows 10 Pro",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-martha.calcagno@dpg.ar",Height:Decimal(3),MeasureUnit:"EA",Name:"Benda Laptop 1408",NameLanguage:"EN",Price:Decimal(976),ProductID:"HT-1253",SupplierID:"0100000073",SupplierName:"Developement Para O Governo",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.2),WeightUnit:"KG",Width:Decimal(30)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(12),Description:"Optimum Hi-Resolution Widescreen max. 1920 x 1080 @ 85Hz, Dot Pitch: 0.27mm, HDMI, D-Sub",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-beatriz.da_silva@brazil-tec.br",Height:Decimal(36),MeasureUnit:"EA",Name:"Bending Screen 21HD",NameLanguage:"EN",Price:Decimal(250),ProductID:"HT-1254",SupplierID:"0100000074",SupplierName:"Brazil Technologies",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(15),WeightUnit:"KG",Width:Decimal(37)},
                {Category:"Flat Screen Monitors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(12),Description:"Optimum Hi-Resolution Widescreen max. 2048 x 1080 @ 85Hz, Dot Pitch: 0.27mm, HDMI, D-Sub",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-anthony.lebouef@crtu.ca",Height:Decimal(38),MeasureUnit:"EA",Name:"Broad Screen 22HD",NameLanguage:"EN",Price:Decimal(270),ProductID:"HT-1255",SupplierID:"0100000075",SupplierName:"C.R.T.U.",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(16),WeightUnit:"KG",Width:Decimal(39)},
                {Category:"Smartphones",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(15),Description:"7 inch 1280x800 HD display (216 ppi), Quad-core processor, 16 GB internal storage (actual formatted capacity will be less), 4325 mAh battery (Up to 8 hours of active use), white or black",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-lisa.felske@jologa.ch",Height:Decimal(1.5),MeasureUnit:"EA",Name:"Cerdik Phone 7",NameLanguage:"EN",Price:Decimal(549),ProductID:"HT-1256",SupplierID:"0100000076",SupplierName:"Jologa",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.75),WeightUnit:"KG",Width:Decimal(9)},
                {Category:"Tablets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"10.5-inch Multitouch HD Screen (1280 x 800), 16GB Internal Memory, Wireless N Wi-Fi; Bluetooth, GPS Enabled, 1GHz Dual-Core Processor",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jonathan.d.mason@baleda.com",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Cepat Tablet 10.5",NameLanguage:"EN",Price:Decimal(549),ProductID:"HT-1257",SupplierID:"0100000077",SupplierName:"Baleda",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.8),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Tablets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(21),Description:"8-inch Multitouch HD Screen (2000 x 1500) 32GB Internal Memory, Wireless N Wi-Fi, Bluetooth, GPS Enabled, 1.5 GHz Quad-Core Processor",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-amelie.troyat@angere.fr",Height:Decimal(3.5),MeasureUnit:"EA",Name:"Cepat Tablet 8",NameLanguage:"EN",Price:Decimal(529),ProductID:"HT-1258",SupplierID:"0100000078",SupplierName:"Angeré",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.5),WeightUnit:"KG",Width:Decimal(38)},
                {Category:"Servers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(35),Description:"Dual socket, quad-core processing server with 1333 MHz Front Side Bus with 10Gb connectivity",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sven.j@getraenke-janssen.de",Height:Decimal(23),MeasureUnit:"EA",Name:"Server Basic",NameLanguage:"EN",Price:Decimal(5000),ProductID:"HT-1500",SupplierID:"0100000069",SupplierName:"Getränkegroßhandel Janssen",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(18),WeightUnit:"KG",Width:Decimal(34)},
                {Category:"Servers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(30),Description:"Dual socket, quad-core processing server with 1644 MHz Front Side Bus with 10Gb connectivity",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-yoshiko.kakuji@jateco.jp",Height:Decimal(27),MeasureUnit:"EA",Name:"Server Professional",NameLanguage:"EN",Price:Decimal(15000),ProductID:"HT-1501",SupplierID:"0100000070",SupplierName:"JaTeCo",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(25),WeightUnit:"KG",Width:Decimal(29)},
                {Category:"Servers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(27.3),Description:"Dual socket, quad-core processing server with 1644 MHz Front Side Bus with 100Gb connectivity",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-alessio.galasso@tcdr.it",Height:Decimal(37),MeasureUnit:"EA",Name:"Server Power Pro",NameLanguage:"EN",Price:Decimal(25000),ProductID:"HT-1502",SupplierID:"0100000071",SupplierName:"Tessile Casa Di Roma",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(35),WeightUnit:"KG",Width:Decimal(22)},
                {Category:"PCs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(29),Description:"2,8 Ghz dual core, 4 GB DDR3 SDRAM, 500 GB Hard Disc, Graphic Card: Proctra X, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jorge.velez@telecomunicacionesstar.com",Height:Decimal(38),MeasureUnit:"EA",Name:"Family PC Basic",NameLanguage:"EN",Price:Decimal(600),ProductID:"HT-1600",SupplierID:"0100000055",SupplierName:"Telecomunicaciones Star",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.8),WeightUnit:"KG",Width:Decimal(21.4)},
                {Category:"PCs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31.7),Description:"2,8 Ghz dual core, 4 GB DDR3 SDRAM, 1000 GB Hard Disc, Graphic Card: Gladiator MX, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-victor.sanchez@avantel.com",Height:Decimal(40.2),MeasureUnit:"EA",Name:"Family PC Pro",NameLanguage:"EN",Price:Decimal(900),ProductID:"HT-1601",SupplierID:"0100000054",SupplierName:"AVANTEL",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(5.3),WeightUnit:"KG",Width:Decimal(25)},
                {Category:"PCs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(34),Description:"3,4 Ghz quad core, 8 GB DDR3 SDRAM, 2000 GB Hard Disc, Graphic Card: Gladiator MX, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sophie.ribery@laurent-paris.com",Height:Decimal(47),MeasureUnit:"EA",Name:"Gaming Monster",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-1602",SupplierID:"0100000053",SupplierName:"Laurent",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(5.9),WeightUnit:"KG",Width:Decimal(26.5)},
                {Category:"PCs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(28),Description:"3,4 Ghz quad core, 16 GB DDR3 SDRAM, 4000 GB Hard Disc, Graphic Card: Hurricane GX, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-yoko.nakamura@asia-ht.com",Height:Decimal(42),MeasureUnit:"EA",Name:"Gaming Monster Pro",NameLanguage:"EN",Price:Decimal(1700),ProductID:"HT-1603",SupplierID:"0100000052",SupplierName:"Asia High tech",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(6.8),WeightUnit:"KG",Width:Decimal(27)},
                {Category:"Portable Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(19),Description:"7"" LCD Screen, storage battery holds up to 6 hours!",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-bart.koenig@tecum-ag.de",Height:Decimal(27.6),MeasureUnit:"EA",Name:"7"" Widescreen Portable DVD Player w MP3",NameLanguage:"EN",Price:Decimal(249.99),ProductID:"HT-2000",SupplierID:"0100000051",SupplierName:"TECUM",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.79),WeightUnit:"KG",Width:Decimal(21.4)},
                {Category:"Portable Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(19.5),Description:"10"" LCD Screen, storage battery holds up to 8 hours",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-bob.buyer@panorama-studios.biz",Height:Decimal(29),MeasureUnit:"EA",Name:"10"" Portable DVD player",NameLanguage:"EN",Price:Decimal(449.99),ProductID:"HT-2001",SupplierID:"0100000050",SupplierName:"Panorama Studios",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.84),WeightUnit:"KG",Width:Decimal(24)},
                {Category:"Portable Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(16.5),Description:"9"" LCD Screen, storage holds up to 8 hours, 2 speakers included",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-mirjam.schmidt@sorali.de",Height:Decimal(14),MeasureUnit:"EA",Name:"Portable DVD Player with 9"" LCD Monitor",NameLanguage:"EN",Price:Decimal(853.99),ProductID:"HT-2002",SupplierID:"0100000090",SupplierName:"Sorali",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.72),WeightUnit:"KG",Width:Decimal(21)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(13),Description:"Organizer and protective case for 264 CDs and DVDs",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-saskia.sommer@talpa-hannover.de",Height:Decimal(20),MeasureUnit:"EA",Name:"CD/DVD case: 264 sleeves",NameLanguage:"EN",Price:Decimal(44.99),ProductID:"HT-2025",SupplierID:"0100000049",SupplierName:"Talpa",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.65),WeightUnit:"KG",Width:Decimal(13)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(10.2),Description:"Quality cables for notebooks and projectors",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-maria.brown@delbont.com",Height:Decimal(13),MeasureUnit:"EA",Name:"Audio/Video Cable Kit - 4m",NameLanguage:"EN",Price:Decimal(29.99),ProductID:"HT-2026",SupplierID:"0100000048",SupplierName:"DelBont Industries",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.2),WeightUnit:"KG",Width:Decimal(21)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(2),Description:"Removable jewel case labels, zero residues (100)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-dagmar.schulze@beckerberlin.de",Height:Decimal(2),MeasureUnit:"EA",Name:"Removable CD/DVD Laser Labels",NameLanguage:"EN",Price:Decimal(8.99),ProductID:"HT-2027",SupplierID:"0100000047",SupplierName:"Becker Berlin",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.15),WeightUnit:"KG",Width:Decimal(5.5)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T02:21:33.471027",CreatedAt:"2025-01-24T02:21:33.471027",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3691",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T02:31:21.579804",CreatedAt:"2025-01-24T02:31:21.579804",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3692",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T04:13:07.847993",CreatedAt:"2025-01-24T04:13:07.847993",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3693",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T04:17:27.253551",CreatedAt:"2025-01-24T04:17:27.253551",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3694",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T06:19:17.443068",CreatedAt:"2025-01-24T06:19:17.443068",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3695",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T06:56:30.088125",CreatedAt:"2025-01-24T06:56:30.088125",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3696",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T07:27:28.37042",CreatedAt:"2025-01-24T07:27:28.37042",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3697",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T07:52:10.898056",CreatedAt:"2025-01-24T07:52:10.898056",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3698",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T07:57:58.083724",CreatedAt:"2025-01-24T07:57:58.083724",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3699",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T07:58:51.04519",CreatedAt:"2025-01-24T07:58:51.04519",CurrencyCode:"USD",Depth:Decimal(0),Description:"High-performance laptop",DescriptionLanguage:"EN",DimUnit:"",Email:"supplier-do.not.reply@sap.com",Height:Decimal(0),MeasureUnit:"EA",Name:"Example Product",NameLanguage:"EN",Price:Decimal(1200),ProductID:"HT-3700",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0),WeightUnit:"",Width:Decimal(0)},
                {Category:"Projectors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(23.1),Description:"720p, DLP Projector max. 8,45 Meter, 2D",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-do.not.reply@sap.com",Height:Decimal(23),MeasureUnit:"EA",Name:"Beam Breaker B-1",NameLanguage:"EN",Price:Decimal(469),ProductID:"HT-6100",SupplierID:"0100000046",SupplierName:"SAP",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.7),WeightUnit:"KG",Width:Decimal(30.4)},
                {Category:"Projectors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(23.1),Description:"1080p, DLP max.9,34 Meter, 2D-ready",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-frederik.christensen@dftc.dk",Height:Decimal(23),MeasureUnit:"EA",Name:"Beam Breaker B-2",NameLanguage:"EN",Price:Decimal(679),ProductID:"HT-6101",SupplierID:"0100000089",SupplierName:"Danish Fish Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2),WeightUnit:"KG",Width:Decimal(30.4)},
                {Category:"Projectors",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(23.1),Description:"1080p, DLP max. 12,3 Meter, 3D-ready",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jian.si@siwusha.cn",Height:Decimal(23),MeasureUnit:"EA",Name:"Beam Breaker B-3",NameLanguage:"EN",Price:Decimal(889),ProductID:"HT-6102",SupplierID:"0100000088",SupplierName:"Siwusha",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.5),WeightUnit:"KG",Width:Decimal(30.4)},
                {Category:"Portable Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(24),Description:"CD-RW, DVD+R/RW, DVD-R/RW, MPEG 2 (Video-DVD), MPEG 4, VCD, SVCD, DivX, Xvid",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-laura.campillo@saitc.ar",Height:Decimal(6),MeasureUnit:"EA",Name:"Play Movie",NameLanguage:"EN",Price:Decimal(130),ProductID:"HT-6110",SupplierID:"0100000087",SupplierName:"South American IT Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.4),WeightUnit:"KG",Width:Decimal(37)},
                {Category:"Portable Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(26),Description:"160 GB HDD, CD-RW, DVD+R/RW, DVD-R/RW, MPEG 2 (Video-DVD), MPEG 4, VCD, SVCD, DivX, Xvid",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-pawel-lewandoski@catf.pl",Height:Decimal(6.2),MeasureUnit:"EA",Name:"Record Movie",NameLanguage:"EN",Price:Decimal(288),ProductID:"HT-6111",SupplierID:"0100000086",SupplierName:"Chemia A Technicznie Fabryka",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.1),WeightUnit:"KG",Width:Decimal(38)},
                {Category:"MP3 Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6),Description:"64 GB USB Music-on-a-Stick",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sunita-kapoor@it-trade.in",Height:Decimal(1),MeasureUnit:"EA",Name:"ITelo MusicStick",NameLanguage:"EN",Price:Decimal(45),ProductID:"HT-6120",SupplierID:"0100000085",SupplierName:"Indian IT Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(134),WeightUnit:"G",Width:Decimal(1.5)},
                {Category:"MP3 Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(8),Description:"ITelo Jog-Mate 64 GB HDD and Color Display, can play movies",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-tamara.flaig@brl-ag.de",Height:Decimal(9.2),MeasureUnit:"EA",Name:"ITelo Jog-Mate",NameLanguage:"EN",Price:Decimal(63),ProductID:"HT-6121",SupplierID:"0100000084",SupplierName:"Bionic Research Lab",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(134),WeightUnit:"G",Width:Decimal(5.1)},
                {Category:"MP3 Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(8),Description:"MP3-Player with 40 GB HDD and Color Display, can play movies",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jefferson.parker@pico-bit.com",Height:Decimal(9.2),MeasureUnit:"EA",Name:"Power Pro Player 40",NameLanguage:"EN",Price:Decimal(167),ProductID:"HT-6122",SupplierID:"0100000083",SupplierName:"PicoBit",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(266),WeightUnit:"G",Width:Decimal(5.1)},
                {Category:"MP3 Players",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(6),Description:"MP3-Player with 80 GB SSD and Color Display, can play movies",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-dahoma.lawla@agadc.co.za",Height:Decimal(0.8),MeasureUnit:"EA",Name:"Power Pro Player 80",NameLanguage:"EN",Price:Decimal(299),ProductID:"HT-6123",SupplierID:"0100000082",SupplierName:"African Gold And Diamond Corporation",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(267),WeightUnit:"G",Width:Decimal(4)},
                {Category:"Flat Screen TVs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(22.1),Description:"32-inch, 1366x768 Pixel, 16:9, HDTV ready",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-romain.le_mason@verdo.fr",Height:Decimal(55),MeasureUnit:"EA",Name:"Flat Watch HD32",NameLanguage:"EN",Price:Decimal(1459),ProductID:"HT-6130",SupplierID:"0100000072",SupplierName:"Vente Et Réparation de Ordinateur",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.6),WeightUnit:"KG",Width:Decimal(78)},
                {Category:"Flat Screen TVs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(26),Description:"37-inch, 1366x768 Pixel, 16:9, HDTV ready",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-martha.calcagno@dpg.ar",Height:Decimal(61),MeasureUnit:"EA",Name:"Flat Watch HD37",NameLanguage:"EN",Price:Decimal(1199),ProductID:"HT-6131",SupplierID:"0100000073",SupplierName:"Developement Para O Governo",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(2.2),WeightUnit:"KG",Width:Decimal(99.1)},
                {Category:"Flat Screen TVs",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(23),Description:"41-inch, 1366x768 Pixel, 16:9, HDTV ready",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-beatriz.da_silva@brazil-tec.br",Height:Decimal(79.1),MeasureUnit:"EA",Name:"Flat Watch HD41",NameLanguage:"EN",Price:Decimal(899),ProductID:"HT-6132",SupplierID:"0100000074",SupplierName:"Brazil Technologies",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(1.8),WeightUnit:"KG",Width:Decimal(128)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(13),Description:"Our new multifunctional Handheld with phone function in copper",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-amelie.troyat@angere.fr",Height:Decimal(12.1),MeasureUnit:"EA",Name:"Copperberry",NameLanguage:"EN",Price:Decimal(549),ProductID:"HT-7000",SupplierID:"0100000078",SupplierName:"Angeré",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.5),WeightUnit:"KG",Width:Decimal(8.1)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(13),Description:"Our new multifunctional Handheld with phone function in silver",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jonathan.d.mason@baleda.com",Height:Decimal(12.1),MeasureUnit:"EA",Name:"Silverberry",NameLanguage:"EN",Price:Decimal(549),ProductID:"HT-7010",SupplierID:"0100000077",SupplierName:"Baleda",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.5),WeightUnit:"KG",Width:Decimal(8.1)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(13),Description:"Our new multifunctional Handheld with phone function in gold",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-lisa.felske@jologa.ch",Height:Decimal(12.1),MeasureUnit:"EA",Name:"Goldberry",NameLanguage:"EN",Price:Decimal(549),ProductID:"HT-7020",SupplierID:"0100000076",SupplierName:"Jologa",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.5),WeightUnit:"KG",Width:Decimal(8.1)},
                {Category:"PDAs & Organizers",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(13),Description:"Our new multifunctional Handheld with phone function in platinum",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-anthony.lebouef@crtu.ca",Height:Decimal(12.1),MeasureUnit:"EA",Name:"Platinberry",NameLanguage:"EN",Price:Decimal(549),ProductID:"HT-7030",SupplierID:"0100000075",SupplierName:"C.R.T.U.",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.5),WeightUnit:"KG",Width:Decimal(8.1)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(19),Description:"Notebook with 2,80 GHz dual core, 4 GB DDR3 SDRAM, 500 GB Hard Disc, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-joseph_gschwandtner@alp-systems.at",Height:Decimal(3.1),MeasureUnit:"EA",Name:"ITelO FlexTop I4000",NameLanguage:"EN",Price:Decimal(799),ProductID:"HT-8000",SupplierID:"0100000057",SupplierName:"Alpine Systems",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4),WeightUnit:"KG",Width:Decimal(31)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(20),Description:"Notebook with 2,80 GHz dual core, 8 GB DDR3 SDRAM, 500 GB Hard Disc, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-george_d_grant@newlinedesign.co.uk",Height:Decimal(3.4),MeasureUnit:"EA",Name:"ITelO FlexTop I6300c",NameLanguage:"EN",Price:Decimal(999),ProductID:"HT-8001",SupplierID:"0100000058",SupplierName:"New Line Design",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(4.2),WeightUnit:"KG",Width:Decimal(32)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(21),Description:"Notebook with 2,80 GHz quad core, 4 GB DDR3 SDRAM, 1000 GB Hard Disc, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sarah.schwind@hepa-tec.de",Height:Decimal(4.1),MeasureUnit:"EA",Name:"ITelO FlexTop I9100",NameLanguage:"EN",Price:Decimal(1199),ProductID:"HT-8002",SupplierID:"0100000059",SupplierName:"HEPA Tec",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.5),WeightUnit:"KG",Width:Decimal(38)},
                {Category:"Notebooks",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"Notebook with 2,80 GHz quad core, 8 GB DDR3 SDRAM, 1000 GB Hard Disc, Windows 8",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-theodor.monathy@anavideon.com",Height:Decimal(4.5),MeasureUnit:"EA",Name:"ITelO FlexTop I9800",NameLanguage:"EN",Price:Decimal(1388),ProductID:"HT-8003",SupplierID:"0100000060",SupplierName:"Anav Ideon",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.8),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"Button Clasp, Quality Material, 100% Leather, compatible with many smartphone models",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-yoshiko.kakuji@jateco.jp",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Smartphone Leather Case",NameLanguage:"EN",Price:Decimal(25),ProductID:"HT-9991",SupplierID:"0100000070",SupplierName:"JaTeCo",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.02),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Smartphones",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"7 inch 1280x800 HD display (216 ppi), Quad-core processor, 16 GB internal storage (actual formatted capacity will be less), 4325 mAh battery (Up to 8 hours of active use), white or black",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-sven.j@getraenke-janssen.de",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Smartphone Alpha",NameLanguage:"EN",Price:Decimal(599),ProductID:"HT-9992",SupplierID:"0100000069",SupplierName:"Getränkegroßhandel Janssen",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.75),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Tablets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"7 inch 1280x800 HD display (216 ppi), Quad-core processor, 16 GB internal storage, 4325 mAh battery (Up to 8 hours of active use)",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-c.alfaro@quimica-madrilenos.es",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Mini Tablet",NameLanguage:"EN",Price:Decimal(833),ProductID:"HT-9993",SupplierID:"0100000068",SupplierName:"Quimica Madrilenos",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.8),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Camcorders",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"1920x1080 Full HD, image stabilization reduces blur, 27x Optical / 32x Extended Zoom, wide angle Lens, 2.7"" wide LCD display",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-alexis.harper@flor-hc.com",Height:Decimal(27),MeasureUnit:"EA",Name:"Camcorder View",NameLanguage:"EN",Price:Decimal(1388),ProductID:"HT-9994",SupplierID:"0100000067",SupplierName:"Florida Holiday Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.8),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"Durable high quality plastic bump-sleeve, lightweight, protects from scratches, rubber coating, multiple colors available, Accurate design and cut-outs for your device, snap-on design",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-igor.tarassow@retc.ru",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Smartphone Cover",NameLanguage:"EN",Price:Decimal(15),ProductID:"HT-9995",SupplierID:"0100000066",SupplierName:"Russian Electronic Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.02),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(40),Description:"Stylish tablet pouch, protects from scratches, color: black",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-isabel.nemours@pateu.fr",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Tablet Pouch",NameLanguage:"EN",Price:Decimal(20),ProductID:"HT-9996",SupplierID:"0100000065",SupplierName:"Pateu",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.03),WeightUnit:"KG",Width:Decimal(25)},
                {Category:"Tablets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"6-Inch E Ink Screen, Access To e-book Store, Adjustable Font Styles and Sizes, Stores Up To 1,000 Books",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-miguel.luengo@compostela.ar",Height:Decimal(4.5),MeasureUnit:"EA",Name:"e-Book Reader ReadMe",NameLanguage:"EN",Price:Decimal(633),ProductID:"HT-9997",SupplierID:"0100000064",SupplierName:"Compostela",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.8),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Smartphones",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"5 Megapixel Camera, Wi-Fi 802.11 b/g/n, Bluetooth, GPS A-GPS support",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-johanna.esther@meliva.de",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Smartphone Beta",NameLanguage:"EN",Price:Decimal(699),ProductID:"HT-9998",SupplierID:"0100000063",SupplierName:"Meliva",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(0.75),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Tablets",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(31),Description:"10.1-inch Multitouch HD Screen (1280 x 800), 16GB Internal Memory, Wireless N Wi-Fi; Bluetooth, GPS Enabled, 1GHz Dual-Core Processor",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-jorgemontalban@motc.mx",Height:Decimal(4.5),MeasureUnit:"EA",Name:"Maxi Tablet",NameLanguage:"EN",Price:Decimal(749),ProductID:"HT-9999",SupplierID:"0100000062",SupplierName:"Mexican Oil Trading Company",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"PR",WeightMeasure:Decimal(3.8),WeightUnit:"KG",Width:Decimal(48)},
                {Category:"Computer System Accessories",ChangedAt:"2025-01-24T01:00:57",CreatedAt:"2025-01-24T01:00:57",CurrencyCode:"USD",Depth:Decimal(30),Description:"Flyer for our product palette",DescriptionLanguage:"EN",DimUnit:"CM",Email:"supplier-robert_brown@rb-entertainment.ca",Height:Decimal(3),MeasureUnit:"EA",Name:"Flyer",NameLanguage:"EN",Price:Decimal(0),ProductID:"PF-1000",SupplierID:"0100000061",SupplierName:"Robert Brown Entertainment",TaxTarifCode:Decimal(1),ToSalesOrderLineItems:{},ToSupplier:If(false,First(FirstN(BusinessPartnerSet,0))),TypeCode:"AD",WeightMeasure:Decimal(0.01),WeightUnit:"KG",Width:Decimal(46)}
                )
            */           
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
            Assert.Equal<object>("r*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s]", sqlTable.Type.ToStringWithDisplayNames());

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

            Assert.Equal<object>(
                "r*[Author`'Created By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser`'Checked Out To':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, " +
                "Picture:s], ComplianceAssetId`'Compliance Asset Id':s, Created:d, Editor`'Modified By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag`'Color" +
                " Tag':s, OData__DisplayName`Sensitivity:s, OData__ExtendedDescription`Description:s, OData__ip_UnifiedCompliancePolicyProperties`'Unified Compliance Policy Properties':s, Title:s, '{FilenameWithExtensi" +
                "on}'`'File name with extension':s, '{FullPath}'`'Full Path':s, '{Identifier}'`Identifier:s, '{IsCheckedOut}'`'Checked out':b, '{IsFolder}'`IsFolder:b, '{Link}'`'Link to item':s, '{ModerationComment}'`'" +
                "Comments associated with the content approval of this list item':s, '{ModerationStatus}'`'Content approval status':s, '{Name}'`Name:s, '{Path}'`'Folder path':s, '{Thumbnail}'`Thumbnail:![Large:s, " +
                "Medium:s, Small:s], '{TriggerWindowEndToken}'`'Trigger Window End Token':s, '{TriggerWindowStartToken}'`'Trigger Window Start Token':s, '{VersionNumber}'`'Version number':s]", spTable.Type.ToStringWithDisplayNames());

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
            Assert.NotNull(spTable.Relationships);
            Assert.Equal(3, spTable.Relationships.Count);
            Assert.Equal("Editor, Author, CheckoutUser", string.Join(", ", spTable.Relationships.Select(kvp => kvp.Key)));
            Assert.Equal("Editor, Author, CheckoutUser", string.Join(", ", spTable.Relationships.Select(kvp => kvp.Value.TargetEntity)));
            Assert.Equal("Editor#Claims-Claims, Author#Claims-Claims, CheckoutUser#Claims-Claims", string.Join(", ", spTable.Relationships.Select(kvp => string.Join("|", kvp.Value.ReferentialConstraints.Select(kvp2 => $"{kvp2.Key}-{kvp2.Value}")))));

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

            Assert.Equal("ID", string.Join("|", GetPrimaryKeyNames(spTable.RecordType)));
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

            Assert.Equal<object>(
                "r*[Author`'Created By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser`'Checked Out To':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, " +
                "Picture:s], ComplianceAssetId`'Compliance Asset Id':s, Created:d, Editor`'Modified By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag`'Color" +
                " Tag':s, OData__DisplayName`Sensitivity:s, OData__ExtendedDescription`Description:s, OData__ip_UnifiedCompliancePolicyProperties`'Unified Compliance Policy Properties':s, Title:s, '{FilenameWithExtensi" +
                "on}'`'File name with extension':s, '{FullPath}'`'Full Path':s, '{Identifier}'`Identifier:s, '{IsCheckedOut}'`'Checked out':b, '{IsFolder}'`IsFolder:b, '{Link}'`'Link to item':s, '{ModerationComment}'`'" +
                "Comments associated with the content approval of this list item':s, '{ModerationStatus}'`'Content approval status':s, '{Name}'`Name:s, '{Path}'`'Folder path':s, '{Thumbnail}'`Thumbnail:![Large:s, " +
                "Medium:s, Small:s], '{TriggerWindowEndToken}'`'Trigger Window End Token':s, '{TriggerWindowStartToken}'`'Trigger Window Start Token':s, '{VersionNumber}'`'Version number':s]", spTable.Type.ToStringWithDisplayNames());

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

        private static IEnumerable<string> GetPrimaryKeyNames(RecordType rt)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            rt.TryGetPrimaryKeyFieldName(out IEnumerable<string> primaryKeyNames);
#pragma warning restore CS0618 // Type or member is obsolete
            return primaryKeyNames;
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
            // Note 2: ~ notation denotes a relationship. Ex: fieldname`displayname:~externaltable:type
            Assert.Equal<object>(
                "r![AccountSource`'Account Source':l, BillingCity`'Billing City':s, BillingCountry`'Billing Country':s, BillingGeocodeAccuracy`'Billing Geocode Accuracy':l, BillingLatitude`'Billing Latitude':w, BillingLongitude`'Billing " +
                "Longitude':w, BillingPostalCode`'Billing Zip/Postal Code':s, BillingState`'Billing State/Province':s, BillingStreet`'Billing Street':s, CreatedById`'Created By ID'[User]:~User:s, CreatedDate`'Created Date':d, " +
                "Description`'Account Description':s, Id`'Account ID':s, Industry:l, IsDeleted`Deleted:b, Jigsaw`'Data.com Key':s, JigsawCompanyId`'Jigsaw Company ID':s, LastActivityDate`'Last Activity':D, LastModifiedById`'Last " +
                "Modified By ID'[User]:~User:s, LastModifiedDate`'Last Modified Date':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last Viewed Date':d, MasterRecordId`'Master Record ID'[Account]:~Account:s, " +
                "Name`'Account Name':s, NumberOfEmployees`Employees:w, OwnerId`'Owner ID'[User]:~User:s, ParentId`'Parent Account ID'[Account]:~Account:s, Phone`'Account Phone':s, PhotoUrl`'Photo URL':s, ShippingCity`'Shipping " +
                "City':s, ShippingCountry`'Shipping Country':s, ShippingGeocodeAccuracy`'Shipping Geocode Accuracy':l, ShippingLatitude`'Shipping Latitude':w, ShippingLongitude`'Shipping Longitude':w, ShippingPostalCode`'Shipping " +
                "Zip/Postal Code':s, ShippingState`'Shipping State/Province':s, ShippingStreet`'Shipping Street':s, SicDesc`'SIC Description':s, SystemModstamp`'System Modstamp':d, Type`'Account Type':l, Website:s]",
                ((CdpRecordType)sfTable.RecordType).ToStringWithDisplayNames());

            Assert.Equal("Account", sfTable.RecordType.TableSymbolName);

            RecordType rt = sfTable.RecordType;

            Assert.True(rt.TryGetUnderlyingFieldType("AccountSource", out FormulaType ft));

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

            // SF doesn't use x-ms-releationships extension
            Assert.Null(sfTable.Relationships);

            // needs Microsoft.PowerFx.Connectors.CdpExtensions
            // this call does not make any network call
            bool b = sfTable.RecordType.TryGetFieldExternalTableName("OwnerId", out string externalTableName, out string foreignKey);
            Assert.True(b);
            Assert.Equal("User", externalTableName);
            Assert.Null(foreignKey); // Always the case with SalesForce

            testConnector.SetResponseFromFile(@"Responses\SF GetSchema Users.json");
            b = sfTable.RecordType.TryGetFieldType("OwnerId", out FormulaType ownerIdType);

            Assert.True(b);
            CdpRecordType userTable = Assert.IsType<CdpRecordType>(ownerIdType);

            Assert.False((CdpRecordType)sfTable.RecordType is null);
            Assert.False(userTable is null);

            // External relationship table name
            Assert.Equal("User", userTable.TableSymbolName);

            Assert.Equal<object>(
                "r![AboutMe`'About Me':s, AccountId`'Account ID'[Account]:~Account:s, Alias:s, BadgeText`'User Photo badge text overlay':s, BannerPhotoUrl`'Url for banner photo':s, CallCenterId`'Call " +
                "Center ID':s, City:s, CommunityNickname`Nickname:s, CompanyName`'Company Name':s, ContactId`'Contact ID'[Contact]:~Contact:s, Country:s, CreatedById`'Created By ID'[User]:~User:s, CreatedDate`'Created " +
                "Date':d, DefaultGroupNotificationFrequency`'Default Notification Frequency when Joining Groups':l, DelegatedApproverId`'Delegated Approver ID':s, Department:s, DigestFrequency`'Chatter " +
                "Email Highlights Frequency':l, Division:s, Email:s, EmailEncodingKey`'Email Encoding':l, EmailPreferencesAutoBcc`AutoBcc:b, EmailPreferencesAutoBccStayInTouch`AutoBccStayInTouch:b, " +
                "EmailPreferencesStayInTouchReminder`StayInTouchReminder:b, EmployeeNumber`'Employee Number':s, Extension:s, Fax:s, FederationIdentifier`'SAML Federation ID':s, FirstName`'First Name':s, " +
                "ForecastEnabled`'Allow Forecasting':b, FullPhotoUrl`'Url for full-sized Photo':s, GeocodeAccuracy`'Geocode Accuracy':l, Id`'User ID':s, IsActive`Active:b, IsExtIndicatorVisible`'Show " +
                "external indicator':b, IsProfilePhotoActive`'Has Profile Photo':b, LanguageLocaleKey`Language:l, LastLoginDate`'Last Login':d, LastModifiedById`'Last Modified By ID'[User]:~User:s, " +
                "LastModifiedDate`'Last Modified Date':d, LastName`'Last Name':s, LastPasswordChangeDate`'Last Password Change or Reset':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last " +
                "Viewed Date':d, Latitude:w, LocaleSidKey`Locale:l, Longitude:w, ManagerId`'Manager ID'[User]:~User:s, MediumBannerPhotoUrl`'Url for Android banner photo':s, MediumPhotoUrl`'Url for " +
                "medium profile photo':s, MiddleName`'Middle Name':s, MobilePhone`Mobile:s, Name`'Full Name':s, OfflinePdaTrialExpirationDate`'Sales Anywhere Trial Expiration Date':d, OfflineTrialExpirationDate`'Offlin" +
                "e Edition Trial Expiration Date':d, OutOfOfficeMessage`'Out of office message':s, Phone:s, PostalCode`'Zip/Postal Code':s, ProfileId`'Profile ID'[Profile]:~Profile:s, ReceivesAdminInfoEmails`'Admin " +
                "Info Emails':b, ReceivesInfoEmails`'Info Emails':b, SenderEmail`'Email Sender Address':s, SenderName`'Email Sender Name':s, Signature`'Email Signature':s, SmallBannerPhotoUrl`'Url for " +
                "IOS banner photo':s, SmallPhotoUrl`Photo:s, State`'State/Province':s, StayInTouchNote`'Stay-in-Touch Email Note':s, StayInTouchSignature`'Stay-in-Touch Email Signature':s, StayInTouchSubject`'Stay-in-T" +
                "ouch Email Subject':s, Street:s, Suffix:s, SystemModstamp`'System Modstamp':d, TimeZoneSidKey`'Time Zone':l, Title:s, UserPermissionsAvantgoUser`'AvantGo User':b, UserPermissionsCallCenterAutoLogin`'Au" +
                "to-login To Call Center':b, UserPermissionsInteractionUser`'Flow User':b, UserPermissionsKnowledgeUser`'Knowledge User':b, UserPermissionsLiveAgentUser`'Chat User':b, UserPermissionsMarketingUser`'Mark" +
                "eting User':b, UserPermissionsMobileUser`'Apex Mobile User':b, UserPermissionsOfflineUser`'Offline User':b, UserPermissionsSFContentUser`'Salesforce CRM Content User':b, UserPermissionsSupportUser`'Ser" +
                "vice Cloud User':b, UserPreferencesActivityRemindersPopup`ActivityRemindersPopup:b, UserPreferencesApexPagesDeveloperMode`ApexPagesDeveloperMode:b, UserPreferencesCacheDiagnostics`CacheDiagnostics:b, " +
                "UserPreferencesCreateLEXAppsWTShown`CreateLEXAppsWTShown:b, UserPreferencesDisCommentAfterLikeEmail`DisCommentAfterLikeEmail:b, UserPreferencesDisMentionsCommentEmail`DisMentionsCommentEmail:b, " +
                "UserPreferencesDisProfPostCommentEmail`DisProfPostCommentEmail:b, UserPreferencesDisableAllFeedsEmail`DisableAllFeedsEmail:b, UserPreferencesDisableBookmarkEmail`DisableBookmarkEmail:b, " +
                "UserPreferencesDisableChangeCommentEmail`DisableChangeCommentEmail:b, UserPreferencesDisableEndorsementEmail`DisableEndorsementEmail:b, UserPreferencesDisableFileShareNotificationsForApi`DisableFileSha" +
                "reNotificationsForApi:b, UserPreferencesDisableFollowersEmail`DisableFollowersEmail:b, UserPreferencesDisableLaterCommentEmail`DisableLaterCommentEmail:b, UserPreferencesDisableLikeEmail`DisableLikeEma" +
                "il:b, UserPreferencesDisableMentionsPostEmail`DisableMentionsPostEmail:b, UserPreferencesDisableMessageEmail`DisableMessageEmail:b, UserPreferencesDisableProfilePostEmail`DisableProfilePostEmail:b, " +
                "UserPreferencesDisableSharePostEmail`DisableSharePostEmail:b, UserPreferencesEnableAutoSubForFeeds`EnableAutoSubForFeeds:b, UserPreferencesEventRemindersCheckboxDefault`EventRemindersCheckboxDefault:b," +
                " UserPreferencesExcludeMailAppAttachments`ExcludeMailAppAttachments:b, UserPreferencesFavoritesShowTopFavorites`FavoritesShowTopFavorites:b, UserPreferencesFavoritesWTShown`FavoritesWTShown:b, " +
                "UserPreferencesGlobalNavBarWTShown`GlobalNavBarWTShown:b, UserPreferencesGlobalNavGridMenuWTShown`GlobalNavGridMenuWTShown:b, UserPreferencesHideBiggerPhotoCallout`HideBiggerPhotoCallout:b, " +
                "UserPreferencesHideCSNDesktopTask`HideCSNDesktopTask:b, UserPreferencesHideCSNGetChatterMobileTask`HideCSNGetChatterMobileTask:b, UserPreferencesHideChatterOnboardingSplash`HideChatterOnboardingSplash:" +
                "b, UserPreferencesHideEndUserOnboardingAssistantModal`HideEndUserOnboardingAssistantModal:b, UserPreferencesHideLightningMigrationModal`HideLightningMigrationModal:b, UserPreferencesHideS1BrowserUI`Hid" +
                "eS1BrowserUI:b, UserPreferencesHideSecondChatterOnboardingSplash`HideSecondChatterOnboardingSplash:b, UserPreferencesHideSfxWelcomeMat`HideSfxWelcomeMat:b, UserPreferencesLightningExperiencePreferred`L" +
                "ightningExperiencePreferred:b, UserPreferencesPathAssistantCollapsed`PathAssistantCollapsed:b, UserPreferencesPreviewLightning`PreviewLightning:b, UserPreferencesRecordHomeReservedWTShown`RecordHomeRes" +
                "ervedWTShown:b, UserPreferencesRecordHomeSectionCollapseWTShown`RecordHomeSectionCollapseWTShown:b, UserPreferencesReminderSoundOff`ReminderSoundOff:b, UserPreferencesShowCityToExternalUsers`ShowCityTo" +
                "ExternalUsers:b, UserPreferencesShowCityToGuestUsers`ShowCityToGuestUsers:b, UserPreferencesShowCountryToExternalUsers`ShowCountryToExternalUsers:b, UserPreferencesShowCountryToGuestUsers`ShowCountryTo" +
                "GuestUsers:b, UserPreferencesShowEmailToExternalUsers`ShowEmailToExternalUsers:b, UserPreferencesShowEmailToGuestUsers`ShowEmailToGuestUsers:b, UserPreferencesShowFaxToExternalUsers`ShowFaxToExternalUs" +
                "ers:b, UserPreferencesShowFaxToGuestUsers`ShowFaxToGuestUsers:b, UserPreferencesShowManagerToExternalUsers`ShowManagerToExternalUsers:b, UserPreferencesShowManagerToGuestUsers`ShowManagerToGuestUsers:b" +
                ", UserPreferencesShowMobilePhoneToExternalUsers`ShowMobilePhoneToExternalUsers:b, UserPreferencesShowMobilePhoneToGuestUsers`ShowMobilePhoneToGuestUsers:b, UserPreferencesShowPostalCodeToExternalUsers`" +
                "ShowPostalCodeToExternalUsers:b, UserPreferencesShowPostalCodeToGuestUsers`ShowPostalCodeToGuestUsers:b, UserPreferencesShowProfilePicToGuestUsers`ShowProfilePicToGuestUsers:b, UserPreferencesShowState" +
                "ToExternalUsers`ShowStateToExternalUsers:b, UserPreferencesShowStateToGuestUsers`ShowStateToGuestUsers:b, UserPreferencesShowStreetAddressToExternalUsers`ShowStreetAddressToExternalUsers:b, " +
                "UserPreferencesShowStreetAddressToGuestUsers`ShowStreetAddressToGuestUsers:b, UserPreferencesShowTitleToExternalUsers`ShowTitleToExternalUsers:b, UserPreferencesShowTitleToGuestUsers`ShowTitleToGuestUs" +
                "ers:b, UserPreferencesShowWorkPhoneToExternalUsers`ShowWorkPhoneToExternalUsers:b, UserPreferencesShowWorkPhoneToGuestUsers`ShowWorkPhoneToGuestUsers:b, UserPreferencesSortFeedByComment`SortFeedByComme" +
                "nt:b, UserPreferencesTaskRemindersCheckboxDefault`TaskRemindersCheckboxDefault:b, UserRoleId`'Role ID'[UserRole]:~UserRole:s, UserType`'User Type':l, Username:s]", userTable.ToStringWithDisplayNames());

            // Missing field
            b = sfTable.RecordType.TryGetFieldType("XYZ", out FormulaType xyzType);
            Assert.False(b);
            Assert.Null(xyzType);

            // Field with no relationship
            b = sfTable.RecordType.TryGetFieldType("BillingCountry", out FormulaType billingCountryType);
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

            Assert.Equal("Id", string.Join("|", GetPrimaryKeyNames(sfTable.RecordType)));
            Assert.Equal("Id", string.Join("|", GetPrimaryKeyNames(userTable)));
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

            Assert.Equal<object>(
                "r*[AccountSource`'Account Source':l, BillingCity`'Billing City':s, BillingCountry`'Billing Country':s, BillingGeocodeAccuracy`'Billing Geocode Accuracy':l, BillingLatitude`'Billing Latitude':w, BillingLongitude`'Billing " +
                "Longitude':w, BillingPostalCode`'Billing Zip/Postal Code':s, BillingState`'Billing State/Province':s, BillingStreet`'Billing Street':s, CreatedById`'Created By ID':~User:s, CreatedDate`'Created Date':d, " +
                "Description`'Account Description':s, Id`'Account ID':s, Industry:l, IsDeleted`Deleted:b, Jigsaw`'Data.com Key':s, JigsawCompanyId`'Jigsaw Company ID':s, LastActivityDate`'Last Activity':D, LastModifiedById`'Last " +
                "Modified By ID':~User:s, LastModifiedDate`'Last Modified Date':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last Viewed Date':d, MasterRecordId`'Master Record ID':~Account:s, Name`'Account " +
                "Name':s, NumberOfEmployees`Employees:w, OwnerId`'Owner ID':~User:s, ParentId`'Parent Account ID':~Account:s, Phone`'Account Phone':s, PhotoUrl`'Photo URL':s, ShippingCity`'Shipping City':s, ShippingCountry`'Shipping " +
                "Country':s, ShippingGeocodeAccuracy`'Shipping Geocode Accuracy':l, ShippingLatitude`'Shipping Latitude':w, ShippingLongitude`'Shipping Longitude':w, ShippingPostalCode`'Shipping Zip/Postal Code':s, ShippingState`'Shipping " +
                "State/Province':s, ShippingStreet`'Shipping Street':s, SicDesc`'SIC Description':s, SystemModstamp`'System Modstamp':d, Type`'Account Type':l, Website:s]", sfTable.Type.ToStringWithDisplayNames());

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
                "r![active:b, alias:s, created_at:d, custom_role_id:w, details:s, email:s, external_id:s, id:w, last_login_at:d, locale:s, locale_id:w, moderator:b, name:s, notes:s, only_private_comments:b, organization_id:w, " +
                "phone:s, photo:s, restricted_agent:b, role:s, shared:b, shared_agent:b, signature:s, suspended:b, tags:s, ticket_restriction:s, time_zone:s, updated_at:d, url:s, user_fields:s, verified:b]", ((CdpRecordType)zdTable.RecordType).ToStringWithDisplayNames());

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

        [Fact]
        public async Task ZD_CdpTabular_GetTables2()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "ca06d34f4b684e38b7cf4c0f517a7e99";
            string uriPrefix = $"/apim/zendesk/{connectionId}";
            string jwt = "eyJ0eXA...";
            using var client = new PowerPlatformConnectorClient("4d4a8e81-17a4-4a92-9bfe-8d12e607fb7f.08.common.tip1.azure-apihub.net", "4d4a8e81-17a4-4a92-9bfe-8d12e607fb7f", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

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

            CdpTable connectorTable = tables.First(t => t.DisplayName == "Tickets");
            Assert.Equal("tickets", connectorTable.TableName);
            Assert.False(connectorTable.IsInitialized);

            testConnector.SetResponseFromFile(@"Responses\ZD Tickets GetSchema.json");
            await connectorTable.InitAsync(client, uriPrefix, CancellationToken.None, logger);
            Assert.True(connectorTable.IsInitialized);

            CdpTableValue zdTable = connectorTable.GetTableValue();
            Assert.True(zdTable._tabularService.IsInitialized);
            Assert.True(zdTable.IsDelegable);

            Assert.Equal(
                "r![assignee_id:w, brand_id:w, collaborator_ids:s, created_at:d, custom_fields:s, description:s, due_at:d, external_id:s, followup_ids:s, forum_topic_id:w, group_id:w, has_incidents:b, " +
                "id:w, organization_id:w, priority:l, problem_id:w, raw_subject:s, recipient:s, requester_id:w, satisfaction_rating:s, sharing_agreement_ids:s, status:l, subject:s, submitter_id:w, " +
                "tags:s, ticket_form_id:w, type:l, updated_at:d, url:s, via:s]", ((CdpRecordType)zdTable.RecordType).ToStringWithDisplayNames());

            SymbolValues symbolValues = new SymbolValues().Add("Tickets", zdTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddService<ConnectorLogger>(logger);

            // Expression with tabular connector
            string expr = @"First(Tickets).priority";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\ZD Tickets GetRows.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc);

            OptionSetValue priority = Assert.IsType<OptionSetValue>(result);
            Assert.Equal("normal", priority.Option);
            Assert.Equal("Normal", priority.DisplayName);

            Assert.NotNull(connectorTable.OptionSets);
            Assert.Equal("priority (tickets), status (tickets), type (tickets)", string.Join(", ", connectorTable.OptionSets.Select(os => os.EntityName.Value).OrderBy(x => x)));

            Assert.Equal("id", string.Join("|", GetPrimaryKeyNames(zdTable.RecordType)));
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
