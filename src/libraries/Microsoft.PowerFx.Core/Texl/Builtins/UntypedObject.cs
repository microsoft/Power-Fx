﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal sealed class ParseJSONFunction : BuiltinFunction
    {
        public const string ParseJSONInvariantFunctionName = "ParseJSON";

        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsHidden => true;

        public ParseJSONFunction()
            : base(ParseJSONInvariantFunctionName, TexlStrings.AboutParseJSON, FunctionCategories.Text, DType.UntypedObject, 0, 1, 1, DType.String)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ParseJSONArg1 };
        }
    }

    internal sealed class IndexFunction_UO : BuiltinFunction
    {
        public const string IndexInvariantFunctionName = "Index";

        public override bool RequiresErrorContext => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsHidden => true;

        public IndexFunction_UO()
            : base(IndexInvariantFunctionName, TexlStrings.AboutIndex, FunctionCategories.Table, DType.UntypedObject, 0, 2, 2, DType.UntypedObject, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IndexArg1, TexlStrings.IndexArg2 };
        }

        public override string GetUniqueTexlRuntimeName(bool isPrefetching = false)
        {
            return GetUniqueTexlRuntimeName(suffix: "_UO");
        }
    }
}
