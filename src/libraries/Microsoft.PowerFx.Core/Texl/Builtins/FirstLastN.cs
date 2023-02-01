﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // FirstN(source:*, [count:n])
    // LastN(source:*, [count:n])
    internal sealed class FirstLastNFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public FirstLastNFunction(bool isFirst)
            : base(
                  isFirst ? "FirstN" : "LastN",
                  isFirst ? TexlStrings.AboutFirstN : TexlStrings.AboutLastN,
                  FunctionCategories.Table,
                  DType.EmptyTable,
                  0,
                  1,
                  2,
                  DType.EmptyTable,
                  DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.FirstLastNArg1 };
            yield return new[] { TexlStrings.FirstLastNArg1, TexlStrings.FirstLastNArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fArgsValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var arg0Type = argTypes[0];
            if (arg0Type.IsTable)
            {
                returnType = arg0Type;
            }
            else
            {
                returnType = arg0Type.IsRecord ? arg0Type.ToTable() : DType.Error;
                fArgsValid = false;
            }

            return fArgsValid;
        }
    }
}
#pragma warning restore SA1649 // File name should match first type name
