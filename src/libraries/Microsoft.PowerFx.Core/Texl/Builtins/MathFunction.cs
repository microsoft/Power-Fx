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

        public enum DecimalOverload
        {
            None = 0,
            Number,
            Decimal
        }

        private readonly DecimalOverload _decimalOverload = DecimalOverload.None;

        public MathOneArgFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, DecimalOverload decimalOverload = DecimalOverload.None)
            : base(name, description, fc, decimalOverload == DecimalOverload.Decimal ? DType.Decimal : DType.Number, 0, 1, 1, decimalOverload == DecimalOverload.Decimal ? DType.Decimal : DType.Number)
        {
            _decimalOverload = decimalOverload;
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
            Contracts.Assert(args.Length == 1);
            Contracts.AssertValue(errors);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);

            var arg = args[0];
            var argType = argTypes[0];

            if ((nodeToCoercedTypeMap?.Any() ?? false) && 
                ((_decimalOverload == DecimalOverload.Decimal && context.NumberIsFloat) ||
                 (_decimalOverload == DecimalOverload.Number && !context.NumberIsFloat)))
            {
                fValid = false;
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

        private readonly DecimalOverload _decimalOverload = DecimalOverload.None;

        public enum DecimalOverload
        {
            None = 0,
            Number,
            Decimal
        }

        public override bool IsSelfContained => true;

        public MathOneArgTableFunction(string name, TexlStrings.StringGetter description, FunctionCategories fc, DecimalOverload decimalOverload = DecimalOverload.None)
            : base(name, description, fc, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
            _decimalOverload = decimalOverload;
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

            fValid &= _decimalOverload == DecimalOverload.Decimal ? CheckDecimalColumnType(argType, arg, errors, ref nodeToCoercedTypeMap) : CheckNumericColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            if (nodeToCoercedTypeMap?.Any() ?? false)
            {
                // Now set the coerced type to a table with numeric column type with the same name as in the argument.
                returnType = nodeToCoercedTypeMap[arg];

                if ((_decimalOverload == DecimalOverload.Decimal && context.NumberIsFloat) ||
                    (_decimalOverload == DecimalOverload.Number && !context.NumberIsFloat))
                {
                    fValid = false;
                }
            }
            else
            {
                returnType = argType;
            }

            returnType = context.Features.HasFlag(Features.ConsistentOneColumnTableResult) ? DType.CreateTable(new TypedName(_decimalOverload == DecimalOverload.Decimal ? DType.Decimal : DType.Number, GetOneColumnTableResultName(context.Features))) : returnType;

            if (!fValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fValid;
        }
    }
}
