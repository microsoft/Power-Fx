// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public async Task SQL_Tabular()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            using var httpClient = new HttpClient(testConnector);
            string jwt = "eyJ0eXAiOi...";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", "e74bd8913489439e886426eba8dec1c8", () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832383"
            };

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("e74bd8913489439e886426eba8dec1c8") },
                { "server", FormulaValue.New("pfxdev-sql.database.windows.net") },
                { "database", FormulaValue.New("connectortest") },
                { "table", FormulaValue.New("Customers") }
            });

            // Use of tabular connector
            // There is a network call here to retrieve the table's schema
            testConnector.SetResponseFromFile(@"Responses\SQL Server Load Customers DB.json");
            CdpSwaggerTabularService tabularService = new CdpSwaggerTabularService(apiDoc, globals);
            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Customers", tabularService.TableName);
            Assert.Equal("_tbl_e74bd8913489439e886426eba8dec1c8", tabularService.Namespace);

            await tabularService.InitAsync(config, client, CancellationToken.None, new ConsoleLogger(_output)).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);
            Assert.Equal("Customers", tabularService.Name);
            Assert.Equal("Customers", tabularService.DisplayName);

            ConnectorTableValue sqlTable = tabularService.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegatable);

            //Assert.True(sqlTable.Is)
            Assert.Equal("*[Address:s, Country:s, CustomerId:w, Name:s, Phone:s]", sqlTable.Type._type.ToString());

#pragma warning disable CS0618 // Type or member is obsolete

            // Enable IR rewritter to auto-inject ServiceProvider where needed
            engine.EnableTabularConnectors();

#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("Customers", sqlTable);
            BaseRuntimeConnectorContext runtimeContext = new TestConnectorRuntimeContext(tabularService.Namespace, client, console: _output);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddRuntimeContext(runtimeContext);

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
            testConnector.SetResponseFromFiles(@"Responses\SQL Server Get First Customers.json", @"Responses\SQL Server Get First Customers.json");
            result = await engine.EvalAsync("First(Customers).Phone; Last(Customers).Phone", CancellationToken.None, options: new ParserOptions() { AllowsSideEffects = true }, runtimeConfig: rc).ConfigureAwait(false);
            StringValue phone = Assert.IsType<StringValue>(result);
            Assert.Equal("+1-425-705-0000", phone.Value);
            Assert.Equal(2, testConnector.CurrentResponse); // This confirms we had 2 network calls
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
            CdpTabularService tabularService = new CdpTabularService("pfxdev-sql.database.windows.net,connectortest", "Customers");

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Customers", tabularService.TableName);

            await tabularService.InitAsync(client, $"/apim/sql/{connectionId}", true, CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);

            ConnectorTableValue sqlTable = tabularService.GetTableValue();
            Assert.True(sqlTable._tabularService.IsInitialized);
            Assert.True(sqlTable.IsDelegatable);
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
        [Obsolete("Using SwaggerTabularService.GetFunctions")]
        public async Task SQL_Tabular_CheckParams()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SQL Server.json", _output);
            var apiDoc = testConnector._apiDocument;

            string[] globalValueNames = CdpSwaggerTabularService.GetGlobalValueNames(apiDoc);
            Assert.Equal("connectionId, database, server, table", string.Join(", ", globalValueNames.OrderBy(x => x)));

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("e74bd8913489439e886426eba8dec1c8") },
                { "server", FormulaValue.New("pfxdev-sql.database.windows.net") },
                { "database", FormulaValue.New("connectortest") },
                { "table", FormulaValue.New("Customers") }
            });

            bool isTabularService = CdpSwaggerTabularService.IsTabular(globals, apiDoc, out string error);
            Assert.True(isTabularService);

            var (s, c, r, u, d) = CdpSwaggerTabularService.GetFunctions(globals, apiDoc);

            Assert.Equal("GetTableV2", s.Name);
            Assert.Equal("PostItemV2", c.Name);
            Assert.Equal("GetItemsV2", r.Name);
            Assert.Equal("PatchItemV2", u.Name);
            Assert.Equal("DeleteItemV2", d.Name);

            Assert.Equal(@"/apim/sql/{connectionId}/v2/$metadata.json/datasets/{server},{database}/tables/{table}", s.OperationPath);
            Assert.Equal(@"/apim/sql/{connectionId}/v2/datasets/{server},{database}/tables/{table}/items", c.OperationPath);
            Assert.Equal(@"/apim/sql/{connectionId}/v2/datasets/{server},{database}/tables/{table}/items", r.OperationPath);
            Assert.Equal(@"/apim/sql/{connectionId}/v2/datasets/{server},{database}/tables/{table}/items/{id}", u.OperationPath);
            Assert.Equal(@"/apim/sql/{connectionId}/v2/datasets/{server},{database}/tables/{table}/items/{id}", d.OperationPath);

            Assert.Equal("GET", s.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("POST", c.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("GET", r.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("PATCH", u.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("DELETE", d.HttpMethod.Method.ToUpperInvariant());

            Assert.Equal(string.Empty, string.Join(", ", s.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("item", string.Join(", ", c.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal(string.Empty, string.Join(", ", r.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("id, item", string.Join(", ", u.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("id", string.Join(", ", d.RequiredParameters.Select(rp => rp.Name)));
        }

        [Fact]
        public async Task SP_Tabular()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SharePoint.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            using var httpClient = new HttpClient(testConnector);
            string jwt = "eyJ0eXAiO...";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", "0b905132239e463a9d12f816be201da9", () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832384"
            };

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("0b905132239e463a9d12f816be201da9") },
                { "dataset", FormulaValue.New("https://microsofteur.sharepoint.com/teams/pfxtest") },
                { "table", FormulaValue.New("Documents") },
            });

            // Use of tabular connector
            // There is a network call here to retrieve the table's schema
            testConnector.SetResponseFromFile(@"Responses\SP GetTable.json");

            CdpSwaggerTabularService tabularService = new CdpSwaggerTabularService(apiDoc, globals);
            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Documents", tabularService.TableName);
            Assert.Equal("_tbl_0b905132239e463a9d12f816be201da9", tabularService.Namespace);

            await tabularService.InitAsync(config, client, CancellationToken.None, new ConsoleLogger(_output)).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);
            Assert.Equal("Documents", tabularService.Name);
            Assert.Equal("Documents", tabularService.DisplayName);

            ConnectorTableValue spTable = tabularService.GetTableValue();
            Assert.True(spTable._tabularService.IsInitialized);
            Assert.True(spTable.IsDelegatable);

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
            BaseRuntimeConnectorContext runtimeContext = new TestConnectorRuntimeContext(tabularService.Namespace, client, console: _output);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddRuntimeContext(runtimeContext);

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
            using var testConnector = new LoggingTestServer(@"Swagger\SharePoint.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            using var httpClient = new HttpClient(testConnector);
            string connectionId = "0b905132239e463a9d12f816be201da9";
            string jwt = "eyJ0eXAiOiJKV....";
            using var client = new PowerPlatformConnectorClient("firstrelease-003.azure-apihub.net", "49970107-0806-e5a7-be5e-7c60e2750f01", connectionId, () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832384"
            };

            // Use of tabular connector
            // There is a network call here to retrieve the table's schema
            testConnector.SetResponseFromFile(@"Responses\SP GetTable.json");
            ConsoleLogger logger = new ConsoleLogger(_output);
            CdpTabularService tabularService = new CdpTabularService("https://microsofteur.sharepoint.com/teams/pfxtest", "Documents");

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Documents", tabularService.TableName);

            await tabularService.InitAsync(client, $"/apim/sharepointonline/{connectionId}", false, CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);

            ConnectorTableValue spTable = tabularService.GetTableValue();
            Assert.True(spTable._tabularService.IsInitialized);
            Assert.True(spTable.IsDelegatable);

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
        public async Task SP_Tabular_MissingParam()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SharePoint.json", _output);
            var apiDoc = testConnector._apiDocument;

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("0b905132239e463a9d12f816be201da9") },
                { "table", FormulaValue.New("Documents") },
            });

            bool isTabularService = CdpSwaggerTabularService.IsTabular(globals, apiDoc, out string error);
            Assert.False(isTabularService);
            Assert.Equal("Missing global value dataset", error);
        }

        [Fact]
        public async Task SP_Tabular_MissingParam2()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SharePoint.json", _output);
            var apiDoc = testConnector._apiDocument;

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("0b905132239e463a9d12f816be201da9") },
                { "dataset", FormulaValue.New("https://microsofteur.sharepoint.com/teams/pfxtest") },
            });

            bool isTabularService = CdpSwaggerTabularService.IsTabular(globals, apiDoc, out string error);
            Assert.False(isTabularService);
            Assert.Equal("Missing global value table", error);
        }

        [Fact]
        [Obsolete("Using SwaggerTabularService.GetFunctions")]
        public async Task SP_Tabular_CheckParams()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SharePoint.json", _output);
            var apiDoc = testConnector._apiDocument;

            string[] globalValueNames = CdpSwaggerTabularService.GetGlobalValueNames(apiDoc);
            Assert.Equal("connectionId, dataset, table", string.Join(", ", globalValueNames.OrderBy(x => x)));

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("0b905132239e463a9d12f816be201da9") },
                { "dataset", FormulaValue.New("https://microsofteur.sharepoint.com/teams/pfxtest") },
                { "table", FormulaValue.New("Documents") },
            });

            bool isTabularService = CdpSwaggerTabularService.IsTabular(globals, apiDoc, out string error);
            Assert.True(isTabularService);

            var (s, c, r, u, d) = CdpSwaggerTabularService.GetFunctions(globals, apiDoc);

            Assert.Equal("GetTable", s.Name);
            Assert.Equal("PostItem", c.Name);
            Assert.Equal("GetItems", r.Name);
            Assert.Equal("PatchItem", u.Name);
            Assert.Equal("DeleteItem", d.Name);

            Assert.Equal(@"/apim/sharepointonline/{connectionId}/$metadata.json/datasets/{dataset}/tables/{table}", s.OperationPath);
            Assert.Equal(@"/apim/sharepointonline/{connectionId}/datasets/{dataset}/tables/{table}/items", c.OperationPath);
            Assert.Equal(@"/apim/sharepointonline/{connectionId}/datasets/{dataset}/tables/{table}/items", r.OperationPath);
            Assert.Equal(@"/apim/sharepointonline/{connectionId}/datasets/{dataset}/tables/{table}/items/{id}", u.OperationPath);
            Assert.Equal(@"/apim/sharepointonline/{connectionId}/datasets/{dataset}/tables/{table}/items/{id}", d.OperationPath);

            Assert.Equal("GET", s.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("POST", c.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("GET", r.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("PATCH", u.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("DELETE", d.HttpMethod.Method.ToUpperInvariant());

            Assert.Equal(string.Empty, string.Join(", ", s.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("item", string.Join(", ", c.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal(string.Empty, string.Join(", ", r.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("id, item", string.Join(", ", u.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("id", string.Join(", ", d.RequiredParameters.Select(rp => rp.Name)));
        }

        [Fact]
        public async Task SF_Tabular()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SalesForce.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            using var httpClient = new HttpClient(testConnector);
            string jwt = "eyJ0eXAi...";
            using var client = new PowerPlatformConnectorClient("tip2-001.azure-apihub.net", "53d7f409-4bce-e458-8245-5fa1346ec433", "ec5fe6d1cad744a0a716fe4597a74b2e", () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832384"
            };

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("ec5fe6d1cad744a0a716fe4597a74b2e") },
                { "table", FormulaValue.New("Account") },
            });

            // Use of tabular connector
            CdpSwaggerTabularService tabularService = new CdpSwaggerTabularService(apiDoc, globals);
            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Account", tabularService.TableName);
            Assert.Equal("_tbl_ec5fe6d1cad744a0a716fe4597a74b2e", tabularService.Namespace);

            // There is a network call here to retrieve the table's schema
            testConnector.SetResponseFromFile(@"Responses\SF GetSchema.json");
            await tabularService.InitAsync(config, client, CancellationToken.None, new ConsoleLogger(_output)).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);
            Assert.Equal("Account", tabularService.Name);
            Assert.Equal("Accounts", tabularService.DisplayName);

            ConnectorTableValue sfTable = tabularService.GetTableValue();
            Assert.True(sfTable._tabularService.IsInitialized);
            Assert.True(sfTable.IsDelegatable);

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
            BaseRuntimeConnectorContext runtimeContext = new TestConnectorRuntimeContext(tabularService.Namespace, client, console: _output);
            RuntimeConfig rc = new RuntimeConfig(symbolValues).AddRuntimeContext(runtimeContext);

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

            StringValue docName = Assert.IsType<StringValue>(result);
            Assert.Equal("001DR00001Xj1YmYAJ", docName.Value);
        }

        [Fact]
        public async Task SF_CdpTabular()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SalesForce.json", _output);
            var apiDoc = testConnector._apiDocument;
            var config = new PowerFxConfig(Features.PowerFxV1);
            var engine = new RecalcEngine(config);

            using var httpClient = new HttpClient(testConnector);
            string connectionId = "ec5fe6d1cad744a0a716fe4597a74b2e";
            string jwt = "eyJ0eXAiOiJ...";
            using var client = new PowerPlatformConnectorClient("tip2-001.azure-apihub.net", "53d7f409-4bce-e458-8245-5fa1346ec433", connectionId, () => jwt, httpClient)
            {
                SessionId = "8e67ebdc-d402-455a-b33a-304820832384"
            };

            // Use of tabular connector
            // There is a network call here to retrieve the table's schema
            testConnector.SetResponseFromFile(@"Responses\SF GetSchema.json");
            ConsoleLogger logger = new ConsoleLogger(_output);
            CdpTabularService tabularService = new CdpTabularService("default", "Account");

            Assert.False(tabularService.IsInitialized);
            Assert.Equal("Account", tabularService.TableName);

            await tabularService.InitAsync(client, $"/apim/salesforce/{connectionId}", false, CancellationToken.None, logger).ConfigureAwait(false);
            Assert.True(tabularService.IsInitialized);

            ConnectorTableValue sfTable = tabularService.GetTableValue();
            Assert.True(sfTable._tabularService.IsInitialized);
            Assert.True(sfTable.IsDelegatable);

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
        [Obsolete("Using SwaggerTabularService.GetFunctions")]
        public async Task SF_Tabular_CheckParams()
        {
            using var testConnector = new LoggingTestServer(@"Swagger\SalesForce.json", _output);
            var apiDoc = testConnector._apiDocument;

            string[] globalValueNames = CdpSwaggerTabularService.GetGlobalValueNames(apiDoc);
            Assert.Equal("connectionId, table", string.Join(", ", globalValueNames.OrderBy(x => x)));

            IReadOnlyDictionary<string, FormulaValue> globals = new ReadOnlyDictionary<string, FormulaValue>(new Dictionary<string, FormulaValue>()
            {
                { "connectionId", FormulaValue.New("0b905132239e463a9d12f816be201da9") },
                { "table", FormulaValue.New("Documents") },
            });

            bool isTabularService = CdpSwaggerTabularService.IsTabular(globals, apiDoc, out string error);
            Assert.True(isTabularService, error);

            var (s, c, r, u, d) = CdpSwaggerTabularService.GetFunctions(globals, apiDoc);

            Assert.Equal("GetTable", s.Name);
            Assert.Equal("PostItemV2", c.Name);
            Assert.Equal("GetItems", r.Name);
            Assert.Equal("PatchItemV3", u.Name);
            Assert.Equal("DeleteItem", d.Name);

            Assert.Equal(@"/apim/salesforce/{connectionId}/$metadata.json/datasets/default/tables/{table}", s.OperationPath);
            Assert.Equal(@"/apim/salesforce/{connectionId}/v2/datasets/default/tables/{table}/items", c.OperationPath);
            Assert.Equal(@"/apim/salesforce/{connectionId}/datasets/default/tables/{table}/items", r.OperationPath);
            Assert.Equal(@"/apim/salesforce/{connectionId}/v3/datasets/default/tables/{table}/items/{id}", u.OperationPath);
            Assert.Equal(@"/apim/salesforce/{connectionId}/datasets/default/tables/{table}/items/{id}", d.OperationPath);

            Assert.Equal("GET", s.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("POST", c.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("GET", r.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("PATCH", u.HttpMethod.Method.ToUpperInvariant());
            Assert.Equal("DELETE", d.HttpMethod.Method.ToUpperInvariant());

            Assert.Equal(string.Empty, string.Join(", ", s.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("item", string.Join(", ", c.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal(string.Empty, string.Join(", ", r.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("id, item", string.Join(", ", u.RequiredParameters.Select(rp => rp.Name)));
            Assert.Equal("id", string.Join(", ", d.RequiredParameters.Select(rp => rp.Name)));
        }
    }
}
