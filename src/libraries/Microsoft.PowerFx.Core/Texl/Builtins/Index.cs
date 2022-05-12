// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Index(source:*, n)
    internal sealed class IndexFunction : FunctionWithTableInput
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public IndexFunction()
            : base(
                  "Index",
                  TexlStrings.AboutIndex,
                  FunctionCategories.Table,
                  DType.EmptyRecord,
                  0,
                  2,
                  2,
                  DType.EmptyTable,
                  DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IndexArg1, TexlStrings.IndexArg2 };
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fArgsValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var arg0Type = argTypes[0];
            if (arg0Type.IsTable)
            {
                returnType = arg0Type.ToRecord();
            }
            else
            {
                returnType = arg0Type.IsRecord ? arg0Type : DType.Error;
                fArgsValid = false;
            }

            return fArgsValid;
        }
    }
}
