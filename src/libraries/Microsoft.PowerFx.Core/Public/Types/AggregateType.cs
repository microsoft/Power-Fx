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
        public IEnumerable<string> FieldNames { get; }

        public ITypeIdentity Identity { get; }

        internal AggregateType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsAggregate);
            FieldNames = DType.GetNames(DPath.Root).Select(typedName => typedName.Name.Value);
        }

        public AggregateType(ITypeIdentity identity, IEnumerable<string> fieldNames, bool isTable)
            : base(DType.ObjNull)
        {
            FieldNames = fieldNames;
            Identity = identity;

            var lazyTypeProvider = new LazyTypeProvider(Identity, FieldNames, TryGetFieldType);
            DType = new DType(lazyTypeProvider, isTable: false);
        }

        public FormulaType GetFieldType(string fieldName)
        {
            return TryGetFieldType(fieldName, out var type) ? 
                type :
                throw new InvalidOperationException($"No field {fieldName}");
        }

        public virtual bool TryGetFieldType(string name, out FormulaType type)
        {
            if (!DType.TryGetType(new DName(name), out var dType))
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
            var displayNameProvider = DType.DisplayNameProvider;
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

            var newType = DType.Add(field._typedName);

            if (displayNameProvider != null)
            {
                newType = DType.ReplaceDisplayNameProvider(newType, displayNameProvider);
            }

            return newType;
        }
    }
}
