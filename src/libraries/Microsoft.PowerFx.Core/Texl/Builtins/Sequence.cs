// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sequence(records:n, start:n, step:n): *[Value:n]
    internal sealed class SequenceFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgPreprocessor(int index)
        {
            return ArgPreprocessor.ReplaceBlankWithCallZero_SingleColumnTable;
        }

        public override bool IsSelfContained => true;

        public SequenceFunction()
            : base("Sequence", TexlStrings.AboutSequence, FunctionCategories.MathAndStat, DType.Unknown, 0, 1, 3, DType.Unknown, DType.Unknown, DType.Unknown)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SequenceArg1 };
            yield return new[] { TexlStrings.SequenceArg1, TexlStrings.SequenceArg2 };
            yield return new[] { TexlStrings.SequenceArg1, TexlStrings.SequenceArg2, TexlStrings.SequenceArg3 };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = new Dictionary<TexlNode, DType>();
            var fArgsValid = true;

            // first argument is the count, but does not determine the type of result
            // second argument (if present) determines the type and all other args are coerced to this type
            var returnScalarType = args.Length == 1 ? 
                            DType.Number : // Decimal TODO (context.NumberIsFloat ? DType.Number : DType.Decimal) :
                            DetermineNumericFunctionReturnType(nativeDecimal: true, context.NumberIsFloat, argTypes[1]);
            returnType = DType.CreateTable(new TypedName(returnScalarType, new DName("Value")));

            // Ensure that all the arguments are numeric/coercible to numeric.
            for (var i = 0; i < argTypes.Length; i++)
            {
                if (!CheckType(args[i], argTypes[i], returnScalarType, DefaultErrorContainer, ref nodeToCoercedTypeMap))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[i], TexlStrings.ErrNumberExpected);
                    fArgsValid = false;
                }
            }

            if (!fArgsValid)
            {
                nodeToCoercedTypeMap = null;
            }

            return fArgsValid;
        }
    }
}
