// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all math functions that return numeric values.
    internal abstract class MathFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        // This function supports native operations on Decimal values and will return a Decimal value if Decimal args are provided.
        // Values are not coerced to Float before calling this function.
        private readonly bool _nativeDecimal;

        private readonly bool _nativeDateTime;

        private readonly bool _replaceBlankWithZero;

        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return _replaceBlankWithZero ?
                (_nativeDecimal ? ArgPreprocessor.ReplaceBlankWithFuncResultTypedZero : ArgPreprocessor.ReplaceBlankWithFloatZero) :
                ArgPreprocessor.None;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Decimal TODO: Math arg strings
            yield return new[] { TexlStrings.MathFuncArg1 };
            if (MaxArity > 1)
            {
                yield return new[] { TexlStrings.MathFuncArg1, TexlStrings.MathFuncArg1 };
            }
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 1)
            {
                // Decimal TODO: Math arg strings
                return GetGenericSignatures(arity, TexlStrings.MathFuncArg1);
            }

            return base.GetSignatures(arity);
        }

        public MathFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, int minArity, int maxArity, bool nativeDecimal = false, bool replaceBlankWithZero = true, bool nativeDateTime = false)
            : base(name, description, fc, DType.Unknown, 0, minArity, maxArity, nativeDecimal ? DType.Unknown : DType.Number)
        {
            _nativeDecimal = nativeDecimal;
            _replaceBlankWithZero = replaceBlankWithZero;
            _nativeDateTime = nativeDateTime;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);
            Contracts.AssertValue(errors);

            var fValid = true;

            nodeToCoercedTypeMap = null;

            // Return type of the funciton is taken from the first argument.
            returnType = _nativeDecimal ? NumDecReturnType(context, argTypes[0]) : DType.Number;

            if (_nativeDateTime)
            {
                // If there is mixing of Date and DateTime, coerce Date to DateTime
                if (Array.TrueForAll(argTypes, element => element.Kind == DKind.Date || element.Kind == DKind.DateTime) &&
                    !Array.TrueForAll(argTypes, element => element.Kind == DKind.Date))
                {
                    returnType = DType.DateTime;
                }
                
                // ELSE If all elements are the same type AND if the elements are it is Date/Time/DateTime, use the Date/Time/DateTime type.
                else if (Array.TrueForAll(argTypes, element => element.Kind == argTypes[0].Kind) &&
                    !Array.Exists(argTypes, element => element.Kind != DKind.Date && element.Kind != DKind.DateTime && element.Kind != DKind.Time))
                {
                    returnType = argTypes[0];
                }
            }

            for (int i = 0; i < argTypes.Length; i++)
            {
                if (CheckType(args[i], argTypes[i], returnType, DefaultErrorContainer, out var matchedWithCoercion))
                {
                    if (matchedWithCoercion)
                    {
                        if (nodeToCoercedTypeMap == null)
                        {
                            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();
                        }

                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], returnType, allowDupes: true);
                    }
                }
                else
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNumberExpected);
                    fValid = false;
                }
            }

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }

    internal abstract class MathOneArgFunction : MathFunction
    {
        public MathOneArgFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
            : base(name, description, fc, 1, 1, nativeDecimal)
        {
        }
    }

    internal abstract class MathTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        // This function supports native operations on Decimal values and will return a Decimal value if Decimal args are provided.
        // Values are not coerced to Float before calling this function.
        private readonly bool _nativeDecimal;

        public MathTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, int minArity, int maxArity, bool nativeDecimal)
            : base(name, description, fc, DType.EmptyTable, 0, minArity, maxArity, DType.EmptyTable)
        {
            _nativeDecimal = nativeDecimal;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // Decimal TODO: Math arg strings
            yield return new[] { TexlStrings.MathFuncArg1 };
            if (MaxArity > 1)
            {
                yield return new[] { TexlStrings.MathFuncArg1, TexlStrings.MathFuncArg1 };
            }
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 1)
            {
                // Decimal TODO: Math arg strings
                return GetGenericSignatures(arity, TexlStrings.MathTFuncArg1, TexlStrings.MathTFuncArg1);
            }

            return base.GetSignatures(arity);
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == MaxArity);
            Contracts.AssertValue(errors);

            // each argument is run through base.CheckTypes below if scalar
            var fValid = true;
            nodeToCoercedTypeMap = null;

            int tables = 0;

            DType scalarType = _nativeDecimal ? NumDecReturnType(context, argTypes[0]) : DType.Number;

            returnType = DType.CreateTable(new TypedName(scalarType, GetOneColumnTableResultName(context.Features)));

            for (int i = 0; i < argTypes.Length; i++)
            {   
                if (argTypes[i].IsTable)
                {
                    // Ensure we have a one-column table of numbers.
                    if (scalarType == DType.Number)
                    {
                        fValid &= CheckNumericColumnType(argTypes[i], args[i], errors, ref nodeToCoercedTypeMap);
                    }
                    else
                    {
                        fValid &= CheckDecimalColumnType(argTypes[i], args[i], errors, ref nodeToCoercedTypeMap);
                    }

                    tables++;
                }
                else if (CheckType(args[i], argTypes[i], scalarType, DefaultErrorContainer, out var matchedWithCoercion))
                {
                    if (matchedWithCoercion)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[i], scalarType);
                    }
                }
                else
                {
                    fValid = false;
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNumberExpected);
                }
            }

            // At least one arg has to be a table.
            if (tables == 0)
            {
                fValid = false;
            }

            if (fValid)
            {
                if (!context.Features.HasFlag(Features.ConsistentOneColumnTableResult) && args.Length == 1)
                {
                    if (nodeToCoercedTypeMap?.Any() ?? false)
                    {
                        // Now set the coerced type to a table with numeric column type with the same name as in the argument.
                        returnType = nodeToCoercedTypeMap[args[0]];
                    }
                    else
                    {
                        returnType = argTypes[0];
                    }
                }
            }
            else
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }

    internal abstract class MathOneArgTableFunction : MathTableFunction
    {
        public MathOneArgTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
            : base(name, description, fc, 1, 1, nativeDecimal)
        {
        }
    }
}
