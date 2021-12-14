// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class RecordType : AggregateType
    {
        // For DisplayNameSet the Keys are Logical Names, and the Values are Display Names
        internal RecordType(DType type, Dictionary<string, string> displayNameSet = null) : base(type, displayNameSet)
        {
            Contract.Assert(type.IsRecord);
        }

        public RecordType() : base(DType.EmptyRecord)
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

        public RecordType Add(string name, FormulaType type)
        {
            return Add(new NamedFormulaType(new TypedName(type._type, new DName(name))));
        }

        public RecordType WithDisplayNames(Dictionary<string, string> displayNameSet)
        {
            return new RecordType(_type, displayNameSet);
        }

        public TableType ToTable()
        {
            return new TableType(this._type.ToTable());
        }

        public FormulaType MaybeGetFieldType(string fieldName)
        {
            // $$$ Better lookup
            foreach (var field in this.GetNames())
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
