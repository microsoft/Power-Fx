// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx
{
    [Flags]
    public enum Features : int
    {
        None = 0x0,

        /// <summary>
        /// Enable Table syntax to not add "Value:" extra layer.
        /// Added on 1st July 2022.
        /// </summary>
        TableSyntaxDoesntWrapRecords = 0x1,

        /// <summary>
        /// Enable functions to consistently return one dimension tables with a "Value" column rather than some other name like "Result"
        /// </summary>
        ConsistentOneColumnTableResult = 0x2,

        /// <summary>
        /// Disables support for row-scope disambiguation syntax.
        /// Now,for example user would need to use Filter(A, ThisRecord.Value = 2) or Filter(A As Foo, Foo.Value = 2)
        /// instead of
        /// Filter(A, A[@Value] = 2)
        /// </summary>
        DisableRowScopeDisambiguationSyntax = 0x4,

        /// <summary>
        /// All features enabled
        /// [USE WITH CAUTION] In using this value, you expose your code to future features.
        /// </summary>
        All = ~0
    }
}
