// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Types
{
    /// <summary>
    /// You can refer to a field name both by a "logical" name and a "display" name. Sometimes
    /// it's important to distinguish which kind of name you are requesting, to avoid ambiguous
    /// strings.
    /// </summary>
    internal enum FieldNameKind
    {
        Display,
        Logical
    }
}