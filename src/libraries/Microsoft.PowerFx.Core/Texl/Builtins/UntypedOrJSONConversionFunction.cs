// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal abstract class UntypedOrJSONConversionFunction : BuiltinFunction
    {
        public override bool IsSelfContained => true;

        public override bool SupportsParamCoercion => false;

        public override bool HasTypeArgs => true;

        public override bool ArgIsType(int argIndex)
        {
            Contracts.Assert(HasTypeArgs);
            return argIndex == 1;
        }

        public override bool HasSuggestionsForParam(int argIndex)
        {
            return argIndex == 1;
        }

        // This list is intersected with Engine.PrimitiveTypes, which the parser will only let through.
        // In other words, just because a type is listed here, does not mean it is supported unless it is also in Engine.PrimitiveTypes.
        // Notably this list excludes ObjNull and Void, which make no sense to use in this context.
        // It also excludes Color, as we have not implemented the coversion of "Red" and other color names.
        internal static readonly ISet<DType> SupportedJSONTypes = new HashSet<DType> { DType.Boolean, DType.Number, DType.Decimal, DType.Date, DType.DateTime, DType.DateTimeNoTimeZone, DType.Time, DType.String, DType.Guid, DType.Hyperlink, DType.UntypedObject };

        internal static readonly ISet<DType> UnSupportedJSONTypes = new HashSet<DType> { DType.Color, DType.ObjNull, DType.Void };

        public UntypedOrJSONConversionFunction(string name, TexlStrings.StringGetter description, DType returnType, int arityMax, params DType[] paramTypes)
            : base(name, description, FunctionCategories.Text, returnType, 0, 2, arityMax, paramTypes)
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

            // check if the given type argument is not supported
            if (!DType.IsSupportedType(argTypes[1], SupportedJSONTypes, out var unsupportedType))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[1], TexlStrings.ErrUnsupportedTypeInTypeArgument, unsupportedType.GetKindString());
            }
        }
    }
}
