// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.ContractsUtils;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Logging.Trackers
{
    internal interface IAddSuggestionMessageEventArgs
    {
        string Message { get; }

        TexlNode Node { get; }

        TexlBinding Binding { get; }
    }

    internal class AddSuggestionMessageEventArgs : IAddSuggestionMessageEventArgs
    {
        public string Message { get; }

        public TexlNode Node { get; }

        public TexlBinding Binding { get; }

        public AddSuggestionMessageEventArgs(string message, TexlNode node, TexlBinding binding)
        {
            Contracts.AssertValue(message);
            Contracts.AssertValue(node);
            Contracts.AssertValue(binding);

            Message = message;
            Node = node;
            Binding = binding;
        }
    }
}
