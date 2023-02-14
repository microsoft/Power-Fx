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

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Scalar overloads of Left and Right.
    //  Left(arg:s, count:n)
    //  Right(arg:s, count:n)
    internal sealed class LeftRightScalarFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            if (index == 1)
            {
                return ArgPreprocessor.ReplaceBlankWithZeroAndTruncate;
            }

            return ArgPreprocessor.None;
        }

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public LeftRightScalarFunction(bool isLeft)
            : base(isLeft ? "Left" : "Right", isLeft ? TexlStrings.AboutLeft : TexlStrings.AboutRight, FunctionCategories.Text, DType.String, 0, 2, 2, DType.String, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LeftRightArg1, TexlStrings.LeftRightArg2 };
        }
    }

    // Table overloads of Left and Right:
    //  Left(arg:*[_:s], count:n)
    //  Right(arg:*[_:s], count:n)
    internal sealed class LeftRightTableScalarFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public LeftRightTableScalarFunction(bool isLeft)
            : base(isLeft ? "Left" : "Right", isLeft ? TexlStrings.AboutLeftT : TexlStrings.AboutRightT, FunctionCategories.Table, DType.EmptyTable, 0, 2, 2, DType.EmptyTable, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LeftRightTArg1, TexlStrings.LeftRightArg2 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            // Disambiguate these from the scalar overloads, so we don't have to
            // do type checking in the JS (runtime) implementation of Left/Right.
            return GetUniqueTexlRuntimeName(suffix: "_TS");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 2);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Typecheck the input table
            fValid &= CheckStringColumnType(argTypes[0], args[0], errors, ref nodeToCoercedTypeMap);

            returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                ? DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_ValueStr)))
                : argTypes[0];

            return fValid;
        }
    }

    // Table overloads of Left and Right:
    //  Left(arg:*[_:s], count:*[_:n])
    //  Right(arg:*[_:s], count:*[_:n])
    internal sealed class LeftRightTableTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public LeftRightTableTableFunction(bool isLeft)
            : base(isLeft ? "Left" : "Right", isLeft ? TexlStrings.AboutLeftT : TexlStrings.AboutRightT, FunctionCategories.Table, DType.EmptyTable, 0, 2, 2, DType.EmptyTable, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LeftRightTArg1, TexlStrings.LeftRightArg2 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            // Disambiguate these from the scalar overloads, so we don't have to
            // do type checking in the JS (runtime) implementation of Left/Right.
            return GetUniqueTexlRuntimeName(suffix: "_TT");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 2);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Typecheck the input table
            fValid &= CheckStringColumnType(argTypes[0], args[0], errors, ref nodeToCoercedTypeMap);

            // Typecheck the count table
            fValid &= CheckNumericColumnType(argTypes[1], args[1], errors, ref nodeToCoercedTypeMap);

            returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult)
                ? DType.CreateTable(new TypedName(DType.String, new DName(ColumnName_ValueStr)))
                : argTypes[0];

            return fValid;
        }
    }

    // Table overloads of Left and Right:
    //  Left(arg:s, count:*[_:n])
    //  Right(arg:s, count:*[_:n])
    internal sealed class LeftRightScalarTableFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public LeftRightScalarTableFunction(bool isLeft)
            : base(isLeft ? "Left" : "Right", isLeft ? TexlStrings.AboutLeftT : TexlStrings.AboutRightT, FunctionCategories.Table, DType.EmptyTable, 0, 2, 2, DType.String, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LeftRightArg1, TexlStrings.LeftRightArg2 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            // Disambiguate these from the scalar overloads, so we don't have to
            // do type checking in the JS (runtime) implementation of Left/Right.
            return GetUniqueTexlRuntimeName(suffix: "_ST");
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length == 2);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Typecheck the count table
            fValid &= CheckNumericColumnType(argTypes[1], args[1], errors, ref nodeToCoercedTypeMap);

            // Synthesize a new return type
            returnType = DType.CreateTable(new TypedName(DType.String, GetOneColumnTableResultName(context.Features)));

            return fValid;
        }
    }
}
