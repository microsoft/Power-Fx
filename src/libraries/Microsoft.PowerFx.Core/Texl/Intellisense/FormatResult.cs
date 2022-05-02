﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
{
    internal sealed class FormatResult
    {
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public string resultText { get; }
#pragma warning restore SA1300 // Element should begin with upper-case letter

        public FormatResult(string text)
        {
            resultText = text;
        }
    }
}
