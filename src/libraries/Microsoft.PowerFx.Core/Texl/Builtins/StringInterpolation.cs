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
    // StringInterpolation(source1:s, source2:s, ...)
    // No DAX function, compiler-only, not available for end users, no table support
    // String interpolations such as $"Hello {"World"}" translate into a call to this function
    internal sealed class StringInterpolationFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public StringInterpolationFunction()
            : base("StringInterpolation", TexlStrings.AboutStringInterpolation, FunctionCategories.Text, DType.String, 0, 1, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StringInterpolationArg1 };
            yield return new[] { TexlStrings.StringInterpolationArg1, TexlStrings.StringInterpolationArg1 };
            yield return new[] { TexlStrings.StringInterpolationArg1, TexlStrings.StringInterpolationArg1, TexlStrings.StringInterpolationArg1 };
            yield return new[] { TexlStrings.StringInterpolationArg1, TexlStrings.StringInterpolationArg1, TexlStrings.StringInterpolationArg1, TexlStrings.StringInterpolationArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.StringInterpolationArg1, TexlStrings.StringInterpolationArg1);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 1);
            Contracts.AssertValue(errors);

            var count = args.Length;
            var fArgsValid = true;
            nodeToCoercedTypeMap = null;

            for (var i = 0; i < count; i++)
            {
                var typeChecks = CheckType(args[i], argTypes[i], DType.String, errors, true, out DType coercionType);
                if (typeChecks && coercionType != null)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], coercionType);
                }

                fArgsValid &= typeChecks;
            }

            if (!fArgsValid)
            {
                nodeToCoercedTypeMap = null;
            }

            returnType = ReturnType;

            return fArgsValid;
        }
    }
}
