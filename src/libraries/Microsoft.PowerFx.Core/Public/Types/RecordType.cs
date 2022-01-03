﻿// Copyright (c) Microsoft Corporation.
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
        internal RecordType(DType type) : base(type)
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
            var displayNameProvider = _type.DisplayNameProvider;
            if (field.DisplayName != default)
            {
                if (displayNameProvider == null)
                    displayNameProvider = new DisplayNameProvider();
                
                displayNameProvider = displayNameProvider.AddField(field.Name, field.DisplayName);
            }

            var newType = _type.Add(field._typedName);
            if (displayNameProvider != null)
            {
                newType = DType.ReplaceDisplayNameProvider(newType, displayNameProvider);
            }

            return new RecordType(newType);
        }

        public RecordType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            if (optionalDisplayName != null)
                return Add(new NamedFormulaType(new TypedName(type._type, new DName(logicalName)), new DName(optionalDisplayName)));
            return Add(new NamedFormulaType(new TypedName(type._type, new DName(logicalName))));
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
