// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Tabular;
using Microsoft.PowerFx.Core.Tests;
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

            ConnectorDataSource cds = new ConnectorDataSource("pfxdev-sql.database.windows.net,connectortest");

            testConnector.SetResponseFromFile(@"Responses\SQL GetDatasetsMetadata.json");
            await cds.GetDatasetsMetadataAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);

            Assert.NotNull(cds.DatasetMetadata);
            Assert.Null(cds.DatasetMetadata.Blob);

            Assert.Equal("{server},{database}", cds.DatasetMetadata.DatasetFormat);
            Assert.NotNull(cds.DatasetMetadata.Tabular);
            Assert.Equal("dataset", cds.DatasetMetadata.Tabular.DisplayName);
            Assert.Equal("mru", cds.DatasetMetadata.Tabular.Source);
            Assert.Equal("Table", cds.DatasetMetadata.Tabular.TableDisplayName);
            Assert.Equal("Tables", cds.DatasetMetadata.Tabular.TablePluralName);
            Assert.Equal("single", cds.DatasetMetadata.Tabular.UrlEncoding);
            Assert.NotNull(cds.DatasetMetadata.Parameters);
            Assert.Equal(2, cds.DatasetMetadata.Parameters.Count);

            Assert.Equal("Server name.", cds.DatasetMetadata.Parameters[0].Description);
            Assert.Equal("server", cds.DatasetMetadata.Parameters[0].Name);
            Assert.True(cds.DatasetMetadata.Parameters[0].Required);
            Assert.Equal("string", cds.DatasetMetadata.Parameters[0].Type);
            Assert.Equal("double", cds.DatasetMetadata.Parameters[0].UrlEncoding);
            Assert.Null(cds.DatasetMetadata.Parameters[0].XMsDynamicValues);
            Assert.Equal("Server name", cds.DatasetMetadata.Parameters[0].XMsSummary);

            Assert.Equal("Database name.", cds.DatasetMetadata.Parameters[1].Description);
            Assert.Equal("database", cds.DatasetMetadata.Parameters[1].Name);
            Assert.True(cds.DatasetMetadata.Parameters[1].Required);
            Assert.Equal("string", cds.DatasetMetadata.Parameters[1].Type);
            Assert.Equal("double", cds.DatasetMetadata.Parameters[1].UrlEncoding);
            Assert.NotNull(cds.DatasetMetadata.Parameters[1].XMsDynamicValues);
            Assert.Equal("/v2/databases?server={server}", cds.DatasetMetadata.Parameters[1].XMsDynamicValues.Path);
            Assert.Equal("value", cds.DatasetMetadata.Parameters[1].XMsDynamicValues.ValueCollection);
            Assert.Equal("Name", cds.DatasetMetadata.Parameters[1].XMsDynamicValues.ValuePath);
            Assert.Equal("DisplayName", cds.DatasetMetadata.Parameters[1].XMsDynamicValues.ValueTitle);
            Assert.Equal("Database name", cds.DatasetMetadata.Parameters[1].XMsSummary);

            testConnector.SetResponseFromFile(@"Responses\SQL GetTables.json");
            IEnumerable<ConnectorTable> tables = await cds.GetTablesAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);

            Assert.NotNull(tables);
            Assert.Equal(4, tables.Count());
            Assert.Equal("[dbo].[Customers],[dbo].[Orders],[dbo].[Products],[sys].[database_firewall_rules]", string.Join(",", tables.Select(t => t.TableName)));
            Assert.Equal("Customers,Orders,Products,sys.database_firewall_rules", string.Join(",", tables.Select(t => t.DisplayName)));

            ConnectorTable connectorTable = tables.First(t => t.DisplayName == "Customers");

            Assert.False(connectorTable.IsInitialized);
            Assert.Equal("Customers", connectorTable.DisplayName);

            testConnector.SetResponseFromFile(@"Responses\SQL Server Load Customers DB.json");
            await connectorTable.InitAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(connectorTable.IsInitialized);

            ConnectorTableValue sqlTable = connectorTable.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegable);
            Assert.Equal("*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s]", sqlTable.Type._type.ToString());

#pragma warning disable CS0618 // Type or member is obsolete

            // Enable IR rewritter to auto-inject ServiceProvider where needed
            engine.EnableTabularConnectors();

#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("Customers", sqlTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues)
                                    .AddService<ConnectorLogger>(logger)
                                    .AddService<HttpClient>(client);

            // Expression with tabular connector
            string expr = @"First(Customers).Address";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Confirm that InjectServiceProviderFunction has properly been added
            string ir = new Regex("RuntimeValues_[0-9]+").Replace(check.PrintIR(), "RuntimeValues_XXX");
            Assert.Equal("FieldAccess(First:![Address:s, Country:s, CustomerId:w, Name:s, Phone:s](InjectServiceProviderFunction:*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s](ResolvedObject('Customers:RuntimeValues_XXX'))), Address)", ir);

            // Use tabular connector. Internally we'll call ConnectorTableValueWithServiceProvider.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);

            StringValue address = Assert.IsType<StringValue>(result);
            Assert.Equal("Juigné", address.Value);

            // Rows are not cached here as the cache is stored in ConnectorTableValueWithServiceProvider which is created by InjectServiceProviderFunction, itself added during Engine.Check
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            result = await engine.EvalAsync("Last(Customers).Phone", CancellationToken.None, runtimeConfig: rc).ConfigureAwait(false);
            StringValue phone = Assert.IsType<StringValue>(result);
            Assert.Equal("+1-425-705-0000", phone.Value);
        }

        [Fact]
        public async Task SQL_CdpTabular()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
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
            ConnectorTable tabularService = new ConnectorTable("pfxdev-sql.database.windows.net,connectortest", "Customers");

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Customers", tabularService.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SQL GetDatasetsMetadata.json", @"Responses\SQL Server Load Customers DB.json");
            await tabularService.InitAsync(client, $"/apim/sql/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);

            ConnectorTableValue sqlTable = tabularService.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegable);
            Assert.Equal("*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s]", sqlTable.Type._type.ToString());

#pragma warning disable CS0618 // Type or member is obsolete

            // Enable IR rewritter to auto-inject ServiceProvider where needed
            engine.EnableTabularConnectors();

#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("Customers", sqlTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues)
                                    .AddService<ConnectorLogger>(logger)
                                    .AddService<HttpClient>(client);

            // Expression with tabular connector
            string expr = @"First(Customers).Address";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Confirm that InjectServiceProviderFunction has properly been added
            string ir = new Regex("RuntimeValues_[0-9]+").Replace(check.PrintIR(), "RuntimeValues_XXX");
            Assert.Equal("FieldAccess(First:![Address:s, Country:s, CustomerId:w, Name:s, Phone:s](InjectServiceProviderFunction:*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s](ResolvedObject('Customers:RuntimeValues_XXX'))), Address)", ir);

            // Use tabular connector. Internally we'll call ConnectorTableValueWithServiceProvider.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);

            StringValue address = Assert.IsType<StringValue>(result);
            Assert.Equal("Juigné", address.Value);

            // Rows are not cached here as the cache is stored in ConnectorTableValueWithServiceProvider which is created by InjectServiceProviderFunction, itself added during Engine.Check
            testConnector.SetResponseFromFile(@"Responses\SQL Server Get First Customers.json");
            result = await engine.EvalAsync("Last(Customers).Phone", CancellationToken.None, runtimeConfig: rc).ConfigureAwait(false);
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
            ConnectorDataSource cds = new ConnectorDataSource("https://microsofteur.sharepoint.com/teams/pfxtest");

            testConnector.SetResponseFromFiles(@"Responses\SP GetDatasetsMetadata.json", @"Responses\SP GetTables.json");
            IEnumerable<ConnectorTable> tables = await cds.GetTablesAsync(client, $"/apim/sharepointonline/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);

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

            ConnectorTable connectorTable = tables.First(t => t.DisplayName == "Documents");

            Assert.False(connectorTable.IsInitialized);
            Assert.Equal("4bd37916-0026-4726-94e8-5a0cbc8e476a", connectorTable.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SP GetTable.json");
            await connectorTable.InitAsync(client, $"/apim/sharepointonline/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(connectorTable.IsInitialized);

            ConnectorTableValue spTable = connectorTable.GetTableValue();
            Assert.True(spTable._tabularService.IsInitialized);
            Assert.True(spTable.IsDelegable);

            Assert.Equal(
                "*[Author`'Created By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser`'Checked Out To':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ComplianceAssetId`'Compliance " +
                "Asset Id':s, Created:d, Editor`'Modified By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag`'Color Tag':s, OData__DisplayName`Sensitivity:s, " +
                "OData__ExtendedDescription`Description:s, OData__ip_UnifiedCompliancePolicyProperties`'Unified Compliance Policy Properties':s, Title:s, '{FilenameWithExtension}'`'File name with extension':s, '{FullPath}'`'Full " +
                "Path':s, '{Identifier}'`Identifier:s, '{IsCheckedOut}'`'Checked out':b, '{IsFolder}'`IsFolder:b, '{Link}'`'Link to item':s, '{ModerationComment}'`'Comments associated with the content approval of this " +
                "list item':s, '{ModerationStatus}'`'Content approval status':s, '{Name}'`Name:s, '{Path}'`'Folder path':s, '{Thumbnail}'`Thumbnail:![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}'`'Trigger Window " +
                "End Token':s, '{TriggerWindowStartToken}'`'Trigger Window Start Token':s, '{VersionNumber}'`'Version number':s]", spTable.Type.ToStringWithDisplayNames());

#pragma warning disable CS0618 // Type or member is obsolete

            // Enable IR rewritter to auto-inject ServiceProvider where needed
            engine.EnableTabularConnectors();

#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("Documents", spTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues)
                                    .AddService<ConnectorLogger>(logger)
                                    .AddService<HttpClient>(client);

            // Expression with tabular connector
            string expr = @"First(Documents).Name";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Confirm that InjectServiceProviderFunction has properly been added
            string ir = new Regex("RuntimeValues_[0-9]+").Replace(check.PrintIR(), "RuntimeValues_XXX");
            Assert.Equal(
                "FieldAccess(First:![Author:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], " +
                "ComplianceAssetId:s, Created:d, Editor:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag:s, OData__DisplayName:s, " +
                "OData__ExtendedDescription:s, OData__ip_UnifiedCompliancePolicyProperties:s, Title:s, '{FilenameWithExtension}':s, '{FullPath}':s, '{Identifier}':s, '{IsCheckedOut}':b, '{IsFolder}':b, " +
                "'{Link}':s, '{ModerationComment}':s, '{ModerationStatus}':s, '{Name}':s, '{Path}':s, '{Thumbnail}':![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}':s, '{TriggerWindowStartToken}':s, " +
                "'{VersionNumber}':s](InjectServiceProviderFunction:*[Author:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser:![Claims:s, Department:s, DisplayName:s, " +
                "Email:s, JobTitle:s, Picture:s], ComplianceAssetId:s, Created:d, Editor:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag:s, " +
                "OData__DisplayName:s, OData__ExtendedDescription:s, OData__ip_UnifiedCompliancePolicyProperties:s, Title:s, '{FilenameWithExtension}':s, '{FullPath}':s, '{Identifier}':s, '{IsCheckedOut}':b, " +
                "'{IsFolder}':b, '{Link}':s, '{ModerationComment}':s, '{ModerationStatus}':s, '{Name}':s, '{Path}':s, '{Thumbnail}':![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}':s, " +
                "'{TriggerWindowStartToken}':s, '{VersionNumber}':s](ResolvedObject('Documents:RuntimeValues_XXX'))), {Name})", ir);

            // Use tabular connector. Internally we'll call ConnectorTableValueWithServiceProvider.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SP GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);

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

            ConnectorTable tabularService = new ConnectorTable("https://microsofteur.sharepoint.com/teams/pfxtest", "Documents");

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Documents", tabularService.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SP GetDatasetsMetadata.json", @"Responses\SP GetTable.json");
            await tabularService.InitAsync(client, $"/apim/sharepointonline/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);

            ConnectorTableValue spTable = tabularService.GetTableValue();
            Assert.True(spTable._tabularService.IsInitialized);
            Assert.True(spTable.IsDelegable);

            Assert.Equal(
                "*[Author`'Created By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser`'Checked Out To':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ComplianceAssetId`'Compliance " +
                "Asset Id':s, Created:d, Editor`'Modified By':![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag`'Color Tag':s, OData__DisplayName`Sensitivity:s, " +
                "OData__ExtendedDescription`Description:s, OData__ip_UnifiedCompliancePolicyProperties`'Unified Compliance Policy Properties':s, Title:s, '{FilenameWithExtension}'`'File name with extension':s, '{FullPath}'`'Full " +
                "Path':s, '{Identifier}'`Identifier:s, '{IsCheckedOut}'`'Checked out':b, '{IsFolder}'`IsFolder:b, '{Link}'`'Link to item':s, '{ModerationComment}'`'Comments associated with the content approval of this " +
                "list item':s, '{ModerationStatus}'`'Content approval status':s, '{Name}'`Name:s, '{Path}'`'Folder path':s, '{Thumbnail}'`Thumbnail:![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}'`'Trigger Window " +
                "End Token':s, '{TriggerWindowStartToken}'`'Trigger Window Start Token':s, '{VersionNumber}'`'Version number':s]", spTable.Type.ToStringWithDisplayNames());

#pragma warning disable CS0618 // Type or member is obsolete

            // Enable IR rewritter to auto-inject ServiceProvider where needed
            engine.EnableTabularConnectors();

#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("Documents", spTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues)
                                    .AddService<ConnectorLogger>(logger)
                                    .AddService<HttpClient>(client);

            // Expression with tabular connector
            string expr = @"First(Documents).Name";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Confirm that InjectServiceProviderFunction has properly been added
            string ir = new Regex("RuntimeValues_[0-9]+").Replace(check.PrintIR(), "RuntimeValues_XXX");
            Assert.Equal(
                "FieldAccess(First:![Author:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], " +
                "ComplianceAssetId:s, Created:d, Editor:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag:s, OData__DisplayName:s, " +
                "OData__ExtendedDescription:s, OData__ip_UnifiedCompliancePolicyProperties:s, Title:s, '{FilenameWithExtension}':s, '{FullPath}':s, '{Identifier}':s, '{IsCheckedOut}':b, '{IsFolder}':b, " +
                "'{Link}':s, '{ModerationComment}':s, '{ModerationStatus}':s, '{Name}':s, '{Path}':s, '{Thumbnail}':![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}':s, '{TriggerWindowStartToken}':s, " +
                "'{VersionNumber}':s](InjectServiceProviderFunction:*[Author:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], CheckoutUser:![Claims:s, Department:s, DisplayName:s, " +
                "Email:s, JobTitle:s, Picture:s], ComplianceAssetId:s, Created:d, Editor:![Claims:s, Department:s, DisplayName:s, Email:s, JobTitle:s, Picture:s], ID:w, Modified:d, OData__ColorTag:s, " +
                "OData__DisplayName:s, OData__ExtendedDescription:s, OData__ip_UnifiedCompliancePolicyProperties:s, Title:s, '{FilenameWithExtension}':s, '{FullPath}':s, '{Identifier}':s, '{IsCheckedOut}':b, " +
                "'{IsFolder}':b, '{Link}':s, '{ModerationComment}':s, '{ModerationStatus}':s, '{Name}':s, '{Path}':s, '{Thumbnail}':![Large:s, Medium:s, Small:s], '{TriggerWindowEndToken}':s, " +
                "'{TriggerWindowStartToken}':s, '{VersionNumber}':s](ResolvedObject('Documents:RuntimeValues_XXX'))), {Name})", ir);

            // Use tabular connector. Internally we'll call ConnectorTableValueWithServiceProvider.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SP GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);

            StringValue docName = Assert.IsType<StringValue>(result);
            Assert.Equal("Document1", docName.Value);
        }

        [Fact]
        public async Task SF_CdpTabular_GetTables()
        {
            using var testConnector = new LoggingTestServer(null /* no swagger */, _output);
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            ConsoleLogger logger = new ConsoleLogger(_output);
            using var httpClient = new HttpClient(testConnector);
            string connectionId = "e88a5bf3321547e0965695384a2fe57d";
            string jwt = "eyJ0eXAiOiJKV1Qi...";
            using var client = new PowerPlatformConnectorClient("tip2-001.azure-apihub.net", "8d626c93-244c-eaa5-b3d8-bbffbb04b626", connectionId, () => jwt, httpClient) { SessionId = "8e67ebdc-d402-455a-b33a-304820832383" };

            ConnectorDataSource cds = new ConnectorDataSource("default");

            testConnector.SetResponseFromFile(@"Responses\SF GetDatasetsMetadata.json");
            await cds.GetDatasetsMetadataAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);

            Assert.NotNull(cds.DatasetMetadata);
            Assert.Null(cds.DatasetMetadata.Blob);
            Assert.Null(cds.DatasetMetadata.DatasetFormat);
            Assert.Null(cds.DatasetMetadata.Parameters);

            Assert.NotNull(cds.DatasetMetadata.Tabular);
            Assert.Equal("dataset", cds.DatasetMetadata.Tabular.DisplayName);
            Assert.Equal("singleton", cds.DatasetMetadata.Tabular.Source);
            Assert.Equal("Table", cds.DatasetMetadata.Tabular.TableDisplayName);
            Assert.Equal("Tables", cds.DatasetMetadata.Tabular.TablePluralName);
            Assert.Equal("double", cds.DatasetMetadata.Tabular.UrlEncoding);

            testConnector.SetResponseFromFile(@"Responses\SF GetTables.json");
            IEnumerable<ConnectorTable> tables = await cds.GetTablesAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);

            Assert.NotNull(tables);
            Assert.Equal(569, tables.Count());

            ConnectorTable connectorTable = tables.First(t => t.DisplayName == "Accounts");
            Assert.Equal("Account", connectorTable.TableName);
            Assert.False(connectorTable.IsInitialized);

            testConnector.SetResponseFromFile(@"Responses\SF GetSchema.json");
            await connectorTable.InitAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(connectorTable.IsInitialized);

            ConnectorTableValue sfTable = connectorTable.GetTableValue();
            Assert.True(sfTable._tabularService.IsInitialized);
            Assert.True(sfTable.IsDelegable);

            Assert.Equal(
                "*[AccountSource`'Account Source':s, BillingCity`'Billing City':s, BillingCountry`'Billing Country':s, BillingGeocodeAccuracy`'Billing Geocode Accuracy':s, BillingLatitude`'Billing Latitude':w, BillingLongitude`'Billing " +
                "Longitude':w, BillingPostalCode`'Billing Zip/Postal Code':s, BillingState`'Billing State/Province':s, BillingStreet`'Billing Street':s, CreatedById`'Created By ID':s, CreatedDate`'Created Date':d, Description`'Account " +
                "Description':s, Id`'Account ID':s, Industry:s, IsDeleted`Deleted:b, Jigsaw`'Data.com Key':s, JigsawCompanyId`'Jigsaw Company ID':s, LastActivityDate`'Last Activity':D, LastModifiedById`'Last Modified By " +
                "ID':s, LastModifiedDate`'Last Modified Date':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last Viewed Date':d, MasterRecordId`'Master Record ID':s, Name`'Account Name':s, NumberOfEmployees`Employees:w, " +
                "OwnerId`'Owner ID':s, ParentId`'Parent Account ID':s, Phone`'Account Phone':s, PhotoUrl`'Photo URL':s, ShippingCity`'Shipping City':s, ShippingCountry`'Shipping Country':s, ShippingGeocodeAccuracy`'Shipping " +
                "Geocode Accuracy':s, ShippingLatitude`'Shipping Latitude':w, ShippingLongitude`'Shipping Longitude':w, ShippingPostalCode`'Shipping Zip/Postal Code':s, ShippingState`'Shipping State/Province':s, ShippingStreet`'Shipping " +
                "Street':s, SicDesc`'SIC Description':s, SystemModstamp`'System Modstamp':d, Type`'Account Type':s, Website:s]", sfTable.Type.ToStringWithDisplayNames());

#pragma warning disable CS0618 // Type or member is obsolete

            // Enable IR rewritter to auto-inject ServiceProvider where needed
            engine.EnableTabularConnectors();

#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("Accounts", sfTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues)
                                    .AddService<ConnectorLogger>(logger)
                                    .AddService<HttpClient>(client);

            // Expression with tabular connector
            string expr = @"First(Accounts).'Account ID'";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Confirm that InjectServiceProviderFunction has properly been added
            string ir = new Regex("RuntimeValues_[0-9]+").Replace(check.PrintIR(), "RuntimeValues_XXX");
            Assert.Equal(
                "FieldAccess(First:![AccountSource:s, BillingCity:s, BillingCountry:s, BillingGeocodeAccuracy:s, BillingLatitude:w, BillingLongitude:w, BillingPostalCode:s, BillingState:s, BillingStreet:s, CreatedById:s, " +
                "CreatedDate:d, Description:s, Id:s, Industry:s, IsDeleted:b, Jigsaw:s, JigsawCompanyId:s, LastActivityDate:D, LastModifiedById:s, LastModifiedDate:d, LastReferencedDate:d, LastViewedDate:d, MasterRecordId:s, " +
                "Name:s, NumberOfEmployees:w, OwnerId:s, ParentId:s, Phone:s, PhotoUrl:s, ShippingCity:s, ShippingCountry:s, ShippingGeocodeAccuracy:s, ShippingLatitude:w, ShippingLongitude:w, ShippingPostalCode:s, ShippingState:s, " +
                "ShippingStreet:s, SicDesc:s, SystemModstamp:d, Type:s, Website:s](InjectServiceProviderFunction:*[AccountSource:s, BillingCity:s, BillingCountry:s, BillingGeocodeAccuracy:s, BillingLatitude:w, BillingLongitude:w, " +
                "BillingPostalCode:s, BillingState:s, BillingStreet:s, CreatedById:s, CreatedDate:d, Description:s, Id:s, Industry:s, IsDeleted:b, Jigsaw:s, JigsawCompanyId:s, LastActivityDate:D, LastModifiedById:s, LastModifiedDate:d, " +
                "LastReferencedDate:d, LastViewedDate:d, MasterRecordId:s, Name:s, NumberOfEmployees:w, OwnerId:s, ParentId:s, Phone:s, PhotoUrl:s, ShippingCity:s, ShippingCountry:s, ShippingGeocodeAccuracy:s, ShippingLatitude:w, " +
                "ShippingLongitude:w, ShippingPostalCode:s, ShippingState:s, ShippingStreet:s, SicDesc:s, SystemModstamp:d, Type:s, Website:s](ResolvedObject('Accounts:RuntimeValues_XXX'))), Id)", ir);

            // Use tabular connector. Internally we'll call ConnectorTableValueWithServiceProvider.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SF GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);

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

            ConnectorTable tabularService = new ConnectorTable("default", "Account");

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Account", tabularService.TableName);

            testConnector.SetResponseFromFiles(@"Responses\SF GetDatasetsMetadata.json", @"Responses\SF GetSchema.json");
            await tabularService.InitAsync(client, $"/apim/salesforce/{connectionId}", CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);

            ConnectorTableValue sfTable = tabularService.GetTableValue();
            Assert.True(sfTable._tabularService.IsInitialized);
            Assert.True(sfTable.IsDelegable);

            Assert.Equal(
                "*[AccountSource`'Account Source':s, BillingCity`'Billing City':s, BillingCountry`'Billing Country':s, BillingGeocodeAccuracy`'Billing Geocode Accuracy':s, BillingLatitude`'Billing Latitude':w, BillingLongitude`'Billing " +
                "Longitude':w, BillingPostalCode`'Billing Zip/Postal Code':s, BillingState`'Billing State/Province':s, BillingStreet`'Billing Street':s, CreatedById`'Created By ID':s, CreatedDate`'Created Date':d, Description`'Account " +
                "Description':s, Id`'Account ID':s, Industry:s, IsDeleted`Deleted:b, Jigsaw`'Data.com Key':s, JigsawCompanyId`'Jigsaw Company ID':s, LastActivityDate`'Last Activity':D, LastModifiedById`'Last Modified By " +
                "ID':s, LastModifiedDate`'Last Modified Date':d, LastReferencedDate`'Last Referenced Date':d, LastViewedDate`'Last Viewed Date':d, MasterRecordId`'Master Record ID':s, Name`'Account Name':s, NumberOfEmployees`Employees:w, " +
                "OwnerId`'Owner ID':s, ParentId`'Parent Account ID':s, Phone`'Account Phone':s, PhotoUrl`'Photo URL':s, ShippingCity`'Shipping City':s, ShippingCountry`'Shipping Country':s, ShippingGeocodeAccuracy`'Shipping " +
                "Geocode Accuracy':s, ShippingLatitude`'Shipping Latitude':w, ShippingLongitude`'Shipping Longitude':w, ShippingPostalCode`'Shipping Zip/Postal Code':s, ShippingState`'Shipping State/Province':s, ShippingStreet`'Shipping " +
                "Street':s, SicDesc`'SIC Description':s, SystemModstamp`'System Modstamp':d, Type`'Account Type':s, Website:s]", sfTable.Type.ToStringWithDisplayNames());

#pragma warning disable CS0618 // Type or member is obsolete

            // Enable IR rewritter to auto-inject ServiceProvider where needed
            engine.EnableTabularConnectors();

#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("Accounts", sfTable);
            RuntimeConfig rc = new RuntimeConfig(symbolValues)
                                    .AddService<ConnectorLogger>(logger)
                                    .AddService<HttpClient>(client);

            // Expression with tabular connector
            string expr = @"First(Accounts).'Account ID'";
            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Confirm that InjectServiceProviderFunction has properly been added
            string ir = new Regex("RuntimeValues_[0-9]+").Replace(check.PrintIR(), "RuntimeValues_XXX");
            Assert.Equal(
                "FieldAccess(First:![AccountSource:s, BillingCity:s, BillingCountry:s, BillingGeocodeAccuracy:s, BillingLatitude:w, BillingLongitude:w, BillingPostalCode:s, BillingState:s, BillingStreet:s, CreatedById:s, " +
                "CreatedDate:d, Description:s, Id:s, Industry:s, IsDeleted:b, Jigsaw:s, JigsawCompanyId:s, LastActivityDate:D, LastModifiedById:s, LastModifiedDate:d, LastReferencedDate:d, LastViewedDate:d, MasterRecordId:s, " +
                "Name:s, NumberOfEmployees:w, OwnerId:s, ParentId:s, Phone:s, PhotoUrl:s, ShippingCity:s, ShippingCountry:s, ShippingGeocodeAccuracy:s, ShippingLatitude:w, ShippingLongitude:w, ShippingPostalCode:s, ShippingState:s, " +
                "ShippingStreet:s, SicDesc:s, SystemModstamp:d, Type:s, Website:s](InjectServiceProviderFunction:*[AccountSource:s, BillingCity:s, BillingCountry:s, BillingGeocodeAccuracy:s, BillingLatitude:w, BillingLongitude:w, " +
                "BillingPostalCode:s, BillingState:s, BillingStreet:s, CreatedById:s, CreatedDate:d, Description:s, Id:s, Industry:s, IsDeleted:b, Jigsaw:s, JigsawCompanyId:s, LastActivityDate:D, LastModifiedById:s, LastModifiedDate:d, " +
                "LastReferencedDate:d, LastViewedDate:d, MasterRecordId:s, Name:s, NumberOfEmployees:w, OwnerId:s, ParentId:s, Phone:s, PhotoUrl:s, ShippingCity:s, ShippingCountry:s, ShippingGeocodeAccuracy:s, ShippingLatitude:w, " +
                "ShippingLongitude:w, ShippingPostalCode:s, ShippingState:s, ShippingStreet:s, SicDesc:s, SystemModstamp:d, Type:s, Website:s](ResolvedObject('Accounts:RuntimeValues_XXX'))), Id)", ir);

            // Use tabular connector. Internally we'll call ConnectorTableValueWithServiceProvider.GetRowsInternal to get the data
            testConnector.SetResponseFromFile(@"Responses\SF GetData.json");
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, rc).ConfigureAwait(false);

            StringValue accountId = Assert.IsType<StringValue>(result);
            Assert.Equal("001DR00001Xj1YmYAJ", accountId.Value);
        }
    }
}
