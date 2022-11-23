// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Abstract base class for all 1-arg math functions that return numeric values.
    internal abstract class MathOneArgFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public MathOneArgFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc)
            : base(name, description, fc, DType.Number, 0, 1, 1, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.MathFuncArg1 };
        }
    }

    internal abstract class MathOneArgTableFunction : BuiltinFunction
    {
        public override bool SupportsParamCoercion => true;

        public override bool IsSelfContained => true;

        public MathOneArgTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc)
            : base(name, description, fc, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
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

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckNumericColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            if (nodeToCoercedTypeMap?.Any() ?? false)
            {
                // Now set the coerced type to a table with numeric column type with the same name as in the argument.
                returnType = nodeToCoercedTypeMap[arg];
            }
            else
            {
                returnType = argType;
            }

            returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult) ? DType.CreateTable(new TypedName(DType.Number, GetOneColumnTableResultName(context.Features))) : returnType;

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }
}
