// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// Base class for type of a Formula. 
    /// Formula Types are a class hiearchy.
    /// </summary>
    [DebuggerDisplay("{_type}")]
    public abstract class FormulaType
    {
        // protected isn't enough to let derived classes access this.
        internal readonly DType _type;

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

        // Get the correct derived type
        internal static FormulaType Build(DType type)
        {
            switch (type.Kind)
            {
                case DKind.ObjNull: return Blank;

                case DKind.Record: return new RecordType(type);
                case DKind.Table: return new TableType(type);

                case DKind.Number: return Number;
                case DKind.String: return String;
                case DKind.Boolean: return Boolean;
                case DKind.Currency: return Number; // TODO: validate
                case DKind.Hyperlink: return Hyperlink;

                case DKind.Time: return Time;
                case DKind.Date: return Date;
                case DKind.DateTime: return DateTime;
                case DKind.DateTimeNoTimeZone: return DateTimeNoTimeZone;

                case DKind.OptionSetValue:
                    var isBoolean = type.OptionSetInfo?.IsBooleanValued;
                    return isBoolean.HasValue && isBoolean.Value ? Boolean : OptionSetValue;

                // This isn't quite right, but once we're in the IR, an option set acts more like a record with optionsetvalue fields. 
                case DKind.OptionSet:
                    return new RecordType(DType.CreateRecord(type.GetAllNames(DPath.Root)));

                case DKind.UntypedObject:
                    return UntypedObject;

                default:
                    throw new NotImplementedException($"Not implemented type: {type}");
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

        public abstract void Visit(ITypeVistor vistor);
    }
}
