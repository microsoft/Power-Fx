// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    // Operator strings which are part of delegation metadata Json.
    // These are used when parsing the metadata Json.
    internal static class DelegationMetadataOperatorConstants
    {
        public const string Equal = "eq";
        public const string NotEqual = "ne";
        public const string Less = "lt";
        public const string LessEqual = "le";
        public const string Greater = "gt";
        public const string GreaterEqual = "ge";
        public const string And = "and";
        public const string Or = "or";
        public const string Contains = "contains";
        public const string IndexOf = "indexof";
        public const string SubStringOf = "substringof";
        public const string Not = "not";
        public const string Year = "year";
        public const string Month = "month";
        public const string Day = "day";
        public const string Hour = "hour";
        public const string Minute = "minute";
        public const string Second = "second";
        public const string Lower = "tolower";
        public const string Upper = "toupper";
        public const string Trim = "trim";
        public const string Null = "null";
        public const string Date = "date";
        public const string Length = "length";
        public const string Sum = "sum";
        public const string Min = "min";
        public const string Max = "max";
        public const string Average = "average";
        public const string Count = "count";
        public const string Add = "add";
        public const string Sub = "sub";
        public const string StartsWith = "startswith";
        public const string Mul = "mul";
        public const string Div = "div";
        public const string EndsWith = "endswith";
        public const string CountDistinct = "countdistinct";
        public const string CdsIn = "cdsin";
        public const string Top = "top";
        public const string AsType = "astype";
        public const string ArrayLookup = "arraylookup";
        public const string Distinct = "distinct";
        public const string JoinInner = "joininner";
        public const string JoinLeft = "joinleft";
        public const string JoinRight = "joinright";
        public const string JoinFull = "joinfull";
    }

    /// <summary>
    /// Enum representing supported delegation operators for metadata parsing.
    /// </summary>
    public enum DelegationOperator
    {
        /// <summary>Equal (eq)</summary>
        Eq,

        /// <summary>Not equal (ne)</summary>
        Ne,

        /// <summary>Less than (lt)</summary>
        Lt,

        /// <summary>Less than or equal (le)</summary>
        Le,

        /// <summary>Greater than (gt)</summary>
        Gt,

        /// <summary>Greater than or equal (ge)</summary>
        Ge,

        /// <summary>Logical AND (and)</summary>
        And,

        /// <summary>Logical OR (or)</summary>
        Or,

        /// <summary>String contains (contains)</summary>
        Contains,

        /// <summary>String index of (indexof)</summary>
        Indexof,

        /// <summary>String substring of (substringof)</summary>
        Substringof,

        /// <summary>Logical NOT (not)</summary>
        Not,

        /// <summary>Year extraction (year)</summary>
        Year,

        /// <summary>Month extraction (month)</summary>
        Month,

        /// <summary>Day extraction (day)</summary>
        Day,

        /// <summary>Hour extraction (hour)</summary>
        Hour,

        /// <summary>Minute extraction (minute)</summary>
        Minute,

        /// <summary>Second extraction (second)</summary>
        Second,

        /// <summary>Convert to lower case (tolower)</summary>
        Tolower,

        /// <summary>Convert to upper case (toupper)</summary>
        Toupper,

        /// <summary>Trim whitespace (trim)</summary>
        Trim,

        /// <summary>Null value (null)</summary>
        Null,

        /// <summary>Date extraction (date)</summary>
        Date,

        /// <summary>String length (length)</summary>
        Length,

        /// <summary>Sum aggregation (sum)</summary>
        Sum,

        /// <summary>Minimum aggregation (min)</summary>
        Min,

        /// <summary>Maximum aggregation (max)</summary>
        Max,

        /// <summary>Average aggregation (average)</summary>
        Average,

        /// <summary>Count aggregation (count)</summary>
        Count,

        /// <summary>Addition (add)</summary>
        Add,

        /// <summary>Subtraction (sub)</summary>
        Sub,

        /// <summary>String starts with (startswith)</summary>
        Startswith,

        /// <summary>Multiplication (mul)</summary>
        Mul,

        /// <summary>Division (div)</summary>
        Div,

        /// <summary>String ends with (endswith)</summary>
        Endswith,

        /// <summary>Count distinct aggregation (countdistinct)</summary>
        Countdistinct,

        /// <summary>CDS in (cdsin)</summary>
        Cdsin,

        /// <summary>Top N (top)</summary>
        Top,

        /// <summary>As type (astype)</summary>
        Astype,

        /// <summary>Array lookup (arraylookup)</summary>
        Arraylookup,

        /// <summary>Distinct aggregation (distinct)</summary>
        Distinct,

        /// <summary>Inner join (joininner)</summary>
        JoinInner,

        /// <summary>Left join (joinleft)</summary>
        JoinLeft,

        /// <summary>Right join (joinright)</summary>
        JoinRight,

        /// <summary>Full join (joinfull)</summary>
        JoinFull
    }
}
