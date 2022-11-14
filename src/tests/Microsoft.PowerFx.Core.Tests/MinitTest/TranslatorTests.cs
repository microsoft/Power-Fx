// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Xunit;

namespace Microsoft.PowerFx.Minit
{
    public class TranslatorTests
    {
        [Theory]
        [InlineData("Sum(ProcessEvents, Duration)", "Sum(ProcessEvents, Duration())")]
        public void Test1(string expressionFx, string expectedMinit)
        {
            var engine = new Converter();

            var actual = engine.Convert(expressionFx);

            Assert.Equal(expectedMinit, actual);
        }        
    }
}
