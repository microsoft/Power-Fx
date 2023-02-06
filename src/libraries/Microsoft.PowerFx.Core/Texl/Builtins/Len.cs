// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.Delegation;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Len(arg:s)
    // Corresponding DAX function: Len
    internal sealed class LenFunction : StringOneArgFunction
    {
        public override bool HasPreciseErrors => true;

        public override bool SupportsParamCoercion => true;

        public LenFunction()
            : base("Len", TexlStrings.AboutLen, FunctionCategories.Text, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LenArg1 };
        }

        public override DelegationCapability FunctionDelegationCapability => DelegationCapability.Length;

        public override bool IsRowScopedServerDelegatable(CallNode callNode, TexlBinding binding, OperationCapabilityMetadata metadata)
        {
            return base.IsRowScopedServerDelegatable(callNode, binding, metadata);
        }
    }

    // Len(arg:*[s])
    internal sealed class LenTFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public LenTFunction()
            : base("Len", TexlStrings.AboutLenT, FunctionCategories.Table, DType.EmptyTable, 0, 1, 1, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.LenTArg1 };
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
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            var fValid = base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            Contracts.Assert(returnType.IsTable);

            // Typecheck the input table
            fValid &= CheckStringColumnType(argTypes[0], args[0], errors, ref nodeToCoercedTypeMap);

            // Synthesize a new return type
            returnType = DType.CreateTable(new TypedName(DType.Number, ColumnName_Value));

            return fValid;
        }
    }
}
