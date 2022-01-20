// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Errors
{
    [TransportType(TransportKind.ByValue)]
    internal interface IErrorHelpLink
    {
        string DisplayText { get; }

        string Url { get; }
    }

    internal sealed class ErrorHelpLink : IErrorHelpLink
    {
        public string DisplayText { get; }

        public string Url { get; }

        public ErrorHelpLink(string displayText, string url)
        {
            Contracts.AssertNonEmpty(displayText);
            Contracts.AssertNonEmpty(url);

            DisplayText = displayText;
            Url = url;
        }
    }
}
