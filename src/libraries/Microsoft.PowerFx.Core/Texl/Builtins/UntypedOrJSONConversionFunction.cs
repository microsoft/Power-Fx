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

        private static readonly ISet<DType> SupportedJSONTypes = new HashSet<DType> { DType.Boolean, DType.Number, DType.Decimal, DType.Date, DType.DateTime, DType.DateTimeNoTimeZone, DType.Time, DType.String, DType.Guid, DType.Hyperlink, DType.UntypedObject };

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

            CheckTypeArgHasSupportedTypes(args[1], argTypes[1], errors);
        }

        private void CheckTypeArgHasSupportedTypes(TexlNode typeArg, DType type, IErrorContainer errors)
        {
            // Dataverse types may contain fields with ExpandInfo that may have self / mutually recursive reference
            // we allow these in type check phase by ignoring validation of types in such fields.
            if (type.HasExpandInfo)
            {
                return;
            }

            if ((type.IsRecordNonObjNull || type.IsTableNonObjNull) && type.TypeTree != null)
            {
                type.TypeTree.ToList().ForEach(t => CheckTypeArgHasSupportedTypes(typeArg, t.Value, errors));
                return;
            }

            if (!SupportedJSONTypes.Contains(type))
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, typeArg, TexlStrings.ErrUnsupportedTypeInTypeArgument, type.Kind);
                return;
            }
        }
    }
}
