// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class CompletionList
    {
        public CompletionList()
        {
            Items = new CompletionItem[] { };
        }

        /// <summary>
        /// This list is not complete. Further typing should result in recomputing
        /// this list.
        /// </summary>
        public bool IsIncomplete { get; set; }

        /// <summary>
        /// The completion items.
        /// </summary>
        public CompletionItem[] Items { get; set; }
    }
}
