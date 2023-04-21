// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Hex2Dec(number:n)
    internal sealed class Hex2DecFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return base.GetGenericArgPreprocessor(index);
        }

        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public Hex2DecFunction()
            : base("Hex2Dec", TexlStrings.AboutHex2Dec, FunctionCategories.MathAndStat, DType.Number, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Hex2DecArg1 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckTypes(context, args, argTypes, errors, out _, out nodeToCoercedTypeMap);

            // As this is an integer returning function, it can be either a number or a decimal depending on NumberIsFloat.
            // We do this to preserve decimal precision if this function is used in a calculation
            // since returning Float would promote everything to Float and precision could be lost
            returnType = context.NumberIsFloat ? DType.Number : DType.Decimal;

            return fValid;
        }
    }

    // Hex2DecT(number:[n])
    internal sealed class Hex2DecTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        private static readonly DType TabularReturnType = DType.CreateTable(new TypedName(DType.Number, ColumnName_Value));

        public Hex2DecTFunction()
            : base("Hex2Dec", TexlStrings.AboutHex2DecT, FunctionCategories.Table, TabularReturnType, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Hex2DecTArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 1);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out _, out nodeToCoercedTypeMap);

            var type = argTypes[0];
            var arg = args[0];

            fValid &= CheckStringColumnType(context, arg, type, errors, ref nodeToCoercedTypeMap);

            // Synthesize a new return type
            // As this is an integer returning function, it can be either a number or a decimal depending on NumberIsFloat.
            // We do this to preserve decimal precision if this function is used in a calculation
            // since returning Float would promote everything to Float and precision could be lost
            var returnScalarType = context.NumberIsFloat ? DType.Number : DType.Decimal;
            returnType = DType.CreateTable(new TypedName(returnScalarType, GetOneColumnTableResultName(context.Features)));

            return fValid;
        }
    }
}
