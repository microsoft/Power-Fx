// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core
{
    /// <summary>
    /// PowerFx Flags.
    /// </summary>
    [Flags]
    public enum PowerFxFlags : uint
    {
        /// <summary>
        /// No flag, only use defaults
        /// </summary>
        None = 0,

        /// <summary>
        /// Enable parser expression chaining
        /// </summary>
        EnableExpressionChaining = 1
    }

    /// <summary>
    /// PowerFxFlags extensions
    /// </summary>
#pragma warning disable SA1649 // File name should match first type name
    public static class PowerFxFlagsExtensions
#pragma warning restore SA1649 // File name should match first type name
    {
        /// <summary>
        /// Returns true if EnableExpressionChaining is enabled.
        /// </summary>
        /// <param name="flags">PowerFxFlags.</param>
        /// <returns></returns>
        public static bool HasEnableExpressionChaining(this PowerFxFlags flags)
        {
            return (flags & PowerFxFlags.EnableExpressionChaining) != 0;
        }
    }
}
