// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using Xunit;

namespace Microsoft.PowerFx.Connectors.Tests
{
    [TestCaseOrderer("Microsoft.PowerFx.Connectors.Tests.PriorityOrderer", "Microsoft.PowerFx.Connectors.Tests")]
    public class PriorityOrdererTests
    {
        private static long _i = 0;

        [Theory]
        [TestPriority(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void PriorityOrderer_Test1(int x)
        {
            Interlocked.Add(ref _i, x);
        }

        [Fact]
        [TestPriority(0)]
        public void PriorityOrderer_Test2()
        {
            Interlocked.Increment(ref _i);
        }

        [Fact]
        [TestPriority(1)]
        public void PriorityOrderer_Test()
        {
            // As this test has a priority of 1, it should execute after all other tests with priority 0 (Fact or Theory)
            Assert.Equal(7, Interlocked.Read(ref _i));
        }

        [Fact]
        public void PriorityOrderer_NoPriority()
        {
        }
    }
}
