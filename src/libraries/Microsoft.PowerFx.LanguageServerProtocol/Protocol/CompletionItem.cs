// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class CompletionItem
    {
        public CompletionItem()
        {
            Label = string.Empty;
            Kind = CompletionItemKind.Text;
            Detail = string.Empty;
            Documentation = string.Empty;
            SortText = string.Empty;
            InsertText = string.Empty;
        }

        /// <summary>
        /// The label of this completion item. By default
        /// also the text that is inserted when selecting
        /// this completion.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The kind of this completion item. Based of the kind
        /// an icon is chosen by the editor. The standardized set
        /// of available values is defined in `CompletionItemKind`.
        /// </summary>
        public CompletionItemKind Kind { get; set; }

        /// <summary>
        /// A human-readable string with additional information
        /// about this item, like type or symbol information.
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// A human-readable string that represents a doc-comment.
        /// </summary>
        public string Documentation { get; set; }
        
        /// <summary>
        /// A string that should be used when comparing this item
        /// with other items. When omitted the label is used
        /// as the sort text for this item.
        /// </summary>
        public string SortText { get; set; }
        
        /// <summary>
        /// The text to be inserted into the editor if the completion item is selected.
        /// </summary>
        public string InsertText { get; set; }
    }
}
