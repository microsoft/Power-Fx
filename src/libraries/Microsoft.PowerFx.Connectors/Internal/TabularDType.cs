// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Connectors
{
    // Used in TabularIRVisitor
    internal class TabularDType : DType
    {
        internal RecordType RecordType;

        internal TabularDType(RecordType recordType)
            : base(DKind.Table, recordType._type.ToTable().TypeTree, null, recordType._type.DisplayNameProvider)
        {
            RecordType = recordType;
        }
    }
}
