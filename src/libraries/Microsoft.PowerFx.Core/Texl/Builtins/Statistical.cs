// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class StatisticalFunction : MathFunction
    {
        public StatisticalFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
            : base(name, description, fc, int.MaxValue, nativeDecimal: nativeDecimal, replaceBlankWithZero: false)
        {
        }
    }
#if false
    // Abstract base class for all statistical functions with similar signatures that take
    // scalar arguments.
    internal abstract class StatisticalFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public bool SupportsDecimal;

        public StatisticalFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc)
            : base(name, description, fc, DType.Unknown, 0, 1, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.StatisticalArg };
            yield return new[] { TexlStrings.StatisticalArg, TexlStrings.StatisticalArg };
            yield return new[] { TexlStrings.StatisticalArg, TexlStrings.StatisticalArg, TexlStrings.StatisticalArg };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.StatisticalArg, TexlStrings.StatisticalArg);
            }

            return base.GetSignatures(arity);
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            returnType = SupportsDecimal && Array.Find(argTypes, item => item != DType.Decimal && item != DType.Boolean && item != DType.ObjNull && (item != DType.String || context.NumberIsFloat)) == null ? DType.Decimal : DType.Number;
            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();
            var fValid = true;

            // Ensure that all the arguments are numeric/coercible to numeric.
            for (var i = 0; i < argTypes.Length; i++)
            {
                if (CheckType(args[i], argTypes[i], returnType, DefaultErrorContainer, out var matchedWithCoercion))
                {
                    if (matchedWithCoercion)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], returnType, allowDupes: true);
                    }
                }
                else
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNumberExpected);
                    fValid = false;
                    break;
                }
            }

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }
#endif
}
