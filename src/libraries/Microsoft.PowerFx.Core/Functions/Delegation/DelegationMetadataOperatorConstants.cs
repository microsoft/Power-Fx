// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    }
}
