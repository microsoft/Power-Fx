// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Log(number:n, [base:n]):n
    // Equivalent Excel function: Log
    internal sealed class LogFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;
        public override bool IsSelfContained => true;
        public override bool HasPreciseErrors => true;
        public override bool RequiresErrorContext => true;


        public LogFunction()
            : base("Log", TexlStrings.AboutLog, FunctionCategories.MathAndStat, DType.Number, 0, 1, 2, DType.Number, DType.Number)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathFuncArg1 };
            yield return new[] { TexlStrings.MathFuncArg1, TexlStrings.LogBase };
        }
    }

    // Log(number:n|*[n], [base:n|*[n]]):*[n]
    // Equivalent Excel function: Log
    internal sealed class LogTFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;
        public override bool IsSelfContained => true;
        public override bool RequiresErrorContext => true;

        public LogTFunction()
            : base("Log", TexlStrings.AboutLogT, FunctionCategories.MathAndStat, DType.EmptyTable, 0, 1, 2)
        { }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathFuncArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            var fValid = base.CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            fValid &= CheckAllParamsAreTypeOrSingleColumnTable(DType.Number, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            return fValid;
        }
    }
}