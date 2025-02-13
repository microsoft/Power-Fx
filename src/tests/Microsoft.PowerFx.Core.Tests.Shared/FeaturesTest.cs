// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Xunit;

namespace Microsoft.PowerFx.Tests
{
    public class FeaturesTest
    {
        [Fact]
        public void Singleton()
        {
            // Ensure that we have a singleton - this is for performance reasons.
            Assert.True(object.ReferenceEquals(Features.PowerFxV1, Features.PowerFxV1));

            // Equality checks
            Assert.True(Features.PowerFxV1 == Features.PowerFxV1);
            Assert.False(Features.PowerFxV1 != Features.PowerFxV1);

            Assert.True(object.ReferenceEquals(Features.None, Features.None));
        }

        [Fact]
        public void Flag()
        {
            // Ensure the V1 object is actually initialized. 
            var v1 = Features.PowerFxV1;
            Assert.True(v1.SupportColumnNamesAsIdentifiers);
        }
    }
}
