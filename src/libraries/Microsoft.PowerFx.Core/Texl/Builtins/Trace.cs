// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal sealed class TraceFunction : BuiltinFunction
    {
        public override bool IsSelfContained => false;

        internal const string IgnoreUnsupportedTypesEnumValue = "I";

        public override bool IsAsync => true;
        
        public override bool SupportsParamCoercion => true;

        public TraceFunction()
            : base("Trace", TexlStrings.AboutTrace, FunctionCategories.Behavior, DType.Boolean /* maybe change to void */, 0, 1, 4, DType.String, BuiltInEnums.TraceSeverityEnum.FormulaType._type, DType.EmptyRecord, BuiltInEnums.TraceOptionsEnum.FormulaType._type)
        { 
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.TraceSeverityEnumString, LanguageConstants.TraceOptionsEnumString };
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.TraceArg1 };
            yield return new[] { TexlStrings.TraceArg1, TexlStrings.TraceArg2 };
            yield return new[] { TexlStrings.TraceArg1, TexlStrings.TraceArg2, TexlStrings.TraceArg3 };
            yield return new[] { TexlStrings.TraceArg1, TexlStrings.TraceArg2, TexlStrings.TraceArg3, TexlStrings.TraceArg4 };
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 1 && args.Length <= 4);
            Contracts.AssertValue(errors);

            if (argTypes.Length > 2)
            {
                bool ignoreUnsupportedTypes = false;
                if (argTypes.Length > 3)
                {
                    TexlNode optionsNode = args[3];
                    if (!BinderUtils.TryGetConstantValue(binding.CheckTypesContext, optionsNode, out var nodeValue) ||
                        !(argTypes[3].Kind == DKind.String || argTypes[3].Kind == DKind.OptionSetValue) ||
                        !binding.IsConstant(optionsNode))
                    {
                        errors.EnsureError(
                            optionsNode,
                            TexlStrings.ErrFunctionArg2ParamMustBeConstant,
                            "Trace",
                            TexlStrings.TraceArg4.Invoke());
                        return;
                    }

                    ignoreUnsupportedTypes = nodeValue.Contains(IgnoreUnsupportedTypesEnumValue);
                }

                // We have a 'custom_dimensions' parameter; check that it has only valid property types
                if (!ignoreUnsupportedTypes && !CheckCustomDimensionsTypes(args[2], errors, argTypes[2]))
                {
                    errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrInvalidArgs_Func, Name);
                }
            }
        }

        /// <summary>
        /// Checks that the types of the properties of the custom dimensions objects are supported.
        /// </summary>
        /// <param name="customDimensionsNode">The node with the custom dimensions object.</param>
        /// <param name="errors">Error container to add an error if one is found in this method.</param>
        /// <param name="customDimensionDType"><see cref="DType"/> for the custom dimensions parameter.</param>
        /// <returns> returns true if the properties of the custom dimensions object are all valid false otherwise.</returns>
        private bool CheckCustomDimensionsTypes(TexlNode customDimensionsNode, IErrorContainer errors, DType customDimensionDType)
        {
            switch (customDimensionDType.Kind)
            {
                case DKind.Boolean:
                case DKind.Color:
                case DKind.Currency:
                case DKind.Date:
                case DKind.DateTime:
                case DKind.DateTimeNoTimeZone:
                case DKind.Enum:
                case DKind.Guid:
                case DKind.Hyperlink:
                case DKind.Number:
                case DKind.ObjNull:
                case DKind.OptionSetValue:
                case DKind.String:
                case DKind.Time:
                case DKind.Decimal: /* not in PA */
                    return true;
                case DKind.Record:
                case DKind.Table:
                    foreach (TypedName child in customDimensionDType.GetAllNames(DPath.Root))
                    {
                        if (!CheckCustomDimensionsTypes(customDimensionsNode, errors, child.Type))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                     // Other types are not supported
                    errors.EnsureError(customDimensionsNode, TexlStrings.ErrTraceInvalidCustomRecordType, TexlStrings.TraceArg3.Invoke(), customDimensionDType.GetKindString());
                    return false;
            }
        }

        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);

            return argumentIndex is 1 or 3;
        }
    }
}
