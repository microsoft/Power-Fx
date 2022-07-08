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
        public virtual IAggregateTypeIdentity Identity { get; }

        internal AggregateType(DType type)
            : base(type)
        {
        }

        public FormulaType GetFieldType(string fieldName)
        {
            return TryGetFieldType(fieldName, out var type) ? 
                type :
                throw new InvalidOperationException($"No field {fieldName}");
        }

        public virtual IEnumerable<string> FieldNames => Type.GetNames(DPath.Root).Select(typedName => typedName.Name.Value);

        public virtual bool TryGetFieldType(string name, out FormulaType type)
        {
            if (!Type.TryGetType(new DName(name), out var dType))
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
            var displayNameProvider = Type.DisplayNameProvider;
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

            var newType = Type.Add(field._typedName);

            if (displayNameProvider != null)
            {
                newType = DType.ReplaceDisplayNameProvider(newType, displayNameProvider);
            }

            return newType;
        }
    }
}
