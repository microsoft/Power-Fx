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
    // Concatenate(source1:s, source2:s, ...)
    // Corresponding DAX function: Concatenate
    // This only performs string/string concatenation.
    internal sealed class ConcatenateFunction : ConcatenateFunctionBase
    {
        public ConcatenateFunction()
            : base("Concatenate")
        {
        }
    }

    // Base implementation for Concatenate and StringInterpolation
    internal abstract class ConcatenateFunctionBase : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ConcatenateFunctionBase(string name)
            : base(name, TexlStrings.AboutConcatenate, FunctionCategories.Text, DType.String, 0, 1, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ConcatenateArg1 };
            yield return new[] { TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1 };
            yield return new[] { TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1 };
            yield return new[] { TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1);
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

    // Concatenate(source1:s|*[s], source2:s|*[s], ...)
    // Corresponding DAX function: Concatenate
    // Note, this performs string/table, table/table, table/string concatenation, but not string/string
    // Tables will be expanded to be the same size as the largest table. For each scalar, a new empty table
    // will be created, and the scalar value will be used to fill the table to be the same size as the largest table
    internal sealed class ConcatenateTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ConcatenateTableFunction()
            : base("Concatenate", TexlStrings.AboutConcatenateT, FunctionCategories.Table | FunctionCategories.Text, DType.EmptyTable, 0, 1, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1 };
            yield return new[] { TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1 };
            yield return new[] { TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1, TexlStrings.ConcatenateTArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1);
            }

            return base.GetSignatures(arity);
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 1);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;

            var count = args.Length;
            var hasTableArg = false;
            var fArgsValid = true;

            // Type check the args
            for (var i = 0; i < count; i++)
            {
                fArgsValid &= CheckParamIsTypeOrSingleColumnTable(DType.String, args[i], argTypes[i], errors, out var isTable, ref nodeToCoercedTypeMap);
                hasTableArg |= isTable;
            }

            fArgsValid &= hasTableArg;

            if (!fArgsValid)
            {
                nodeToCoercedTypeMap = null;
            }

            returnType = DType.CreateTable(new TypedName(DType.String, OneColumnTableResultName));

            return hasTableArg && fArgsValid;
        }
    }
}
