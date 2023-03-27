// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // IsEmpty(expression:*[]) or legacy IsEmpty(expression:any)
    internal sealed class IsEmptyFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool HasPreciseErrors => true;

        public IsEmptyFunction()
            : base("IsEmpty", TexlStrings.AboutIsEmpty, FunctionCategories.Table | FunctionCategories.Information, DType.Boolean, 0, 1, 1)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsEmptyArg1 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            if (args.Length != 1)
            {
                return base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap);
            }

            nodeToCoercedTypeMap = null;
            var fValid = true;
            if (context.Features.HasFlag(Features.RestrictedIsEmptyArguments))
            {
                var typeCheck = CheckType(args[0], argTypes[0], DType.EmptyTable, errors, false, out DType coercionType);
                if (typeCheck)
                {
                    if (coercionType != null)
                    {
                        CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], coercionType);
                    }
                }

                fValid = typeCheck;
            }
            else
            {
                // Legacy behavior: all types are supported
            }

            returnType = ReturnType;
            return fValid;
        }
    }
}
