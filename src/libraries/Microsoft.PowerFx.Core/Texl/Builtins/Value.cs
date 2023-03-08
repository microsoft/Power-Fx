// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Syntax;

// Decimal TODO: if decimal or float switch off, those functions unavailable

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Value(arg:s|n [,language:s])
    // Corresponding Excel and DAX function: Value
    internal class ValueBaseFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        private readonly DType _returnType;

        public ValueBaseFunction(string functionName, TexlStrings.StringGetter functionAbout, DType functionReturn) 
            : base(functionName, functionAbout, FunctionCategories.Text, functionReturn, 0, 1, 2, DType.String, DType.String)
        {
            _returnType = functionReturn;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValid(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = null;

            var isValid = true;
            var argType = argTypes[0];
            if (!DType.Decimal.Accepts(argType) && !DType.Number.Accepts(argType) && !DType.String.Accepts(argType) && !DType.Boolean.Accepts(argType))
            {
                if (argType.CoercesTo(DType.DateTime) && !argType.IsControl)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], DType.DateTime);
                }
                else
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNumberOrStringExpected);
                    isValid = false;
                }
            }

            if (args.Length > 1)
            {
                argType = argTypes[1];
                if (!DType.String.Accepts(argType))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrStringExpected);
                    isValid = false;
                }
            }

            returnType = _returnType != DType.Unknown ? _returnType : (context.NumberIsFloat ? DType.Number : DType.Decimal);
            return isValid;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ValueArg1 };
            yield return new[] { TexlStrings.ValueArg1, TexlStrings.ValueArg2 };
        }

        public override bool HasSuggestionsForParam(int index)
        {
            return index == 1;
        }
    }

    internal sealed class ValueFunction : ValueBaseFunction
    {
        public ValueFunction()
            : base("Value", TexlStrings.AboutValue, DType.Unknown)
        {
        }
    }

    internal sealed class DecimalFunction : ValueBaseFunction
    {
        // Decimal TODO: Need new TexlStrings
        public DecimalFunction()
            : base("Decimal", TexlStrings.AboutDecimal, DType.Decimal)
        {
        }
    }

    internal sealed class FloatFunction : ValueBaseFunction
    {
        // Decimal TODO: Need new TexlStrings
        public FloatFunction()
            : base("Float", TexlStrings.AboutFloat, DType.Number)
        {
        }
    }

    // Value(arg:O)
    internal class ValueBaseFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public ValueBaseFunction_UO(string functionName, TexlStrings.StringGetter functionAbout, DType resultType)
            : base(functionName, functionAbout, FunctionCategories.Text, resultType, 0, 1, 2, DType.UntypedObject, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ValueArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }

    // Value(arg:O)
    internal sealed class ValueFunction_UO : ValueBaseFunction_UO
    {
        public ValueFunction_UO()
            : base("Value", TexlStrings.AboutValue, DType.Unknown)
        {
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            returnType = context.NumberIsFloat ? DType.Number : DType.Decimal;
            return fValid;
        }
    }

        // Value(arg:O)
    internal sealed class FloatFunction_UO : ValueBaseFunction_UO
    {
        public FloatFunction_UO()
            : base("Float", TexlStrings.AboutFloat, DType.Number)
        {
        }
    }

    internal sealed class DecimalFunction_UO : ValueBaseFunction_UO
    {
        public DecimalFunction_UO()
            : base("Decimal", TexlStrings.AboutDecimal, DType.Decimal)
        {
        }
    }
}
