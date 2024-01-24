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
    // UniChar(arg:n) : s
    // Corresponding Excel function: UNICHAR
    internal sealed class UniCharFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index, int argCount)
        {
            return base.GetGenericArgPreprocessor(index);
        }

        public override bool IsSelfContained => true;

        public override bool IsStateless => true;

        public UniCharFunction()
            : base("UniChar", TexlStrings.AboutUniChar, FunctionCategories.Text, DType.String, 0, 1, 1, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.UniCharArg1 };
        }
    }

    // UniChar(arg:*[n]) : *[s]
    internal sealed class UniCharTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public UniCharTFunction()
            : base("UniChar", TexlStrings.AboutUniCharT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.UniCharTArg1 };
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

            // Typecheck the input table
            fValid &= CheckNumericColumnType(context, args[0], argTypes[0], errors, ref nodeToCoercedTypeMap);

            // Synthesize a new return type
            returnType = DType.CreateTable(new TypedName(DType.String, ColumnName_Value));

            return fValid;
        }
    }
}
