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
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all math functions that return numeric values.
    internal abstract class MathFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
                return _replaceBlankWithZero ? base.GetGenericArgPreprocessor(index) : ArgPreprocessor.None;
        }

        public override bool IsSelfContained => true;

        private readonly bool _nativeDecimal;

        private readonly bool _replaceBlankWithZero;

        public MathFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false, bool replaceBlankWithZero = true)
            : base(name, description, fc, DType.Unknown, 0, 1, 1, nativeDecimal ? DType.Unknown : DType.Number)
        {
            _nativeDecimal = nativeDecimal;
            _replaceBlankWithZero = replaceBlankWithZero;
        }

        public MathFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool twoArg, bool nativeDecimal = false, bool replaceBlankWithZero = true)
            : base(name, description, fc, DType.Unknown, 0, 1, 2, nativeDecimal ? DType.Unknown : DType.Number, nativeDecimal ? DType.Unknown : DType.Number)
        {
            _nativeDecimal = nativeDecimal;
            _replaceBlankWithZero = replaceBlankWithZero;
        }

        public MathFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, int maxArity, bool nativeDecimal = false, bool replaceBlankWithZero = true)
            : base(name, description, fc, DType.Unknown, 0, 1, maxArity, nativeDecimal ? DType.Unknown : DType.Number)
        {
            _nativeDecimal = nativeDecimal;
            _replaceBlankWithZero = replaceBlankWithZero;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathFuncArg1 };
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

            bool isDeferred = false;

            if (_nativeDecimal)
            {
                returnType = DType.Decimal;

                for (int i = 0; i < argTypes.Length; i++)
                {
                    var argReturn = NumDecReturnType(context, _nativeDecimal, argTypes[i]);
                    if (argReturn == DType.Number)
                    {
                        returnType = DType.Number;
                    }
                    else if (argReturn == DType.Deferred)
                    {
                        isDeferred = true;
                    }
                }

                if (isDeferred && returnType == DType.Decimal)
                {
                    returnType = DType.Deferred;
                    return true;
                }
            }
            else
            {
                returnType = DType.Number;
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

    internal abstract class MathTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        private readonly bool _nativeDecimal;

        public MathTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool twoArg, bool nativeDecimal = false)
            : base(name, description, fc, DType.EmptyTable, 0, 2, 2)
        {
            _nativeDecimal = nativeDecimal;
        }

        public MathTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
            : base(name, description, fc, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
            _nativeDecimal = nativeDecimal;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathTFuncArg1 };
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

            var fValid = true;

            int tables = 0;

            bool isDeferred = false;

            DType scalarType;

            nodeToCoercedTypeMap = null;

            if (_nativeDecimal)
            {
                scalarType = DType.Decimal;

                for (int i = 0; i < argTypes.Length; i++)
                {
                    var argReturn = NumDecReturnType(context, _nativeDecimal, argTypes[i]);
                    if (argReturn == DType.Number)
                    {
                        scalarType = DType.Number;
                    }
                    else if (argReturn == DType.Deferred)
                    {
                        isDeferred = true;
                    }
                }

                if (isDeferred && scalarType == DType.Decimal)
                {
                    returnType = DType.Deferred;
                    return true;
                }
            }
            else
            {
                scalarType = DType.Number;
            }

            for (int i = 0; i < argTypes.Length; i++)
            {   
                if (argTypes[i].IsTable)
                {
                    // Ensure we have a one-column table of numbers.
                    fValid &= CheckNumDecColumnType(scalarType, argTypes[i], args[i], errors, ref nodeToCoercedTypeMap);
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
                returnType = DType.CreateTable(new TypedName(scalarType, GetOneColumnTableResultName(context.Features)));

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
                returnType = null;
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }
}
