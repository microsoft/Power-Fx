// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Public.Types
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
            var newType = _type.Add(field._typedName);
            return new RecordType(newType);
        }

        public TableType ToTable()
        {
            return new TableType(_type.ToTable());
        }

        public FormulaType MaybeGetFieldType(string fieldName)
        {
            // $$$ Better lookup
            foreach (var field in GetNames())
            {
                if (field.Name == fieldName)
                {
                    return field.Type;
                }
            }

            return null;
        }

        public FormulaType GetFieldType(string fieldName)
        {
            return MaybeGetFieldType(fieldName) ??
                throw new InvalidOperationException($"No field {fieldName}");
        }
    }
}
