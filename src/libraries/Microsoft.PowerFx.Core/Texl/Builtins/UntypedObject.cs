// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class ParseJSONFunction : BuiltinFunction
    {
        public const string ParseJSONInvariantFunctionName = "ParseJSON";

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

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

        public override bool IsSelfContained => true;

        public override bool PropagatesMutability => true;

        public IndexFunction_UO()
            : base(IndexInvariantFunctionName, TexlStrings.AboutIndex, FunctionCategories.Table, DType.UntypedObject, 0, 2, 2, DType.UntypedObject, DType.Number)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.IndexArg1, TexlStrings.IndexArg2 };
        }
    }

    // ParseJSON(JsonString:s, Type:U): ?
    internal class TypedParseJSONFunction : UntypedOrJSONConversionFunction
    {
        public TypedParseJSONFunction()
            : base(ParseJSONFunction.ParseJSONInvariantFunctionName, TexlStrings.AboutTypedParseJSON, DType.Error, 2, DType.String, DType.Error)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TypedParseJSONArg1, TexlStrings.TypedParseJSONArg2 };
        }
    }
}
