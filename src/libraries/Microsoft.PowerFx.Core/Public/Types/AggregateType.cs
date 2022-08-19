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
        /// <summary>
        /// Enumerable of logical field names in this type.
        /// </summary>
        public virtual IEnumerable<string> FieldNames { get; }

        internal AggregateType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsAggregate);
        }

        internal AggregateType(bool isTable)
            : base()
        {
            var lazyTypeProvider = new LazyTypeProvider(this);
            var displayNameProvider = new PassThroughDisplayNameProvider(this);
            _type = new DType(lazyTypeProvider, displayNameProvider, isTable: isTable);
        }

        public FormulaType GetFieldType(string fieldName)
        {
            return TryGetFieldType(fieldName, out var type) ? 
                type :
                throw new InvalidOperationException($"No field {fieldName}");
        }

        /// <summary>
        /// Lookup a field by logical name.
        /// </summary>
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

        /// <summary>
        /// Lookup from display names to logical names. 
        /// Derived classes that implement this must also implement
        /// <see cref="TryGetDisplayName(string, out string)"/>.
        /// </summary>
        public virtual bool TryGetLogicalName(string displayName, out string logicalName)
        {
            // This check helps avoid infinite lookup loops
            if (_type.DisplayNameProvider is not PassThroughDisplayNameProvider && 
                _type.DisplayNameProvider.TryGetLogicalName(new DName(displayName), out var logicalDName))
            {
                logicalName = logicalDName.Value;
                return true;
            }

            logicalName = default;
            return false;
        }
        
        /// <summary>
        /// Lookup from logical names to display names. 
        /// Derived classes that implement this must also implement
        /// <see cref="TryGetLogicalName(string, out string)"/>.
        /// </summary>
        public virtual bool TryGetDisplayName(string logicalName, out string displayName)
        {
            // This check helps avoid infinite lookup loops
            if (_type.DisplayNameProvider is not PassThroughDisplayNameProvider &&
                _type.DisplayNameProvider.TryGetDisplayName(new DName(logicalName), out var displayDName))
            {
                displayName = displayDName.Value;
                return true;
            }

            displayName = default;
            return false;
        }

        public abstract override bool Equals(object other);

        public abstract override int GetHashCode();

        // Keeping around to resolve a diamond dependency issue, remove once FormulaRepair is updated
        [Obsolete("This method was replaced with GetFieldTypes", true)]
        public IEnumerable<NamedFormulaType> GetNames() => GetFieldTypes();
    }
}
