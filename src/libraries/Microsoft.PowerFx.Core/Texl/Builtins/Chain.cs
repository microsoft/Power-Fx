// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable IDE0011
#pragma warning disable SA1503
#pragma warning disable SA1520

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Chain(source:*, formula)
    internal sealed class ChainFunction : BuiltinFunction
    {
        public override bool SkipScopeForInlineRecords => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        // REVIEW shonk: Need proper description string.
        public ChainFunction()
            : base("Chain", TexlStrings.AboutConcatenate, FunctionCategories.Table, DType.Unknown, 0, 2, int.MaxValue, DType.EmptyTable)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            // REVIEW shonk: This is wrong - create correct strings.
            yield return new[] { TexlStrings.ConcatenateArg1, TexlStrings.ConcatenateArg1 };
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

            var fArgsValid = true;
            DType typeRes = DType.Invalid;
            for (int i = 0; i < argTypes.Length; i++)
            {
                var type = argTypes[i];
                if (!type.IsTable)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrTypeError);
                    fArgsValid = false;
                }
                else if (!typeRes.IsValid)
                    typeRes = type;
                else if (DType.TryUnionWithCoerce(typeRes, type, context.Features, coerceToLeftTypeOnly: false, out var typeTmp, out var needCoercion))
                    typeRes = typeTmp;
                else
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrTypeError);
                    fArgsValid = false;
                }
            }

            for (int i = 0; i < argTypes.Length; i++)
            {
                if (argTypes[i] != typeRes)
                    (nodeToCoercedTypeMap ??= new Dictionary<TexlNode, DType>()).Add(args[i], typeRes);
            }

            returnType = typeRes;
            return fArgsValid;
        }
    }
}

#pragma warning restore SA1649 // File name should match first type name
