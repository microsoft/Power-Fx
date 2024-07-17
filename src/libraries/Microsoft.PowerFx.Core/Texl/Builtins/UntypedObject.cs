// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

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

    // ParseJSON(ObjectString:P, Type:*[]): ![]
    internal class ParseJSONWithType : BuiltinFunction
    {
        public const string ParseJsonInvariantFunctionName = "ParseJSON";

        public override bool IsAsync => true;

        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool IsRestrictedUDFName => true;

        public override bool HasTypeArgs => true;

        public override bool ArgIsType(int argIndex)
        {
            return argIndex == 1;
        }

        public ParseJSONWithType()
            : base(ParseJsonInvariantFunctionName, TexlStrings.AboutParseJSON, FunctionCategories.REST, DType.Error, 0, 2, 2, DType.String, DType.Error)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.ParseJSONArg1, TexlStrings.AsTypeArg2 };
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

            returnType = argTypes[1];
            return true;
        }
    }
}
