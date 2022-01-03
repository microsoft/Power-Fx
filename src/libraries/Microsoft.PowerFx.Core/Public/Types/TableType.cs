// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public class TableType : AggregateType
    {
        internal TableType(DType type) : base(type)
        {
            Contract.Assert(type.IsTable);
        }

        public TableType() : base(DType.EmptyTable)
        {
        }

        internal static TableType FromRecord(RecordType type)
        {
            var tableType = type._type.ToTable();
            return new TableType(tableType);
        }


        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        public TableType Add(NamedFormulaType field)
        {
            var displayNameProvider = _type.DisplayNameProvider;
            if (field.DisplayName != default)
            {
                if (displayNameProvider == null)
                    displayNameProvider = new DisplayNameProvider();
                else 
                    displayNameProvider = _type.DisplayNameProvider.Clone();


                if (!displayNameProvider.TryAddField(field.Name, field.DisplayName))
                    throw new NameCollisionException(field.DisplayName);
            }

            var newType = _type.Add(field._typedName);

            if (displayNameProvider != null)
            {
                newType = DType.ReplaceDisplayNameProvider(newType, displayNameProvider);
            }

            return new TableType(newType);
        }

        public string SingleColumnFieldName
        {
            get
            {
                Contracts.Assert(GetNames().Count() == 1);
                return GetNames().First().Name;
            }
        }


        public FormulaType SingleColumnFieldType
        {
            get
            {
                Contracts.Assert(GetNames().Count() == 1);
                return GetNames().First().Type;
            }
        }

        public RecordType ToRecord()
        {
            return new RecordType(this._type.ToRecord());
        }
    }
}
