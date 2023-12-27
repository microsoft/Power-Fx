// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Intellisense.SignatureHelp;
using Xunit;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    public class DisclaimerProviderTests
    {
        [Fact]
        public void Tests()
        {
            var ai = new DisclaimerProvider(null);
            var str = ai.DisclaimerMarkdown.Markdown;

            // We don't know excatly what text will be, but we can sanity check. 
            Assert.StartsWith("**Disclaimer:** AI-generated content", str);
        }
    }
}
