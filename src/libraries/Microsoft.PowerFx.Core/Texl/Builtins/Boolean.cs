// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Boolean(arg:s)
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanFunction : BuiltinFunction
    {
        public const string BooleanInvariantFunctionName = "Boolean";

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction()
            : base(BooleanInvariantFunctionName, TexlStrings.AboutBoolean, FunctionCategories.Text, DType.Boolean, 0, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanArg1 };
        }
    }

    // Boolean(E:*[s])
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanFunction_T : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction_T()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanT, FunctionCategories.Table, DType.EmptyTable, 0, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanTArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_T");
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckStringColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.Boolean, ColumnName_Value));
            returnType = rowType.ToTable();

            return fValid;
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanT_" + paramName, out paramDescription);
        }
    }

    // Boolean(arg:n)
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanNFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanNFunction()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanN, FunctionCategories.Text, DType.Boolean, 0, 0, 1, 1, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanNArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "N");
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanN_" + paramName, out paramDescription);
        }
    }

    // Boolean(E:*[n])
    // Corresponding Excel and DAX function: Boolean
    internal sealed class BooleanNFunction_T : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanNFunction_T()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBooleanNT, FunctionCategories.Table, DType.EmptyTable, 0, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanNTArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "N_T");
        }

        public override bool CheckInvocation(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fValid = base.CheckInvocation(args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            var arg = args[0];
            var argType = argTypes[0];
            fValid &= CheckNumericColumnType(argType, arg, errors, ref nodeToCoercedTypeMap);

            var rowType = DType.EmptyRecord.Add(new TypedName(DType.Boolean, ColumnName_Value));
            returnType = rowType.ToTable();

            return fValid;
        }

        public override bool TryGetParamDescription(string paramName, out string paramDescription)
        {
            Contracts.AssertNonEmpty(paramName);

            return StringResources.TryGet("AboutBooleanNT_" + paramName, out paramDescription);
        }
    }

    // Boolean(arg:O)
    internal sealed class BooleanFunction_UO : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public BooleanFunction_UO()
            : base(BooleanFunction.BooleanInvariantFunctionName, TexlStrings.AboutBoolean, FunctionCategories.Text, DType.Boolean, 0, 0, 1, 1, DType.UntypedObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.BooleanArg1 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
