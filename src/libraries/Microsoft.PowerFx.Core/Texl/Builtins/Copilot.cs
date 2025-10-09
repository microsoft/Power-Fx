// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.AppMagic.Authoring.Texl
{
    // Copilot(Prompt:Text): Text
    // Copilot(Prompt:Text, Input:Any): Text
    // Copilot(Prompt:Text, Input:Any, ReturnType:Type): ReturnType
    internal class CopilotFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool IsAsync => true;

        public override bool HasTypeArgs => true;

        public override bool ArgIsType(int argIndex)
        {
            return argIndex == 2;
        }

        public CopilotFunction()
            : base("Copilot", TexlStrings.AboutCopilot, FunctionCategories.Behavior, DType.String, 0, 1, 3, DType.String, DType.Unknown, DType.Unknown)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.CopilotArg1 };
            yield return new[] { TexlStrings.CopilotArg1, TexlStrings.CopilotArg2 };
            yield return new[] { TexlStrings.CopilotArg1, TexlStrings.CopilotArg2, TexlStrings.CopilotArg3 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            returnType = ReturnType;
            nodeToCoercedTypeMap = null;
            if (!base.CheckType(context, args[0], argTypes[0], DType.String, errors, ref nodeToCoercedTypeMap))
            {
                return false;
            }

            // Validate that only primitive types are passed in the context argument
            if (args.Length < 2)
            {
                // No context argument to validate
                return true;
            }

            if (!DType.IsSupportedType(argTypes[1], _primitiveKinds, out var _))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrCopilotContextArgumentInvalidType);
                return false;
            }

            if (args.Length < 3)
            {
                // No return type argument to validate
                return true;
            }

            if (!DType.IsSupportedType(argTypes[2], _primitiveKinds, out var _))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[2], TexlStrings.ErrCopilotTypeArgumentInvalidType);
                return false;
            }

            returnType = argTypes[2];
            return true;
        }

        protected static readonly HashSet<DType> _primitiveKinds = new ()
        {
            DType.Boolean,
            DType.Date,
            DType.DateTime,
            DType.Decimal,
            DType.Number,
            DType.Guid,
            DType.String,
            DType.Time,
            DType.ObjNull,
            DType.UntypedObject,
        };
    }
}
