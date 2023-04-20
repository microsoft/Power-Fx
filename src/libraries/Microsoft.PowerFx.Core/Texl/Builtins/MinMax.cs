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
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Min(arg1:n, arg2:n, ..., argN:n)
    // Max(arg1:n, arg2:n, ..., argN:n)
    // Corresponding Excel functions: Min, Max
    internal sealed class MinMaxFunction : BuiltinFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool IsSelfContained => true;

        public MinMaxFunction(bool isMin)
            : base(isMin ? "Min" : "Max", isMin ? TexlStrings.AboutMin : TexlStrings.AboutMax, FunctionCategories.MathAndStat, DType.Unknown, 0, 1, int.MaxValue, DType.Unknown)
        {
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();            
            var fArgsValid = true;
            returnType = argTypes[0];

            if (context.Features.PowerFxV1CompatibilityRules)
            {
                // When PowerFxV1CompatibilityRules is enabled
                // return type is
                // - always argTypes[0] if all elements are [Date, Time, DateTime, Number, Decimal]
                // - otherwise it's Decimal or Number, depending on NumberIsFloat flag

                returnType = argTypes[0];
                if (!(returnType.IsDateTimeGroup || returnType.IsNumeric))
                { 
                    returnType = context.NumberIsFloat ? DType.Number : DType.Decimal;
                }

                for (var i = 0; i < argTypes.Length; i++)
                {
                    if (!CheckType(context, args[i], argTypes[i], returnType, DefaultErrorContainer, ref nodeToCoercedTypeMap))
                    {
                        errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNumberExpected);
                        fArgsValid = false;
                    }
                }
            }
            else
            {
                // If there is mixing of Date and DateTime, coerce Date to DateTime
                if (Array.TrueForAll(argTypes, element => element.Kind == DKind.Date || element.Kind == DKind.DateTime) && !Array.TrueForAll(argTypes, element => element.Kind == DKind.Date))
                {
                    for (var i = 0; i < argTypes.Length; i++)
                    {
                        if (argTypes[i].Kind == DKind.Date && argTypes[i].CoercesTo(DType.DateTime, aggregateCoercion: true, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: false))
                        {
                            CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], DType.DateTime, allowDupes: true);
                            returnType = DType.DateTime;
                        }
                    }
                } // If there are elements of mixed types OR if the elements are NOT a Date/Time/DateTime, attempt to coerce to numeric.
                else if (!Array.TrueForAll(argTypes, element => element.Kind == argTypes[0].Kind) || !Array.Exists(argTypes, element => element.Kind == DKind.Date || element.Kind == DKind.DateTime || element.Kind == DKind.Time))
                {
                    returnType = DetermineNumericFunctionReturnType(nativeDecimal: true, context.NumberIsFloat, argTypes[0]);

                    // Ensure that all the arguments are numeric/coercible to numeric.
                    for (var i = 0; i < argTypes.Length; i++)
                    {
                        if (!CheckType(context, args[i], argTypes[i], returnType, DefaultErrorContainer, ref nodeToCoercedTypeMap))
                        {
                            errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNumberExpected);
                            fArgsValid = false;
                        }
                    }
                }               
            }

            if (!fArgsValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fArgsValid;
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
    }
}
