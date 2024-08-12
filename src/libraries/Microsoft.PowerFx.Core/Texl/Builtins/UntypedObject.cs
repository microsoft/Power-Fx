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

    internal abstract class UntypedOrJSONConversionFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool HasTypeArgs => true;

        public override bool ArgIsType(int argIndex)
        {
            return argIndex == 1;
        }

        private static readonly ISet<DType> UnsupportedJSONTypes = new HashSet<DType> { DType.Color, DType.Void, DType.Time, DType.ObjNull };

        public UntypedOrJSONConversionFunction(string name, TexlStrings.StringGetter description, DType returnType, int arityMax, params DType[] paramTypes)
            : base(name, description, FunctionCategories.REST, returnType, 0, 2, arityMax, paramTypes)
        {
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length >= 2 && args.Length <= MaxArity);
            Contracts.Assert(argTypes.Length >= 2 && argTypes.Length <= MaxArity);
            Contracts.AssertValue(errors);

            if (!base.CheckTypes(context, args, argTypes, errors, out returnType, out nodeToCoercedTypeMap))
            {
                return false;
            }

            returnType = argTypes[1];
            return true;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length >= 2 && args.Length <= MaxArity);
            Contracts.Assert(argTypes.Length >= 2 && argTypes.Length <= MaxArity);
            Contracts.AssertValue(errors);

            base.CheckSemantics(binding, args, argTypes, errors);

            CheckTypeArgHasSupportedTypes(args[1], argTypes[1], errors);
        }

        private void CheckTypeArgHasSupportedTypes(TexlNode typeArg, DType type, IErrorContainer errors)
        {
            // Handle recursive types from dataverse types
            if (type.HasExpandInfo)
            {
                return;
            }

            if ((type.IsRecordNonObjNull || type.IsTableNonObjNull) && type.TypeTree != null)
            {
                type.TypeTree.ToList().ForEach(t => CheckTypeArgHasSupportedTypes(typeArg, t.Value, errors));
                return;
            }

            if (UnsupportedJSONTypes.Contains(type))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, typeArg, TexlStrings.ErrUnsupportedTypeInTypeArgument, type.Kind);
                return;
            }

            return;
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
