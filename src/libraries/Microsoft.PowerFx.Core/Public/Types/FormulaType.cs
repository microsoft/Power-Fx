// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Base class for type of a Formula. 
    /// Formula Types are a class hiearchy.
    /// </summary>
    [DebuggerDisplay("{_type}")]
    [ThreadSafeImmutable]
    public abstract class FormulaType
    {
#pragma warning disable SA1300 // Element should begin with upper-case letter
        // Uses init to allow setting from derived constructors. Otherwise, is immutable.
        internal DType _type { get; private protected init; }
#pragma warning restore SA1300 // Element should begin with upper-case letter

        public static FormulaType Blank { get; } = new BlankType();

        // Well-known types 
        public static FormulaType Boolean { get; } = new BooleanType();

        public static FormulaType Number { get; } = new NumberType();

        public static FormulaType String { get; } = new StringType();

        public static FormulaType Time { get; } = new TimeType();

        public static FormulaType Date { get; } = new DateType();

        public static FormulaType DateTime { get; } = new DateTimeType();

        public static FormulaType DateTimeNoTimeZone { get; } = new DateTimeNoTimeZoneType();

        public static FormulaType UntypedObject { get; } = new UntypedObjectType();

        public static FormulaType Hyperlink { get; } = new HyperlinkType();

        public static FormulaType Color { get; } = new ColorType();

        public static FormulaType Guid { get; } = new GuidType();

        public static FormulaType Unknown { get; } = new UnknownType();

        public static FormulaType BindingError { get; } = new BindingErrorType();
        
        /// <summary>
        /// Internal use only to represent an arbitrary (un-backed) option set value.
        /// Should be removed if possible.
        /// </summary>
        internal static FormulaType OptionSetValue { get; } = new OptionSetValueType();

        // chained by derived type 
        internal FormulaType(DType type)
        {
            _type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaType"/> class.
        /// Used for subclasses that must set DType themselves.
        /// </summary>
        private protected FormulaType()
        {            
        }

        // Entites may be recursive and their Dytype is tagged with additional schema metadata. 
        // Expand that metadata into a proper Dtype. 
        private static DType GetExpandedEntityType(DType expandEntityType, string relatedEntityPath)
        {
            Contracts.AssertValid(expandEntityType);
            Contracts.Assert(expandEntityType.HasExpandInfo);
            Contracts.AssertValue(relatedEntityPath);

            var expandEntityInfo = expandEntityType.ExpandInfo;

            if (expandEntityInfo.ParentDataSource is not IExternalTabularDataSource dsInfo)
            {
                return expandEntityType;
            }                        

            if (!expandEntityType.TryGetEntityDelegationMetadata(out var metadata))
            {
                // We need more metadata to bind this fully
                return DType.Error;
            }

            var type = expandEntityType.ExpandEntityType(metadata.Schema, metadata.Schema.AssociatedDataSources);
            Contracts.Assert(type.HasExpandInfo);

            // Update the datasource and relatedEntity path.
            type.ExpandInfo.UpdateEntityInfo(expandEntityInfo.ParentDataSource, relatedEntityPath);

            return type;
        }

        internal static FormulaType[] GetValidUDFPrimitiveTypes()
        {
            FormulaType[] validTypes = { Blank, Boolean, Number, String, Time, Date, DateTime, DateTimeNoTimeZone, Hyperlink, Color, Guid };
            return validTypes;
        }

        internal static FormulaType GetFromStringOrNull(string formula)
        {
            foreach (FormulaType formulaType in GetValidUDFPrimitiveTypes())
            {
                if (formulaType.ToString().Equals(formula))
                {
                    return formulaType;
                }
            }

            return null;
        }

        // Get the correct derived type
        internal static FormulaType Build(DType type)
        {
            if (type.IsExpandEntity)
            {
                var expandedType = GetExpandedEntityType(type, string.Empty);
                return Build(expandedType);
            }

            switch (type.Kind)
            {
                case DKind.ObjNull: return Blank;
                case DKind.Record:
                    return new KnownRecordType(type);
                case DKind.Table:
                    return new TableType(type);
                case DKind.Number: return Number;
                case DKind.String: return String;
                case DKind.Boolean: return Boolean;
                case DKind.Currency: return Number; // TODO: validate
                case DKind.Hyperlink: return Hyperlink;
                case DKind.Color: return Color;
                case DKind.Guid: return Guid;

                case DKind.Time: return Time;
                case DKind.Date: return Date;
                case DKind.DateTime: return DateTime;
                case DKind.DateTimeNoTimeZone: return DateTimeNoTimeZone;

                case DKind.OptionSetValue:
                    var isBoolean = type.OptionSetInfo?.IsBooleanValued;
                    if (isBoolean.HasValue && isBoolean.Value)
                    {
                        return Boolean;
                    }
                    else
                    {
                        // In all non-test cases, this option set info must be present
                        // For some existing tests, it isn't available. Once that's resolved, this should be cleaned up
                        return type.OptionSetInfo != null ? new OptionSetValueType(type.OptionSetInfo) : OptionSetValue;
                    }

                // This isn't quite right, but once we're in the IR, an option set acts more like a record with optionsetvalue fields. 
                case DKind.OptionSet:
                    return new KnownRecordType(DType.CreateRecord(type.GetAllNames(DPath.Root)));

                case DKind.UntypedObject:
                    return UntypedObject;

                case DKind.Unknown:
                    return Unknown;

                case DKind.Error:
                    return BindingError;
                    
                case DKind.LazyRecord:
                    if (type.LazyTypeProvider.BackingFormulaType is RecordType record)
                    {
                        // For Build calls, if the type is actually defined by a derived FormulaType, we return the derived instance.
                        return record;
                    }

                    return new KnownRecordType(type);
                    
                case DKind.LazyTable:
                    if (type.LazyTypeProvider.BackingFormulaType is TableType table)
                    {
                        // For Build calls, if the type is actually defined by a derived FormulaType, we return the derived instance.
                        return table;
                    }

                    return new TableType(type);
                default:
                    return new UnsupportedType(type);
            }
        }

        #region Equality

        // Aggregate Types (records, tables) can be complex.  
        // Override op= like system.type does. 
        public static bool operator ==(FormulaType a, FormulaType b)
        {
            // Use object.ReferenceEquals to avoid recursion.
            if (ReferenceEquals(a, null))
            {
                return ReferenceEquals(b, null);
            }

            return a.Equals(b);
        }

        public static bool operator !=(FormulaType a, FormulaType b) => !(a == b);

        public override bool Equals(object other)
        {
            if (other is FormulaType t)
            {
                return _type.Equals(t._type);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        #endregion // Equality

        public abstract void Visit(ITypeVisitor vistor);

        internal virtual void DefaultExpressionValue(StringBuilder sb)
        {
            throw new NotSupportedException();
        }

        internal string DefaultExpressionValue()
        {
            var sb = new StringBuilder();

            DefaultExpressionValue(sb);

            return sb.ToString();
        }
    }
}
