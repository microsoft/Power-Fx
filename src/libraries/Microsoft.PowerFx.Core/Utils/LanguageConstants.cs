// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Core.Utils
{
    internal class LanguageConstants
    {
        /// <summary>
        /// The string value representing SortOrder enum.
        /// </summary>
        public static string SortOrderEnumString => StringResources.Get("LocaleSpecificEnum_SortOrder");

        /// <summary>
        /// Defines ascending sort order string constant.
        /// </summary>
        internal const string AscendingSortOrderString = "ascending";

        /// <summary>
        /// Defines descending sort order string constant.
        /// </summary>
        internal const string DescendingSortOrderString = "descending";

        /// <summary>
        /// The string value representing the locale invariant calendar function namespace.
        /// </summary>
        internal const string InvariantCalendarNamespace = "Calendar";

        /// <summary>
        /// The string value representing the locale invariant clock function namespace.
        /// </summary>
        internal const string InvariantClockNamespace = "Clock";

        /// <summary>
        /// The invariant string value representing SortOrder enum.
        /// </summary>
        internal static string SortOrderEnumStringInvariant => "SortOrder";
    }
}
