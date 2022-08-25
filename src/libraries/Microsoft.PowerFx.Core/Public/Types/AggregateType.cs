// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public abstract class AggregateType : FormulaType
    {
        public virtual IEnumerable<string> FieldNames { get; }

        internal AggregateType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsAggregate);
        }

        public AggregateType(bool isTable)
            : this(isTable, null)
        {
        }

        public AggregateType(bool isTable, DisplayNameProvider displayNameProvider)
            : base()
        {
            var lazyTypeProvider = new LazyTypeProvider(this);
            _type = new DType(lazyTypeProvider, isTable: isTable, displayNameProvider);
        }

        public FormulaType GetFieldType(string fieldName)
        {
            return TryGetFieldType(fieldName, out var type) ? 
                type :
                throw new InvalidOperationException($"No field {fieldName}");
        }

        public virtual bool TryGetFieldType(string name, out FormulaType type)
        {
            if (!_type.TryGetType(new DName(name), out var dType))
            {
                type = Blank;
                return false;
            }

            type = Build(dType);
            return true;
        }

        public IEnumerable<NamedFormulaType> GetFieldTypes()
        {
            return FieldNames.Select(field => new NamedFormulaType(field, GetFieldType(field)));
        }

        private protected DType AddFieldToType(NamedFormulaType field)
        {
            var displayNameProvider = _type.DisplayNameProvider;
            if (displayNameProvider == null)
            {
                displayNameProvider = new SingleSourceDisplayNameProvider();
            }

            if (displayNameProvider is SingleSourceDisplayNameProvider singleSourceDisplayNameProvider)
            {
                if (field.DisplayName != default)
                {
                    displayNameProvider = singleSourceDisplayNameProvider.AddField(field.Name, field.DisplayName);
                }
            }

            var newType = _type.Add(field._typedName);

            if (displayNameProvider != null)
            {
                newType = DType.ReplaceDisplayNameProvider(newType, displayNameProvider);
            }

            return newType;
        }

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        // Keeping around to resolve a diamond dependency issue, remove once FormulaRepair is updated
        [Obsolete("This method was replaced with GetFieldTypes", true)]
        public IEnumerable<NamedFormulaType> GetNames() => GetFieldTypes();
    }
}
