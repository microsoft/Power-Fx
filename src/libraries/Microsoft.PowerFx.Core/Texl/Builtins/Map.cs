// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Map(source:*, formula)
    // Map is a functional programming style function that is equivalent to ForAll
    // but does not allow imperative logic (side effects) within the formula.
    internal sealed class MapFunction : FunctionWithTableInput
    {
        public const string MapInvariantFunctionName = "Map";

        public override bool SkipScopeForInlineRecords => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public MapFunction()
            : base(MapInvariantFunctionName, TexlStrings.AboutMap, FunctionCategories.Table, DType.Unknown, 0x2, 2, 2, DType.EmptyTable)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValues(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;
            var fArgsValid = CheckType(context, args[0], argTypes[0], ParamTypes[0], errors, ref nodeToCoercedTypeMap);

            if (argTypes[1].IsRecord)
            {
                returnType = argTypes[1].ToTable();
            }
            else if (argTypes[1].IsPrimitive || argTypes[1].IsTable || argTypes[1].IsUntypedObject)
            {
                returnType = DType.CreateTable(new TypedName(argTypes[1], ColumnName_Value));
            }
            else if (argTypes[1].IsVoid)
            {
                // Map does not support Void return type since it doesn't allow side effects
                returnType = DType.Error;
                fArgsValid = false;
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

        /// <summary>
        /// Map requires a pure (non-side-effectful) lambda expression.
        /// This validation checks if the lambda argument contains any behavior functions.
        /// </summary>
        public override bool PostVisitValidation(TexlBinding binding, CallNode callNode)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(callNode);

            // Get the lambda argument (second argument, index 1)
            var args = callNode.Args.Children;
            if (args.Count >= 2)
            {
                var lambdaArg = args[1];
                if (binding.HasSideEffects(lambdaArg))
                {
                    binding.ErrorContainer.EnsureError(lambdaArg, TexlStrings.ErrMapFunctionRequiresPureLambda, Name);
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class MapFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public MapFunction_UO()
            : base(MapFunction.MapInvariantFunctionName, TexlStrings.AboutMap, FunctionCategories.Table, DType.Unknown, 0x2, 2, 2, DType.UntypedObject)
        {
            ScopeInfo = new FunctionScopeInfo(this);
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MapArg1, TexlStrings.MapArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValues(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;
            var fArgsValid = CheckType(context, args[0], argTypes[0], ParamTypes[0], errors, ref nodeToCoercedTypeMap);

            if (argTypes[1].IsRecord)
            {
                returnType = argTypes[1].ToTable();
            }
            else if (argTypes[1].IsPrimitive || argTypes[1].IsTable || argTypes[1].IsUntypedObject)
            {
                returnType = DType.CreateTable(new TypedName(argTypes[1], ColumnName_Value));
            }
            else if (argTypes[1].IsVoid)
            {
                // Map does not support Void return type since it doesn't allow side effects
                returnType = DType.Error;
                fArgsValid = false;
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

        /// <summary>
        /// Map requires a pure (non-side-effectful) lambda expression.
        /// This validation checks if the lambda argument contains any behavior functions.
        /// </summary>
        public override bool PostVisitValidation(TexlBinding binding, CallNode callNode)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(callNode);

            // Get the lambda argument (second argument, index 1)
            var args = callNode.Args.Children;
            if (args.Count >= 2)
            {
                var lambdaArg = args[1];
                if (binding.HasSideEffects(lambdaArg))
                {
                    binding.ErrorContainer.EnsureError(lambdaArg, TexlStrings.ErrMapFunctionRequiresPureLambda, Name);
                    return true;
                }
            }

            return false;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
