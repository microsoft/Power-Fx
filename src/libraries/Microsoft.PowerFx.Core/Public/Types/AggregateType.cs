// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public abstract class AggregateType : FormulaType
    {
        internal AggregateType(DType type)
            : base(type)
        {
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

        // Enumerate fields
        public IEnumerable<NamedFormulaType> GetNames()
        {
            var names = _type.GetAllNames(DPath.Root);
            return from name in names select new NamedFormulaType(name);
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

        internal FormulaType RenameFormulaTypeHelper(Queue<DName> segments, DName updatedName)
        {
            var field = segments.Dequeue();
            if (segments.Count == 0)
            {
                // Create a display name provider with only the name in question
                var names = new Dictionary<DName, DName>
                {
                    [field] = updatedName
                };
                var newProvider = new SingleSourceDisplayNameProvider(names);

                return Build(DType.ReplaceDisplayNameProvider(_type, newProvider));
            }

            var fieldType = MaybeGetFieldType(field);
            if (fieldType is not AggregateType aggregateType)
            {
                // Path doesn't exist within parameters, return as is
                return this;
            }

            var updatedType = aggregateType.RenameFormulaTypeHelper(segments, updatedName);
            var fError = false;

            // Use some fancy DType internals to swap one field type for the updated one
            var dropped = _type.Drop(ref fError, DPath.Root, field);
            Contracts.Assert(!fError);

            return Build(dropped.Add(field, updatedType._type));
        }
    }
}
