// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{       
    /// <summary>
    /// Represents a table type within Power Fx. 
    /// Build using <see cref="Empty()"/> and <see cref="Add(NamedFormulaType)"/>
    /// or call <see cref="RecordType.ToTable()"/> on a derived table type.
    /// </summary>
    public sealed class TableType : AggregateType
    {
        public override IEnumerable<string> FieldNames => _type.GetRootFieldNames().Select(name => name.Value);

        internal TableType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsTable);
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }

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
        /// Adding a field to a TableType requires the type to be fully expanded.
        /// </summary>
        /// <param name="field">Field being added.</param>
        public TableType Add(NamedFormulaType field)
        {
            return new TableType(AddFieldToType(field));
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
            return new TableType(DType.EmptyTable);
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

        public override bool Equals(object other)
        {
            if (other is not TableType otherTableType)
            {
                return false;
            }

            if (_type.IsLazyType && otherTableType._type.IsLazyType && _type.IsTable == otherTableType._type.IsTable)
            {
                return _type.LazyTypeProvider.BackingFormulaType.Equals(otherTableType._type.LazyTypeProvider.BackingFormulaType);
            }

            return _type.Equals(otherTableType._type);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }        
    }
}
