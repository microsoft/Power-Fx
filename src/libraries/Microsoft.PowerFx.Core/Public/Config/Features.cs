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
        /// Added on 11th July 2022
        /// </summary>
        ConsistentOneColumnTableResult = 0x2,

        /// <summary>
        /// Enable Identifier support for describing column names
        /// Added on 20ieth July 2022.
        /// </summary>
        SupportIdentifiers = 0x4,
    }
}
