// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Dec2Hex(number:n, [places:n])
    internal sealed class Dec2HexFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgBlankHandlerPolicy(int index)
        {
            return base.GetDefaultArgBlankHandlerPolicy(index);
        }

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
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);
            Contracts.Assert(!fValid || returnType.IsColumn);
            if (argTypes.Length == 1)
            {
                var type = argTypes[0];
                var arg = args[0];
                if (!type.IsTable)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrTypeError);
                    return false;
                }
            }
            else if (argTypes.Length == 2)
            {
                var type0 = argTypes[0];
                var type1 = argTypes[1];
                var arg1 = args[0];
                var arg2 = args[0];

                if (!type0.IsTable && !type1.IsTable)
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg1, TexlStrings.ErrTypeError);
                    errors.EnsureError(DocumentErrorSeverity.Severe, arg2, TexlStrings.ErrTypeError);
                    return false;
                }

                var otherType = DType.Invalid;
                TexlNode otherArg = null;

                if (type0.IsTable)
                {
                    // Ensure we have a one-column table of numerics
                    fValid &= CheckNumericColumnType(type0, args[0], errors, ref nodeToCoercedTypeMap);

                    // Check arg1 below.
                    otherArg = args[1];
                    otherType = type1;
                }
                else if (type1.IsTable)
                {
                    // Ensure we have a one-column table of numerics
                    fValid &= CheckNumericColumnType(type1, args[1], errors, ref nodeToCoercedTypeMap);

                    // Check arg0 below.
                    otherArg = args[0];
                    otherType = type0;
                }

                Contracts.Assert(otherType.IsValid);
                Contracts.AssertValue(otherArg);

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
            }

            return fValid;
        }
    }
}
