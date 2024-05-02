// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Drawing;
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

        public override bool IsLazyEvalParam(TexlNode node, int index, Features features)
        {
            return index > 0;
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 1);
            Contracts.AssertValue(errors);

            nodeToCoercedTypeMap = null;
            if (context.Features.PowerFxV1CompatibilityRules)
            {
                return CheckTypesLatest(context, args, argTypes, errors, out returnType, ref nodeToCoercedTypeMap);
            }
            else
            {
                return CheckTypesLegacy(context, args, argTypes, errors, out returnType, ref nodeToCoercedTypeMap);
            }
        }

        private bool CheckTypesLatest(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            var fArgsValid = true;
            var possibleResults = new List<(TexlNode node, DType type)>();
            for (var i = 0; i < args.Length; i++)
            {
                possibleResults.Add((args[i], argTypes[i]));
            }

            returnType = null;
            var type = possibleResults[0].type;

            foreach (var (argNode, argType) in possibleResults)
            {
                if (argType.IsVoid)
                {
                    fArgsValid = false;
                }
                else if (argType.IsError)
                {
                    errors.EnsureError(argNode, TexlStrings.ErrTypeError);
                    fArgsValid = false;
                }
                else if (type.Kind == DKind.ObjNull)
                {
                    // Anything goes with null
                    type = argType;
                }
                else if (argType.Kind == DKind.ObjNull)
                {
                    // ObjNull can be accepted by the current type
                }
                else if (DType.TryUnionWithCoerce(
                         type,
                         argType,
                         context.Features,
                         coerceToLeftTypeOnly: true,
                         out var unionType,
                         out var coercionNeeded))
                {
                    type = unionType;
                    if (coercionNeeded)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, argNode, type);
                    }
                }
                else
                {
                    errors.EnsureError(
                        DocumentErrorSeverity.Severe,
                        argNode,
                        TexlStrings.ErrBadType_ExpectedType_ProvidedType,
                        type.GetKindString(),
                        argType.GetKindString());
                    fArgsValid = false;
                }
            }

            returnType = type;
            return fArgsValid;
        }

        private bool CheckTypesLegacy(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, ref Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.Assert(
                !context.Features.PowerFxV1CompatibilityRules,
                "This method can only be called wtih PowerFxV1CompatibilityRules disabled");

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

                var typeSuper = DType.Supertype(
                    type, 
                    typeArg, 
                    useLegacyDateTimeAccepts: false, 
                    usePowerFxV1CompatibilityRules: false);

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
                    if (typeArg.CoercesTo(type, aggregateCoercion: true, isTopLevelCoercion: false, context.Features))
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
