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
    /// <summary>
    /// Represents a Record type within PowerFx. If this is subclassed, it's quite likely that 
    /// <see cref="RecordValue"/> should be as well. 
    /// If the type is known in advance and easy to construct, use <see cref="Empty"/> and <see cref="Add(NamedFormulaType)"/>
    /// instead of deriving from this.
    /// </summary>
    public abstract class RecordType : AggregateType
    {
        // The internal constructor allows us to wrap known DTypes, while the public constructor
        // will create a DType that wraps derived TryGetField/Fields calls
        internal RecordType(DType type)
            : base(type)
        {
            Contracts.Assert(type.IsRecord);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordType"/> class.
        /// Derived classes calling this must override <see cref="AggregateType.FieldNames"/>
        /// and <see cref="AggregateType.TryGetFieldType(string, out FormulaType)"/>.
        /// </summary>
        public RecordType() 
            : base(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordType"/> class with <see cref="DisplayNameProvider"/>.
        /// Derived classes calling this must override <see cref="AggregateType.FieldNames"/>
        /// and <see cref="AggregateType.TryGetFieldType(string, out FormulaType)"/>.
        /// </summary>
        /// <param name="displayNameProvider">Provide DispayNamerovide to be used.</param>
        public RecordType(DisplayNameProvider displayNameProvider)
            : base(false, displayNameProvider)
        {
        }

        public override void Visit(ITypeVisitor vistor)
        {
            vistor.Visit(this);
        }
        
        /// <summary>
        /// Converts this type to a table.
        /// If this type was a record of a derived TableType,
        /// returns the derived TableType.
        /// </summary>
        public TableType ToTable()
        {
            // Unwrap lazy types
            if (_type.IsLazyType && _type.LazyTypeProvider.BackingFormulaType is TableType table)
            {
                return table;
            }
            
            return new TableType(_type.ToTable());
        }

        /// <summary>
        /// By default, adding a field to a TableType requires the type to be fully expanded
        /// Override if your derived class can change that behavior.
        /// </summary>
        /// <param name="field">Field being added.</param>
        public virtual RecordType Add(NamedFormulaType field)
        {
            return new KnownRecordType(AddFieldToType(field));
        }

        /// <summary>
        /// Wrapper for <see cref="Add(NamedFormulaType)"/>.
        /// </summary>
        public RecordType Add(string logicalName, FormulaType type, string optionalDisplayName = null)
        {
            return Add(new NamedFormulaType(new TypedName(type._type, new DName(logicalName)), optionalDisplayName));
        }

        /// <summary>
        /// Static builder method for constructing a record type. 
        /// Use with <see cref="Add(NamedFormulaType)"/> to construct a
        /// record type.
        /// </summary>
        /// <returns>An empty RecordType instance.</returns>
        public static RecordType Empty()
        {
            return new KnownRecordType();
        }

        /// <summary>
        /// Resolve logical names when display names are given.
        /// </summary>
        /// <param name="recordToResolve">RecordValue to resolve.</param>
        /// <returns>RecordValue containing only logical field names.</returns>
        internal RecordValue ResolveToLogicalNames(RecordValue recordToResolve)
        {
            var list = new List<NamedValue>();
            var rType = recordToResolve.Type;

            foreach (var field in recordToResolve.Fields)
            {
                var name = field.Name;

                if (DType.TryGetLogicalNameForColumn(_type, field.Name, out var logicalName))
                {
                    name = logicalName;
                }

                list.Add(new NamedValue(name, field.Value));
            }

            return FormulaValue.NewRecordFromFields(list);
        }
    }
}
