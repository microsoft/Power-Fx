﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Core.Public.Config
{
    [Flags]
    public enum Feature : int
    {
        None = 0x0,

        /// <summary>
        /// Enable Table syntax to not add "Value:" extra layer.
        /// Added on 1st July 2022.
        /// </summary>
        TableSyntaxDoesntWrapRecords = 0x1      
    }
}
