// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation.DelegationStrategies;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all 1-arg math functions that return numeric values.
    internal abstract class MathFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        private readonly bool _nativeDecimal;

        public MathFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, int arityMax = 1, bool nativeDecimal = false)
            : base(name, description, fc, DType.Unknown, 0, 1, arityMax, nativeDecimal ? DType.Unknown : DType.Number)
        {
            _nativeDecimal = nativeDecimal;
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

            returnType = Array.FindIndex(argTypes, item => NumDecReturnType(context, _nativeDecimal, item) == DType.Number) == -1 ? DType.Decimal : DType.Number;

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

    internal abstract class MathOneArgTableFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        private readonly bool _nativeDecimal;

        public MathOneArgTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, bool nativeDecimal = false)
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
            Contracts.Assert(args.Length == 1);
            Contracts.AssertValue(errors);

            var arg = args[0];
            var argType = argTypes[0];
            var fValid = true;

            //           var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            //           Contracts.Assert(returnType.IsTable);

            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();

            var scalarType = NumDecReturnType(context, _nativeDecimal, argType);

            fValid &= CheckNumDecColumnType(scalarType, argType, arg, errors, ref nodeToCoercedTypeMap);

            if (nodeToCoercedTypeMap?.Any() ?? false)
            {
                // Now set the coerced type to a table with numeric column type with the same name as in the argument.
                returnType = nodeToCoercedTypeMap[arg];
            }
            else
            {
                returnType = argType;
            }

            returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult) ? DType.CreateTable(new TypedName(scalarType, GetOneColumnTableResultName(context.Features))) : returnType;

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }
}
