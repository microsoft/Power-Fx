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

        /// <summary>
        /// Verifies that different callers can cancel independently without affecting each other.
        /// </summary>
        [Fact]
        public async Task TestCancellationIndependence_OneCallerCancels_OtherCallerSucceeds()
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
            var custTable1 = tables.First(t => t.DisplayName == "Customers");
            var custTable2 = tables.First(t => t.DisplayName == "Customers");

            // Create a cancellation token that's already cancelled
            using var cts1 = new CancellationTokenSource();
            cts1.Cancel();

            // Act - First caller with cancelled token should fail
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await custTable1.InitAsync(_client, _basePath, cts1.Token, _logger));

            // Second caller with valid token should succeed (uses cached result)
            await custTable2.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            // Assert
            Assert.NotNull(custTable2.ConnnectorType);
            Assert.NotNull(custTable2.RecordType);
        }

        /// <summary>
        /// Verifies that failed fetches don't stay in cache permanently.
        /// </summary>
        [Fact]
        public async Task TestFailedFetchRetry_FirstFetchFails_SecondFetchSucceeds()
        {
            // Arrange - Set up responses: success, success, failure, success
            // First two: GetDatasetsMetadata and GetTables
            // Third: Failed table metadata fetch (empty response)
            // Fourth: Successful retry
            _server.SetResponseFromFiles(
                @"Responses\SQL GetDatasetsMetadata.json",
                @"Responses\SQL GetTables.json",
                @"Responses\EmptyResponse.json", // Empty response will cause a failure
                @"Responses\SQL Server Load Customers DB.json");

            var dataSource = new CdpDataSource(
                "pfxdev-sql.database.windows.net,connectortest",
                ConnectorSettings.NewCDPConnectorSettings());

            var tables = await dataSource.GetTablesAsync(_client, _basePath, CancellationToken.None, _logger);
            var custTable1 = tables.First(t => t.DisplayName == "Customers");
            var custTable2 = tables.First(t => t.DisplayName == "Customers");

            var cache = dataSource.TableMetadataCache;
            var initialRequestCount = _server.CurrentResponse;

            // Act - First init should fail (gets empty response)
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await custTable1.InitAsync(_client, _basePath, CancellationToken.None, _logger));

            Assert.Contains("didn't receive any response", exception.Message);

            // Verify network call was made
            var requestCountAfterFail = _server.CurrentResponse;
            Assert.Equal(initialRequestCount + 1, requestCountAfterFail);

            // Cache should be empty (failed task removed)
            Assert.Empty(cache);

            // Act - Second init should succeed (retry with next response)
            await custTable2.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            // Verify another network call was made (not cached failure)
            var requestCountAfterRetry = _server.CurrentResponse;
            Assert.Equal(requestCountAfterFail + 1, requestCountAfterRetry);

            // Cache should now have successful result
            Assert.Single(cache);
            Assert.NotNull(custTable2.ConnnectorType);
            Assert.NotNull(custTable2.RecordType);
        }

        /// <summary>
        /// Verifies that concurrent access with different cancellation tokens works correctly.
        /// </summary>
        [Fact]
        public async Task TestConcurrentAccessWithCancellation_DifferentTokens_IndependentCancellation()
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

            var initialRequestCount = _server.CurrentResponse;

            // Create multiple table instances for same table
            var custTable1 = tables.First(t => t.DisplayName == "Customers");
            var custTable2 = tables.First(t => t.DisplayName == "Customers");
            var custTable3 = tables.First(t => t.DisplayName == "Customers");

            // Create cancellation tokens
            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            cts1.Cancel(); // Cancel first immediately

            // Act - Start all initializations concurrently
            var task1 = custTable1.InitAsync(_client, _basePath, cts1.Token, _logger);
            var task2 = custTable2.InitAsync(_client, _basePath, cts2.Token, _logger);
            var task3 = custTable3.InitAsync(_client, _basePath, CancellationToken.None, _logger);

            // Wait for results
            var results = await Task.WhenAll(
                task1.ContinueWith(t => new { Success = t.Status == TaskStatus.RanToCompletion, Table = (CdpTable)null }, TaskScheduler.Default),
                task2.ContinueWith(t => new { Success = t.Status == TaskStatus.RanToCompletion, Table = t.Status == TaskStatus.RanToCompletion ? custTable2 : null }, TaskScheduler.Default),
                task3.ContinueWith(t => new { Success = t.Status == TaskStatus.RanToCompletion, Table = t.Status == TaskStatus.RanToCompletion ? custTable3 : null }, TaskScheduler.Default));

            // Assert
            // Task1 should have been cancelled
            Assert.False(results[0].Success);

            // Task2 and Task3 should succeed
            Assert.True(results[1].Success);
            Assert.True(results[2].Success);

            Assert.NotNull(results[1].Table?.ConnnectorType);
            Assert.NotNull(results[2].Table?.ConnnectorType);

            // Only one network call should have been made (async deduplication)
            var finalRequestCount = _server.CurrentResponse;
            Assert.Equal(initialRequestCount + 1, finalRequestCount);

            // Cache should have one entry
            Assert.Single(dataSource.TableMetadataCache);
        }
    }

#pragma warning restore CS0618
}
