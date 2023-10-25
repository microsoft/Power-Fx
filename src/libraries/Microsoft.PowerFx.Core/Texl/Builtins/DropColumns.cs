// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // DropColumns(source:*[...], name:s, name:s, ...)
    // DropColumns(source:![...], name:s, name:s, ...)
    internal class DropColumnsFunction : ShowDropColumnsFunctionBase
    {
        public DropColumnsFunction()
            : base(false)
        {
        }
    }
}
