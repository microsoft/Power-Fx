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

        /// <summary>
        /// Initializes a new instance of the <see cref="TableType"/> class.
        /// Derived classes calling this must override <see cref="AggregateType.FieldNames"/>
        /// and <see cref="AggregateType.TryGetFieldType(string, out FormulaType)"/>.
        /// </summary>
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
        
        /// <summary>
        /// Converts this type to a table.
        /// If this type was a Table of a derived RecordType,
        /// returns the derived RecordType.
        /// </summary>
        public RecordType ToRecord()
        {
            // Unwrap lazy types
            if (_type.IsLazyType && _type.LazyTypeProvider.BackingFormulaType is RecordType record)
            {
                return record;
            }

            return new KnownRecordType(_type.ToRecord());
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
            return Add(new NamedFormulaType(new TypedName(type._type, new DName(logicalName)), optionalDisplayName));
        }

        /// <summary>
        /// Static builder method for constructing a TableType. 
        /// Use with <see cref="Add(NamedFormulaType)"/> to construct a
        /// Table type.
        /// </summary>
        /// <returns>An empty TableType instance.</returns>
        public static TableType Empty()
        {
            return new KnownTableType();
        }
    }
}
