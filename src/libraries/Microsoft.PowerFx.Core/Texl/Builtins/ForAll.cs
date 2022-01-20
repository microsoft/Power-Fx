// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // ForAll(source:*, formula)
    internal sealed class ForAllFunction : FunctionWithTableInput
    {
        public override bool SkipScopeForInlineRecords => true;

        public override bool IsSelfContained => true;

        public override bool RequiresErrorContext => true;

        public override bool SupportsParamCoercion => false;

        public ForAllFunction()
            : base("ForAll", TexlStrings.AboutForAll, FunctionCategories.Table, DType.Unknown, 0x2, 2, 2, DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ForAllArg1, TexlStrings.ForAllArg2 };
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var fArgsValid = CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            if (argTypes[1].IsRecord)
            {
                returnType = argTypes[1].ToTable();
            }
            else if (argTypes[1].IsPrimitive || argTypes[1].IsTable)
            {
                returnType = DType.CreateTable(new TypedName(argTypes[1], ColumnName_Value));
            }
            else
            {
                returnType = DType.Error;
                fArgsValid = false;
            }

            return fArgsValid;
        }

        public override bool HasSuggestionsForParam(int index)
        {
            Contracts.Assert(index >= 0);

            return index == 0;
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: isPrefetching ? "_ParallelPrefetching" : string.Empty);
        }
    }
}