// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Xunit;

namespace Microsoft.PowerFx.Performance.Tests.UnitTests
{
    public class PerformanceTests
    {
        [Fact]
        public void PerformanceTest()
        {
            var pt = new PerformanceTest1();
            pt.GlobalSetup();

            for (var i = 0; i < 10000; i++)
            {
                var cr = pt.Check();
                Assert.True(cr.IsSuccess);
            }
        }
    }
}
