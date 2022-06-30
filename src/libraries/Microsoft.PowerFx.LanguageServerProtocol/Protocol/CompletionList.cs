// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class CompletionList
    {
        public CompletionList()
        {
            Items = Array.Empty<CompletionItem>();
        }

        /// <summary>
        /// This list is not complete. Further typing should result in recomputing
        /// this list.
        /// </summary>
        public bool IsIncomplete { get; set; }

        /// <summary>
        /// The completion items.
        /// </summary>
        public IEnumerable<CompletionItem> Items { get; set; }
    }
}
