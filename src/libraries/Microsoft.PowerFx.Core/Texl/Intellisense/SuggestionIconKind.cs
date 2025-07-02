// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// Represents the kind of icon to display for an intellisense suggestion.
    /// </summary>
    public enum SuggestionIconKind
    {
        /// <summary>
        /// Represents a suggestion of an unspecified or other kind.
        /// </summary>
        Other,

        /// <summary>
        /// Represents a function suggestion.
        /// </summary>
        Function,

        /// <summary>
        /// Represents a control suggestion.
        /// </summary>
        Control,

        /// <summary>
        /// Represents a data source suggestion.
        /// </summary>
        DataSource,
    }
}
