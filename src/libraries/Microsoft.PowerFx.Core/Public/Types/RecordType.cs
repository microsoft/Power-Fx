// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

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
            Contracts.Assert(type.IsRecord || type.IsPolymorphic);
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
        /// <param name="displayNameProvider">Provide DisplayNameProvider to be used.</param>
        public RecordType(DisplayNameProvider displayNameProvider)
            : base(false, displayNameProvider)
        {         
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordType"/> class with <see cref="DisplayNameProvider"/> and <see cref="TableDelegationInfo"/>.
        /// Derived classes calling this must override <see cref="AggregateType.TryGetFieldType(string, out FormulaType)"/>.
        /// </summary>
        /// <param name="displayNameProvider">Provide DisplayNameProvider to be used.</param>
        /// <param name="delegationInfo">Table provider to be used.</param>
        public RecordType(DisplayNameProvider displayNameProvider, TableDelegationInfo delegationInfo)
            : base(false, displayNameProvider)
        {
            _type = DType.AttachDataSourceInfo(_type, new DataSourceInfo(this, displayNameProvider, delegationInfo));            
            _fieldNames = displayNameProvider.LogicalToDisplayPairs.Select(pair => pair.Key.Value).ToList();
        }

        public override IEnumerable<string> FieldNames => _fieldNames;

        // List of fields names that compose the primary key
        // This array is ordered and contains logical names
        public virtual string[] PrimaryKeyNames => Array.Empty<string>();

        private readonly IEnumerable<string> _fieldNames = null;

        /// <summary>
        /// For tooling, return back capabilities of the table.
        /// May return null if the table was not created with <see cref="TableDelegationInfo"/>.
        /// </summary>
        /// <param name="delegationInfo">The delegation info this table was created with.</param>
        /// <returns>true if delegationInfo is not null.</returns>
        public bool TryGetCapabilities(out TableDelegationInfo delegationInfo)
        {
            var ads = _type.AssociatedDataSources.FirstOrDefault();
            if (ads is DataSourceInfo x)
            {
                delegationInfo = x.DelegationInfo;
                return true;
            }            

            delegationInfo = null;
            return false;
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
        /// Static builder method for constructing a Polymorphic record type.
        /// Used for Dataverse Polymorphic types.
        /// </summary>
        public static RecordType Polymorphic()
        {
            return new KnownRecordType(DType.Polymorphic);
        }

        internal override void DefaultExpressionValue(StringBuilder sb)
        {
            var symbolName = TableSymbolName;
            if (symbolName != null)
            {
                // If this is coming from a symbol, we need to reference that. 
                // Get a blank record of the given Symbol type. 
                sb.Append("First(FirstN(");
                sb.Append(IdentToken.MakeValidIdentifier(symbolName));
                sb.Append(",0))");
                return;
            }

            var flag = true;

            sb.Append("{");

            foreach (var field in GetFieldTypes())
            {
                if (!flag)
                {
                    sb.Append(",");
                }

                flag = false;
                
                sb.Append($"{IdentToken.MakeValidIdentifier(field.Name)}:");

                field.Type.DefaultExpressionValue(sb);
            }

            sb.Append("}");
        }
    }
}
