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
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
#pragma warning disable CS0618 // Type or member is obsolete https://github.com/microsoft/Power-Fx/issues/2940

    /// <summary>
    /// Tests for CdpTableResolver caching mechanism.
    /// </summary>
    public sealed class CdpTableCacheTests : IAsyncLifetime, IDisposable
    {
        private LoggingTestServer _server;
        private HttpClient _httpClient;
        private ConsoleLogger _logger;
        private PowerPlatformConnectorClient _client;
        private bool _disposed;
        private const string ConnectionId = "c1a4e9f52ec94d55bb82f319b3e33a6a";
        private readonly string _basePath = $"/apim/sql/{ConnectionId}";
        private const string Jwt = "eyJ0eXAiOiJKV1QiL...";
        private readonly ITestOutputHelper _output;

        public CdpTableCacheTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public async Task InitializeAsync()
        {
            _server = new LoggingTestServer(null, _output);
            _httpClient = new HttpClient(_server);
            _logger = new ConsoleLogger(_output);

            _client = new PowerPlatformConnectorClient(
                endpoint: "firstrelease-003.azure-apihub.net",
                environmentId: "49970107-0806-e5a7-be5e-7c60e2750f01",
                connectionId: ConnectionId,
                getAuthToken: () => Jwt,
                httpInvoker: _httpClient)
            { SessionId = Guid.NewGuid().ToString() };

            await Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            Dispose();
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _server?.Dispose();
                _httpClient?.Dispose();
                _client?.Dispose();
            }

            _disposed = true;
        }

        [Fact]
        public async Task TestCacheHit_SameTableResolvedTwice_UsesCachedResult()
        {
            // Arrange
            _server.SetResponseFromFiles(
                @"Responses\SQL GetDatasetsMetadata.json",
                @"Responses\SQL GetTables.json",
                @"Responses\SQL Server Load Customers DB.json");

            var dataSource = new CdpDataSource(
                "pfxdev-sql.database.windows.net,connectortest",
                ConnectorSettings.NewCDPConnectorSettings());

            var tables = await dataSource.GetTablesAsync(_client, _basePath, CancellationToken.None, _logger);
            var cache = dataSource.TableMetadataCache;

            // Verify cache starts empty
            Assert.Empty(cache);

            // Track request count before first table init
            var requestCountBeforeFirstInit = _server.CurrentResponse;

            // Act - First table initialization (should populate cache)
            var custTable1 = tables.First(t => t.DisplayName == "Customers");
            await custTable1.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            // Cache should now have one entry
            Assert.Single(cache);
            var cachedTask = cache.Values.First();
            Assert.True(cachedTask.IsCompleted);

            // Verify one network call was made
            var requestCountAfterFirstInit = _server.CurrentResponse;
            Assert.Equal(requestCountBeforeFirstInit + 1, requestCountAfterFirstInit);

            // Act - Second table with same name (should use cache)
            var custTable2 = tables.First(t => t.DisplayName == "Customers");

            // Don't set a new response - if it tries to fetch, it will fail
            await custTable2.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            // Assert - Cache should still have only one entry (cache hit)
            Assert.Single(cache);

            // Verify NO additional network call was made (cache was used)
            var requestCountAfterSecondInit = _server.CurrentResponse;
            Assert.Equal(requestCountAfterFirstInit, requestCountAfterSecondInit);

            Assert.NotNull(custTable1.ConnnectorType);
            Assert.NotNull(custTable2.ConnnectorType);

            // Both should have valid types
            Assert.NotNull(custTable1.RecordType);
            Assert.NotNull(custTable2.RecordType);
        }

        [Fact]
        public async Task TestCacheMiss_DifferentTables_CreatesMultipleCacheEntries()
        {
            // Arrange
            _server.SetResponseFromFiles(
                @"Responses\SQL GetDatasetsMetadata.json",
                @"Responses\SQL GetTables.json",
                @"Responses\SQL Server Load Customers DB.json",
                @"Responses\SQL GetSchema Products.json");

            var dataSource = new CdpDataSource(
                "pfxdev-sql.database.windows.net,connectortest",
                ConnectorSettings.NewCDPConnectorSettings());

            var tables = await dataSource.GetTablesAsync(_client, _basePath, CancellationToken.None, _logger);
            var cache = dataSource.TableMetadataCache;

            var initialRequestCount = _server.CurrentResponse;

            // Act - Initialize first table
            var custTable = tables.First(t => t.DisplayName == "Customers");
            await custTable.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            // Cache should have one entry
            Assert.Single(cache);

            var requestCountAfterFirst = _server.CurrentResponse;
            Assert.Equal(initialRequestCount + 1, requestCountAfterFirst);

            // Act - Initialize second table (different table = cache miss)
            var productsTable = tables.First(t => t.DisplayName == "Products");
            await productsTable.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            // Assert - Cache should now have two entries
            Assert.Equal(2, cache.Count);

            // Verify second network call was made (different table = cache miss)
            var requestCountAfterSecond = _server.CurrentResponse;
            Assert.Equal(requestCountAfterFirst + 1, requestCountAfterSecond);

            // Total of 2 network calls for 2 different tables
            Assert.Equal(initialRequestCount + 2, requestCountAfterSecond);

            Assert.NotNull(custTable.ConnnectorType);
            Assert.NotNull(productsTable.ConnnectorType);
        }

        [Fact]
        public async Task TestCacheClear_AfterClear_CacheIsEmpty()
        {
            // Arrange
            _server.SetResponseFromFiles(
                @"Responses\SQL GetDatasetsMetadata.json",
                @"Responses\SQL GetTables.json",
                @"Responses\SQL Server Load Customers DB.json");

            var dataSource = new CdpDataSource(
                "pfxdev-sql.database.windows.net,connectortest",
                ConnectorSettings.NewCDPConnectorSettings());

            var tables = await dataSource.GetTablesAsync(_client, _basePath, CancellationToken.None, _logger);
            var cache = dataSource.TableMetadataCache;

            var initialRequestCount = _server.CurrentResponse;
            
            // Populate cache
            var custTable = tables.First(t => t.DisplayName == "Customers");
            await custTable.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            Assert.Single(cache);

            var requestCountAfterInit = _server.CurrentResponse;
            Assert.Equal(initialRequestCount + 1, requestCountAfterInit);

            // Act - Clear cache (should not make any network calls)
            var requestCountBeforeClear = _server.CurrentResponse;
            dataSource.ClearTableMetadataCache();
            var requestCountAfterClear = _server.CurrentResponse;

            // Assert
            Assert.Empty(cache);

            // Verify clearing cache didn't make any network calls
            Assert.Equal(requestCountBeforeClear, requestCountAfterClear);
        }

        [Fact]
        public async Task TestConcurrentAccess_MultipleTables_CacheIsThreadSafe()
        {
            // Arrange
            _server.SetResponseFromFiles(
                @"Responses\SQL GetDatasetsMetadata.json",
                @"Responses\SQL GetTables.json",
                @"Responses\SQL Server Load Customers DB.json",
                @"Responses\SQL GetSchema Products.json");

            var dataSource = new CdpDataSource(
                "pfxdev-sql.database.windows.net,connectortest",
                ConnectorSettings.NewCDPConnectorSettings());

            var tables = await dataSource.GetTablesAsync(_client, _basePath, CancellationToken.None, _logger);
            var cache = dataSource.TableMetadataCache;

            var initialRequestCount = _server.CurrentResponse;

            // Act - Initialize multiple tables concurrently
            var custTable1 = tables.First(t => t.DisplayName == "Customers");
            var custTable2 = tables.First(t => t.DisplayName == "Customers");
            var productsTable = tables.First(t => t.DisplayName == "Products");

            var tasks = new List<Task>
            {
                custTable1.InitAsync(_client, _basePath, CancellationToken.None, _logger),
                custTable2.InitAsync(_client, _basePath, CancellationToken.None, _logger),
                productsTable.InitAsync(_client, _basePath, CancellationToken.None, _logger)
            };

            await Task.WhenAll(tasks);

            var requestCountAfterInit = _server.CurrentResponse;

            // Assert - Cache should have 2 entries (Customers and Products)
            // Both Customers requests should share the same cache entry
            Assert.True(cache.Count == 2, $"Expected at most 2 cache entries, but found {cache.Count}");

            // Verify only 2 network calls were made (one for Customers, one for Products)
            // despite 3 concurrent initialization requests
            Assert.Equal(initialRequestCount + 2, requestCountAfterInit);

            Assert.NotNull(custTable1.ConnnectorType);
            Assert.NotNull(custTable2.ConnnectorType);
            Assert.NotNull(productsTable.ConnnectorType);
        }

        [Fact]
        public void TestCacheInitialization_NewDataSource_CacheIsEmpty()
        {
            // Arrange & Act
            var dataSource = new CdpDataSource(
                "test-dataset",
                ConnectorSettings.NewCDPConnectorSettings());

            var cache = dataSource.TableMetadataCache;

            // Assert
            Assert.NotNull(cache);
            Assert.Empty(cache);
            Assert.IsType<ConcurrentDictionary<string, Task<(ConnectorType, IEnumerable<OptionSet>)>>>(cache);
        }
    }

#pragma warning restore CS0618
}
