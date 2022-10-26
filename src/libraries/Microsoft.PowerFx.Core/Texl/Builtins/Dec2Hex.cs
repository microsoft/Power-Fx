// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Dec2Hex(number:n, [places:n])
    internal sealed class Dec2HexFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        public Dec2HexFunction()
            : base("Dec2Hex", TexlStrings.AboutDec2Hex, FunctionCategories.MathAndStat, DType.String, 0, 1, 2, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Dec2HexArg1, TexlStrings.Dec2HexArg2 };
        }
    }

    // Dec2HexT(number:[n], [places:n])
    internal sealed class Dec2HexTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public override bool SupportsParamCoercion => true;

        public override bool HasPreciseErrors => true;

        private static readonly DType TabularReturnType = DType.CreateTable(new TypedName(DType.String, ColumnName_Value));

        public Dec2HexTFunction()
            : base("Dec2Hex", TexlStrings.AboutDec2HexT, FunctionCategories.Table, TabularReturnType, 0, 1, 2)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.Dec2HexTArg1, TexlStrings.Dec2HexTArg2 };
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var type0 = argTypes[0];
            var type1 = argTypes[1];

            var otherType = DType.Invalid;
            TexlNode otherArg = null;

            // At least one of the arguments has to be a table.
            if (type0.IsTable)
            {
                // Ensure we have a one-column table of numerics
                fValid &= CheckNumericColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);

                returnType = binding.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                    ? DType.CreateTable(new TypedName(DType.Number, new DName(ColumnName_ValueStr)))
                    : type0;

                // Check arg1 below.
                otherArg = args[1];
                otherType = type1;
            }
            else if (type1.IsTable)
            {
                // Ensure we have a one-column table of numerics
                fValid &= CheckNumericColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);

                // Since the 1st arg is not a table, make a new table return type *[Result:n] or
                // *[Value:n] if the consistent return schema flag is enabled
                returnType = DType.CreateTable(new TypedName(DType.Number, GetOneColumnTableResultName(binding)));

                // Check arg0 below.
                otherArg = args[0];
                otherType = type0;
            }

            Contracts.Assert(otherType.IsValid);
            Contracts.AssertValue(otherArg);
            Contracts.Assert(returnType.IsTable);
            Contracts.Assert(!fValid || returnType.IsColumn);

            if (otherType.IsTable)
            {
                // Ensure we have a one-column table of numerics
                fValid &= CheckNumericColumnType(otherType, otherArg, errors, ref nodeToCoercedTypeMap);
            }
            else if (!DType.Number.Accepts(otherType))
            {
                if (otherType.CoercesTo(DType.Number))
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, otherArg, DType.Number);
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, otherArg, TexlStrings.ErrTypeError);
                }
            }

            return fValid;
        }
    }
}
