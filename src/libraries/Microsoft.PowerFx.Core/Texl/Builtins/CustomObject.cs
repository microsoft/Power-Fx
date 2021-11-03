// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal sealed class ParseJsonFunction : BuiltinFunction
    {
        public const string ParseJsonInvariantFunctionName = "ParseJson";

        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsHidden => true;

        public ParseJsonFunction()
            : base(ParseJsonInvariantFunctionName, TexlStrings.AboutParseJson, FunctionCategories.CustomObject, DType.CustomObject, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ParseJsonArg1 };
        }
    }

    internal sealed class GetFieldFunction : BuiltinFunction
    {
        public const string GetFieldInvariantFunctionName = "GetField";

        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsHidden => true;

        public GetFieldFunction()
            : base(GetFieldInvariantFunctionName, TexlStrings.AboutGetField, FunctionCategories.CustomObject, DType.CustomObject, 0, 2, 2, DType.CustomObject, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GetFieldArg1, TexlStrings.GetFieldArg2 };
        }
    }

    internal sealed class GetAtFunction : BuiltinFunction
    {
        public const string GetAtInvariantFunctionName = "GetAt";

        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsHidden => true;

        public GetAtFunction()
            : base(GetAtInvariantFunctionName, TexlStrings.AboutGetAt, FunctionCategories.CustomObject, DType.CustomObject, 0, 2, 2, DType.CustomObject, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.GetAtArg1, TexlStrings.GetAtArg2 };
        }
    }
}
