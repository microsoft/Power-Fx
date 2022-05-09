// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core.Texl.Intellisense
{
    /// <summary>
    /// Flags used to control Intellisense bahavior.
    /// </summary>
    [Flags]
    public enum IntellisenseFlags
    {
        /// <summary>
        /// Default bahavior
        /// </summary>
        Default = 0x0,

        /// <summary>
        /// Suggests unqualified enums when possible
        /// </summary>
        SuggestUnqualifiedEnums = 0x1,
    }

    /// <summary>
    /// IntellisenseFlags extensions.
    /// </summary>
    internal static class IntellisenseFlagExtensions
    {
        /// <summary>
        /// Determines if SuggestUnqualifiedEnums flag is enabled.
        /// </summary>
        /// <param name="flags">IntellisenseFlags.</param>
        /// <returns>True if SuggestUnqualifiedEnums is set.</returns>
        public static bool HasSuggestUnqualifiedEnums(this IntellisenseFlags flags)
        {
            return (flags & IntellisenseFlags.SuggestUnqualifiedEnums) == IntellisenseFlags.SuggestUnqualifiedEnums;
        }
    }
}
