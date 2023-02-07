// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // Sequence(records:n, start:n, step:n): *[Value:n]
    internal sealed class SequenceFunction : BuiltinFunction
    {
        public override ArgPreprocessor GetArgBlankHandlerPolicy(int index)
        {
            return base.GetDefaultArgBlankHandlerPolicy(index);
        }

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => true;

        public SequenceFunction()
            : base("Sequence", TexlStrings.AboutSequence, FunctionCategories.MathAndStat, DType.CreateTable(new TypedName(DType.Number, new DName("Value"))), 0, 1, 3, DType.Number, DType.Number, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.SequenceArg1 };
            yield return new[] { TexlStrings.SequenceArg1, TexlStrings.SequenceArg2 };
            yield return new[] { TexlStrings.SequenceArg1, TexlStrings.SequenceArg2, TexlStrings.SequenceArg3 };
        }
    }
}
