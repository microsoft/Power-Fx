// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class CacheTests : PowerFxTest
    {
        private readonly ICachingHttpClient _cache = new CachingHttpClient();

        [Fact]
        public async Task TestCache()
        {            
            var scope1 = "scope1";
            var scope2 = "scope2";

            Assert.Equal(5.0, Test(scope1, "key1", 5));
            Assert.Equal(5.0, Test(scope1, "key1", -999)); // already cached

            Assert.Equal(7.0, Test(scope1, "key2", 7));

            Assert.Equal(33.0, Test(scope2, "key1", 33)); // new scope, cache miss

            _cache.Reset(scope1);
            Assert.Equal(6.0, Test(scope1, "key1", 6)); // reset, gets new value
            Assert.Equal(33.0, Test(scope2, "key1", -999)); // unchanged
        }

        private double Test(string cacheScope, string requestKey, double callback)
        {
            var r1 = _cache.TryGetAsync(cacheScope, requestKey, () => New(callback)).Result;
            return (double)r1.ToObject();
        }

        public static async Task<FormulaValue> New(double n)
        {
            return FormulaValue.New(n);
        }
    }
}
