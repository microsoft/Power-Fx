﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public class RecordType : AggregateType
    {
        internal RecordType(DType type)
            : base(type)
        {
            Contract.Assert(type.IsRecord);
        }

        public RecordType()
            : base(DType.EmptyRecord)
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        public RecordType Add(NamedFormulaType field)
        {
            return new RecordType(AddFieldToType(field));
        }

        public RecordType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            return Add(new NamedFormulaType(new TypedName(type._type, new DName(logicalName)), optionalDisplayName));
        }

        public TableType ToTable()
        {
            return new TableType(_type.ToTable());
        }
    }
}
