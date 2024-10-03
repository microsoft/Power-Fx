// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    // This interface is used to retrieve field types without following relationships
    // It is only used in RecordType constructor where relationships must not be followed
    // and to generate the LazyProvider backing record type
    public interface ITabularFieldAccessor
    {
        bool TryGetFieldType(string fieldName, out FormulaType type);
    }
}
