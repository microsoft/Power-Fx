// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public abstract class AggregateType : FormulaType
    {
        public virtual IEnumerable<string> FieldNames { get; }

        /// <summary>
        /// Override to add a more specific user-visible type name when this type shows up
        /// in error messages, suggestions, etc..
        /// </summary>
        public virtual string UserVisibleTypeName => null;

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

        // Check for field existence, avoids the overhead of actually building the return type. 
        internal bool HasFieldLogical(string logicalName)
        {
            return _type.TryGetType(new DName(logicalName), out _);
        }

        internal bool HasField(string displayOrLogicalName)
        {
            Contracts.CheckNonEmpty(displayOrLogicalName, nameof(displayOrLogicalName));

            return DType.TryGetDisplayNameForColumn(_type, displayOrLogicalName, out _) ||
                   DType.TryGetLogicalNameForColumn(_type, displayOrLogicalName, out _) ||
                   _type.TryGetType(new DName(displayOrLogicalName), out _);
        }

        /// <summary>
        /// Lookup for logical name and field for input display or logical name.
        /// If there is a conflict, it prioritizes logical name.
        /// i.e. field1->Logical=F1 , Display=Display1; field2-> Logical=Display1, Display=Display2
        /// would return field2 with logical name Display1.
        /// </summary>
        /// <param name="displayOrLogicalName">Display or Logical name.</param>
        /// <param name="logical">Logical name for the input.</param>
        /// <param name="type">Type for the input Display or Logical name.</param>
        /// <returns>true or false.</returns>
        /// <exception cref="ArgumentNullException">Throws, if input displayOrLogicalName is empty.</exception>
        public bool TryGetFieldType(string displayOrLogicalName, out string logical, out FormulaType type)
        {
            Contracts.CheckNonEmpty(displayOrLogicalName, nameof(displayOrLogicalName));

            if (DType.TryGetDisplayNameForColumn(_type, displayOrLogicalName, out _))
            {
                logical = displayOrLogicalName;
            }
            else if (DType.TryGetLogicalNameForColumn(_type, displayOrLogicalName, out var maybeLogical))
            {
                logical = maybeLogical;
            }
            else
            {
                // in-case derived types did not provide DisplayNameProvider then above two will be false
                // but we want to assume that, provided displayOrLogicalName maybe logical name it self
                // and let TryGetFieldType verify that.
                logical = displayOrLogicalName;
            }

            if (!TryGetFieldType(logical, out type))
            {
                logical = null;
                return false;
            }

            return true;
        }

        public IEnumerable<NamedFormulaType> GetFieldTypes()
        {
            return FieldNames.Select(field =>
            {
                var fieldType = GetFieldType(field);
                
                var displayName = DType.TryGetDisplayNameForColumn(_type, field, out var dName)
                    ? dName
                    : null;
                return new NamedFormulaType(field, GetFieldType(field), displayName);
            });
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

        /// <summary>
        /// Get a symbol name - which is the name this was added with in the symbol table. 
        /// This may be null. 
        /// This may often be a Display Name or whatever the host assigned, like "Accounts_2".
        /// </summary>
        public virtual string TableSymbolName
        {
            get
            {
                var ds = _type.AssociatedDataSources.FirstOrDefault();

                if (ds != null)
                {
                    return ds.EntityName.Value;
                }

                return null;
            }
        }
    }
}
