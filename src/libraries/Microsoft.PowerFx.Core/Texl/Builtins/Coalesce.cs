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

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Coalesce(expression:E)
    // Equivalent T-SQL Function: COALESCE
    internal sealed class CoalesceFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool CheckTypesAndSemanticsOnly => true;

        public CoalesceFunction()
            : base("Coalesce", TexlStrings.AboutCoalesce, FunctionCategories.Information, DType.Unknown, 0, 1, int.MaxValue)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CoalesceArg1 };
            yield return new[] { TexlStrings.CoalesceArg1, TexlStrings.CoalesceArg1 };
            yield return new[] { TexlStrings.CoalesceArg1, TexlStrings.CoalesceArg1, TexlStrings.CoalesceArg1 };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures(int arity)
        {
            if (arity > 2)
            {
                return GetGenericSignatures(arity, TexlStrings.CoalesceArg1);
            }

            return base.GetSignatures(arity);
        }

        protected override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 1);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;

            var count = args.Length;
            var fArgsValid = true;
            var fArgsNonNull = false;
            var type = ReturnType;

            for (var i = 0; i < count; i++)
            {
                var nodeArg = args[i];
                var typeArg = argTypes[i];

                if (typeArg.Kind == DKind.ObjNull)
                {
                    continue;
                }

                fArgsNonNull = true;
                if (typeArg.IsError)
                {
                    errors.EnsureError(args[i], TexlStrings.ErrTypeError);
                }

                var typeSuper = DType.Supertype(type, typeArg);

                if (!typeSuper.IsError)
                {
                    type = typeSuper;
                }
                else if (type.Kind == DKind.Unknown)
                {
                    // One of the args is also of unknown type, so we can't resolve the type of IfError
                    type = typeSuper;
                    fArgsValid = false;
                }
                else if (!type.IsError)
                {
                    // Types don't resolve normally, coercion needed
                    if (typeArg.CoercesTo(type))
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, nodeArg, type);
                    }
                    else
                    {
                        errors.EnsureError(
                            DocumentErrorSeverity.Severe,
                            nodeArg,
                            TexlStrings.ErrBadType_ExpectedType_ProvidedType,
                            type.GetKindString(),
                            typeArg.GetKindString());
                        fArgsValid = false;
                    }
                }
                else if (typeArg.Kind != DKind.Unknown)
                {
                    type = typeArg;
                    fArgsValid = false;
                }
            }

            if (!fArgsNonNull)
            {
                type = DType.ObjNull;
            }

            returnType = type;
            return fArgsValid;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
