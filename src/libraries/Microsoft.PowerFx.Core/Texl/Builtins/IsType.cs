﻿// Copyright (c) Microsoft Corporation.
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
    // IsType(UntypedObject:O, Type:U): Boolean
    internal class IsTypeFunction_UO : UntypedOrJSONConversionFunction
    {
        public const string IsTypeInvariantFunctionName = "IsType";

        public IsTypeFunction_UO()
            : base(IsTypeInvariantFunctionName, TexlStrings.AboutIsTypeUO, DType.Boolean, 2, DType.UntypedObject, DType.Error)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IsTypeUOArg1, TexlStrings.IsTypeUOArg2 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == 2);
            Contracts.Assert(argTypes.Length == 2);
            Contracts.AssertValue(errors);

            if (!base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap))
            {
                return false;
            }

            returnType = DType.Boolean;

            return true;
        }
    }
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
