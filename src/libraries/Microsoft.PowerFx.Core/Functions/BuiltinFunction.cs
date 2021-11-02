// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Numerics;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions
{
    // Abstract base class for all Texl builtin functions.
    internal abstract class BuiltinFunction : TexlFunction
    {
        public static readonly DName OneColumnTableResultName = new DName("Result");
        public static readonly DName ColumnName_Name = new DName("Name");
        public static readonly DName ColumnName_Address = new DName("Address");
        public static readonly DName ColumnName_Value = new DName("Value");
        public static readonly DName ColumnName_FullMatch = new DName("FullMatch");
        public static readonly DName ColumnName_SubMatches = new DName("SubMatches");
        public static readonly DName ColumnName_StartMatch = new DName("StartMatch");

        public BuiltinFunction(DPath theNamespace, string name, string localeSpecificName, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : base(theNamespace, name, localeSpecificName, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        { }

        public BuiltinFunction(DPath theNamespace, string name, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(theNamespace, name, /*localeSpecificName*/string.Empty,  description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        { }

        public BuiltinFunction(string name, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        { }

        public BuiltinFunction(string name, string localeSpecificName, TexlStrings.StringGetter description, FunctionCategories functionCategories, DType returnType, BigInteger maskLambdas, int arityMin, int arityMax, params DType[] paramTypes)
            : this(DPath.Root, name, localeSpecificName, description, functionCategories, returnType, maskLambdas, arityMin, arityMax, paramTypes)
        { }
    }
}
