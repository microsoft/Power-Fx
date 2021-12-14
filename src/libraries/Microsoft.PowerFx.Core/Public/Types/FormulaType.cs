// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// Base class for type of a Formula. 
    /// Formula Types are a class hiearchy
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

        public static FormulaType OptionSetValue { get; } = new OptionSetValueType();

        // chained by derived type 
        internal FormulaType(DType type, Dictionary<string, string> displayNameSet = null)
        {
            if (displayNameSet == null)
                _type = type;
            else 
            {
                ValidateDisplayNames(displayNameSet);
                _type = DType.AttachDisplayNameProvider(type, new BidirectionalDictionary<string, string>(displayNameSet));
            }

        }

        private void ValidateDisplayNames(Dictionary<string, string> displayNameSet)
        {
            HashSet<string> displayNames = new();
            foreach (var kvp in displayNameSet)
            {
                // Display names can't collide with other display names
                if (displayNames.Contains(kvp.Value))
                {
                    throw new DisplayNameCollisionException(kvp.Value);
                }
                // Display names can't collide with logical names other than their own
                if (kvp.Key != kvp.Value && displayNameSet.ContainsKey(kvp.Value))
                {
                    throw new DisplayNameCollisionException(kvp.Value);
                }

                displayNames.Add(kvp.Value);
            }
        }

        // Get the correct derived type
        internal static FormulaType Build(DType type)
        {
            switch(type.Kind)
            {
                case DKind.ObjNull: return Blank;

                case DKind.Record: return new RecordType(type);
                case DKind.Table: return new TableType(type);

                case DKind.Number: return Number;
                case DKind.String: return String;
                case DKind.Boolean: return Boolean;
                case DKind.Currency: return Number; // TODO: validate

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
            if (object.ReferenceEquals(a,null))
            {
                return object.ReferenceEquals(b, null);
            }
            return a.Equals(b);
        }

        public static bool operator !=(FormulaType a, FormulaType b)
        {
            return !(a == b);
        }
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
