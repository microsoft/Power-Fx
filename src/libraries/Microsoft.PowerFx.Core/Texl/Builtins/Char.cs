// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Char(arg:n) : s
    // Corresponding Excel function: Char
    internal sealed class CharFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return base.GetGenericArgPreprocessor(index);
        }

        public override bool IsSelfContained => true;

        public CharFunction()
            : base("Char", TexlStrings.AboutChar, FunctionCategories.Text, DType.String, 0, 1, 1, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CharArg1 };
        }
    }

    // Char(arg:*[n]) : *[s]
    internal sealed class CharTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public CharTFunction()
            : base("Char", TexlStrings.AboutCharT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CharTArg1 };
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
            fValid &= CheckNumericColumnType(argTypes[0], args[0], errors, ref nodeToCoercedTypeMap);

            // Synthesize a new return type
            returnType = DType.CreateTable(new TypedName(DType.String, GetOneColumnTableResultName(context.Features)));

            return fValid;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
