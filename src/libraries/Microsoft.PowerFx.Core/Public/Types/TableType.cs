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
        // For DisplayNameSet the Keys are Logical Names, and the Values are Display Names
        internal TableType(DType type, Dictionary<string, string> displayNameSet = null) : base(type, displayNameSet)
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
            var newType = _type.Add(field._typedName);
            return new TableType(newType);
        }

        public TableType WithDisplayNames(Dictionary<string, string> displayNameSet)
        {
            return new TableType(_type, displayNameSet);
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
