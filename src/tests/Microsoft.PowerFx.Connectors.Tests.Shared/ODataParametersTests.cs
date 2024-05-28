// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.PowerFx.Connectors;
using Microsoft.PowerFx.Connectors.Tests;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Tests
{
    public class ODataParametersTests
    {
        [Fact]
        public void Test()
        {
            var od = new ODataParameters
            {
                Filter = "x gt 5",
                Top = 10,
            };

            var str = od.ToQueryString();

            Assert.Equal("$filter=x+gt+5&$top=10", str);
        }

        private static bool IsSingleBitSet(int i)
        {
            // Single bit set means it's a power of two. 
            return i != 0 && (i & (i - 1)) == 0;
        }

        [Fact]
        public void FeatureFlags()
        {
            HashSet<DelegationParameterFeatures> set = new HashSet<DelegationParameterFeatures>();

            // Each flag should be a single bit. 
            foreach (DelegationParameterFeatures flag in Enum.GetValues(typeof(DelegationParameterFeatures)))
            {
                int i = (int)flag;
                Assert.True(IsSingleBitSet(i));

                // Unique. 
                var added = set.Add(flag);
                Assert.True(added);
            }
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(5, true)]
        [InlineData(0x1000005, false)]
        [InlineData(0x1000000, false)] // Future unknown capabilitiy
        public void Capabilities(int flag, bool succeed)
        {
            var delegation = new TestDelegationParameters
            {
                _features = (DelegationParameterFeatures)flag
            };

            if (succeed)
            {
                delegation.ToOdataParameters();
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => delegation.ToOdataParameters());
            }
        }

        [Fact]
        public void TestToOdataParametersSelect()
        {
            var delegation = new TestDelegationParameters()
            {
                _columns = new string[] { "c1", "c2" },
                _features = DelegationParameterFeatures.Columns
            };

            var od = delegation.ToOdataParameters();

            var str = od.ToQueryString();

            Assert.Equal("$select=c1,c2", str);
        }

        [Fact]
        public void TestToOdataParametersSelect2()
        {
            var delegation = new TestDelegationParameters()
            {
                _columns = new string[0],  // 0-length
                _features = DelegationParameterFeatures.Columns
            };

            var od = delegation.ToOdataParameters();

            var str = od.ToQueryString();

            // 0-length not included. 
            Assert.Equal(string.Empty, str);
        }

        [Fact]
        public void TestToOdataParametersFilterTop()
        {
            var delegation = new TestDelegationParameters()
            {
                _columns = null,
                _features = DelegationParameterFeatures.Columns | DelegationParameterFeatures.Filter | DelegationParameterFeatures.Top,
                _filter = "score gt 5",
                Top = 10
            };

            var od = delegation.ToOdataParameters();

            var str = od.ToQueryString();

            Assert.Equal("$filter=score+gt+5&$top=10", str);
        }

        private class TestDelegationParameters : DelegationParameters
        {
            public string[] _columns;
            public string _filter;
            public DelegationParameterFeatures _features;

            public override DelegationParameterFeatures Features =>
                _features;

            public override IReadOnlyCollection<string> GetColumns()
            {
                return _columns;
            }

            public override string GetOdataFilter()
            {
                return _filter;
            }
        }
    }
}
