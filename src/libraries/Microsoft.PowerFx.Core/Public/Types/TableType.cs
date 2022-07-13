// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    public abstract class TableType : AggregateType
    {
        internal TableType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsTable);
        }

        public TableType()
            : base(true)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

        internal string SingleColumnFieldName
        {
            get
            {
                Contracts.Assert(FieldNames.Count() == 1);
                return FieldNames.First();
            }
        }

        internal FormulaType SingleColumnFieldType => GetFieldType(SingleColumnFieldName);

        public RecordType ToRecord()
        {
            // Unwrap lazy types
            if (DType.IsLazyType && DType.LazyTypeProvider.BackingFormulaType is RecordType record)
            {
                return record;
            }

            return new KnownRecordType(DType.ToRecord());
        }
        
        /// <summary>
        /// By default, adding a field to a TableType requires the type to be fully expanded
        /// Override if your derived class can change that behavior.
        /// </summary>
        /// <param name="field">Field being added.</param>
        public virtual TableType Add(NamedFormulaType field)
        {
            return new KnownTableType(AddFieldToType(field));
        }
        
        /// <summary>
        /// Wrapper for <see cref="Add(NamedFormulaType)"/>.
        /// </summary>
        public TableType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            return Add(new NamedFormulaType(new TypedName(type.DType, new DName(logicalName)), optionalDisplayName));
        }

        public static TableType Empty()
        {
            return new KnownTableType();
        }
    }
}
