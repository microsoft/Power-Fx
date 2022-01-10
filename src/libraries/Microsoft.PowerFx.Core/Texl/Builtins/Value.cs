// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Value(arg:s|n [,language:s])
    // Corresponding Excel and DAX function: Value
    internal sealed class ValueFunction : BuiltinFunction
    {
        public const string ValueInvariantFunctionName = "Value";

        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public ValueFunction()
            : base(ValueInvariantFunctionName, TexlStrings.AboutValue, FunctionCategories.Text, DType.Number, 0, 1, 2, DType.String, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ValueArg1 };
            yield return new[] { TexlStrings.ValueArg1, TexlStrings.ValueArg2 };
        }

        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValid(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = null;

            var isValid = true;
            var argType = argTypes[0];
            if (!DType.Number.Accepts(argType) && !DType.String.Accepts(argType))
            {
                if (argType.CoercesTo(DType.DateTime) && !argType.IsControl)
                {
                    CollectionUtils.Add(ref nodeToCoercedTypeMap, args[0], DType.DateTime);
                }
                else
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNumberOrStringExpected);
                    isValid = false;
                }
            }

            if (args.Length > 1)
            {
                argType = argTypes[1];
                if (!DType.String.Accepts(argType))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrStringExpected);
                    isValid = false;
                }
            }

            returnType = DType.Number;
            return isValid;
        }

        public override bool HasSuggestionsForParam(int index)
        {
            return index == 1;
        }
    }

    // Value(arg:O)
    internal sealed class ValueFunction_CO : BuiltinFunction
    {
        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public ValueFunction_CO()
            : base(ValueFunction.ValueInvariantFunctionName, TexlStrings.AboutValue, FunctionCategories.CustomObject, DType.Number, 0, 1, 1, DType.CustomObject)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ValueArg1 };
        }
    }
}
