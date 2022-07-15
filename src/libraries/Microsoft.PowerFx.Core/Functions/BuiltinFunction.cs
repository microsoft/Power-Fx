// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Numerics;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    // Abstract base class for all Texl builtin functions.
    internal abstract class BuiltinFunction : TexlFunction
    {
        public const string OneColumnTableResultNameStrOld = "Result";
        public const string ColumnName_NameStr = "Name";
        public const string ColumnName_AddressStr = "Address";
        public const string ColumnName_ValueStr = "Value";
        public const string ColumnName_FullMatchStr = "FullMatch";
        public const string ColumnName_SubMatchesStr = "SubMatches";
        public const string ColumnName_StartMatchStr = "StartMatch";

        public static DName GetOneColumnTableResultName(TexlBinding binding) => GetOneColumnTableResultName(binding.Features);

        public static DName GetOneColumnTableResultName(Features f) => f.HasFlag(Features.ConsistentOneColumnTableResult) 
            ? new DName(ColumnName_ValueStr) 
            : new DName(OneColumnTableResultNameStrOld);

        public static readonly DName ColumnName_Name = new DName(ColumnName_NameStr);
        public static readonly DName ColumnName_Address = new DName(ColumnName_AddressStr);
        public static readonly DName ColumnName_Value = new DName(ColumnName_ValueStr);
        public static readonly DName ColumnName_FullMatch = new DName(ColumnName_FullMatchStr);
        public static readonly DName ColumnName_SubMatches = new DName(ColumnName_SubMatchesStr);
        public static readonly DName ColumnName_StartMatch = new DName(ColumnName_StartMatchStr);

        public BuiltinFunction(DPath theNamespace, string name, string localeSpecificName, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(theNamespace, name, localeSpecificName, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public BuiltinFunction(DPath theNamespace, string name, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(theNamespace, name, /*localeSpecificName*/string.Empty, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public BuiltinFunction(string name, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }

        public BuiltinFunction(string name, string localeSpecificName, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, localeSpecificName, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        {
        }
    }
}
