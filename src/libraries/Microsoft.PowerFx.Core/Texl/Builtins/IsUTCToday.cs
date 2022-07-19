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
    // IsUTCToday(date: d): b
    internal sealed class IsUTCTodayFunction : BuiltinFunction
    {
        // Multiple invocations may result in different return values.
        public override bool IsStateless => false;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public IsUTCTodayFunction()
            : base("IsUTCToday", TexlStrings.AboutIsUTCToday, FunctionCategories.Information, DType.Boolean, 0, 0, 1, 1, DType.DateTime)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            return EnumerableUtils.Yield(new[] { TexlStrings.IsUTCTodayFuncArg1 });
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

            // Arg0 should not be a Time
            if (type0.Kind == DKind.Time)
            {
                fValid = false;
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrDateExpected);
            }

            returnType = ReturnType;
            return fValid;
        }
    }
}
