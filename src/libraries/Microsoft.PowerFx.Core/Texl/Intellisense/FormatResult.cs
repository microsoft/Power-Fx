// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    internal sealed class FormatResult
    {
        public string ResultText { get; }

        public FormatResult(string text)
        {
            ResultText = text;
        }
    }
}
