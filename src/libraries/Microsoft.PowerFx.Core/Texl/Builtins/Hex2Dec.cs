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

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);
            Contracts.Assert(!fValid || returnType.IsColumn);
            var type = argTypes[0];
            var arg = args[0];
            if (type.IsTable)
            {
                fValid &= CheckStringColumnType(type, arg, errors, ref nodeToCoercedTypeMap);
            }
            else
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, arg, TexlStrings.ErrTypeError);
                return false;
            }

            return fValid;
        }
    }
}
