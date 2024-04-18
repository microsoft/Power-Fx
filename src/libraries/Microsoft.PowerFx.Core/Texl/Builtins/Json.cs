// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    // JSON(data:any, [format:s])    
    internal class JsonFunction : BuiltinFunction
    {        
        private const char _includeBinaryDataEnumValue = 'B';
        private const char _ignoreBinaryDataEnumValue = 'G';
        private const char _ignoreUnsupportedTypesEnumValue = 'I';
        private const char _flattenTableValuesEnumValue = '_';
        private const char _indentFourEnumValue = '4';

        protected bool supportsLazyTypes = false;

        private static readonly DKind[] _unsupportedTopLevelTypes = new[]
        {
            DKind.DataEntity,
            DKind.LazyRecord,
            DKind.LazyTable,
            DKind.View, 
            DKind.ViewValue
        };

        private static readonly DKind[] _unsupportedTypes = new[]
        {
            DKind.Control, 
            DKind.LazyRecord,
            DKind.LazyTable,
            DKind.Metadata,
            DKind.OptionSet, 
            DKind.PenImage, 
            DKind.Polymorphic,
            DKind.UntypedObject,
            DKind.Void
        };

        public override bool IsSelfContained => true;

        public override bool IsAsync => true;       

        public override bool SupportsParamCoercion => false;

        public JsonFunction()
            : base("JSON", TexlStrings.AboutJSON, FunctionCategories.Text, DType.String, 0, 1, 2, DType.EmptyTable, BuiltInEnums.JSONFormatEnum.FormulaType._type)
        {
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { TexlStrings.JSONArg1 };
            yield return new[] { TexlStrings.JSONArg1, TexlStrings.JSONArg2 };
        }

        public override IEnumerable<string> GetRequiredEnumNames()
        {
            return new List<string>() { LanguageConstants.JSONFormatEnumString };
        }

        public override bool CheckTypes(CheckTypesContext context, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            returnType = DType.String;
            nodeToCoercedTypeMap = null;

            // Do not call base.CheckTypes for arg0
            if (args.Length > 1)
            {
                if (context.Features.StronglyTypedBuiltinEnums && 
                    !base.CheckType(context, args[1], argTypes[1], BuiltInEnums.JSONFormatEnum.FormulaType._type, errors, ref nodeToCoercedTypeMap))
                {
                    return false;
                }

                TexlNode optionsNode = args[1];                
                if (!IsConstant(context, argTypes, optionsNode, out string nodeValue))
                {
                    errors.EnsureError(optionsNode, TexlStrings.ErrFunctionArg2ParamMustBeConstant, "JSON", TexlStrings.JSONArg2.Invoke());
                    return false;
                }
            }

            return true;
        }

        public override void CheckSemantics(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors)
        {
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.Assert(args.Length >= 1 && args.Length <= 2);
            Contracts.AssertValue(errors);

            DType dataArgType = argTypes[0];
            TexlNode dataNode = args[0];

            supportsLazyTypes = binding.Features.JsonFunctionAcceptsLazyTypes;

            if (_unsupportedTopLevelTypes.Contains(dataArgType.Kind) || _unsupportedTypes.Contains(dataArgType.Kind))
            {
                errors.EnsureError(dataNode, TexlStrings.ErrJSONArg1UnsupportedType, dataArgType.GetKindString());
                return;
            }

            bool includeBinaryData = false;
            bool ignoreUnsupportedTypes = false;
            bool ignoreBinaryData = false;            

            if (args.Length > 1)
            {
                TexlNode optionsNode = args[1];                
                if (!IsConstant(binding.CheckTypesContext, argTypes, optionsNode, out string nodeValue))
                {
                    return;
                }

                if (nodeValue != null)
                {
                    foreach (var option in nodeValue)
                    {
                        switch (option)
                        {
                            case _ignoreBinaryDataEnumValue:
                                ignoreBinaryData = true;
                                break;
                            case _ignoreUnsupportedTypesEnumValue:
                                ignoreUnsupportedTypes = true;
                                break;
                            case _includeBinaryDataEnumValue:
                                includeBinaryData = true;
                                break;
                            case _flattenTableValuesEnumValue:
                            case _indentFourEnumValue:
                                // Runtime-only options
                                break;
                            default:
                                if (binding.Features.PowerFxV1CompatibilityRules)
                                {
                                    errors.EnsureError(optionsNode, TexlStrings.ErrJSONArg2UnsupportedOption, option);
                                    return;
                                }

                                break;
                        }
                    }

                    if (includeBinaryData && ignoreBinaryData)
                    {
                        errors.EnsureError(optionsNode, TexlStrings.ErrJSONArg2IncompatibleOptions, "JSONFormat.IgnoreBinaryData", "JSONFormat.IncludeBinaryData");
                        return;
                    }
                }

                if (!binding.CheckTypesContext.AllowsSideEffects && includeBinaryData)
                {
                    errors.EnsureError(optionsNode, TexlStrings.ErrJSONArg1UnsupportedTypeWithNonBehavioral);
                    return;
                }
            }

            bool hasMedia = DataHasMedia(argTypes[0]);

            if (hasMedia && !includeBinaryData && !ignoreBinaryData)
            {
                errors.EnsureError(args[0], TexlStrings.ErrJSONArg1ContainsUnsupportedMedia);
                return;
            }

            if (!ignoreUnsupportedTypes)
            {
                if (HasUnsupportedType(dataArgType, supportsLazyTypes, out DType unsupportedNestedType, out var unsupportedColumnName))
                {
                    errors.EnsureError(dataNode, TexlStrings.ErrJSONArg1UnsupportedNestedType, unsupportedColumnName, unsupportedNestedType.GetKindString());
                }
            }            
        }

        private static bool IsConstant(CheckTypesContext context, DType[] argTypes, TexlNode optionsNode, out string nodeValue)
        {
            // Not limited to strings, can be an option set (enum)
            return BinderUtils.TryGetConstantValue(context, optionsNode, out nodeValue);
        }

        public override bool HasSuggestionsForParam(int argumentIndex)
        {
            Contracts.Assert(argumentIndex >= 0);
            return argumentIndex == 1;
        }

#pragma warning disable SA1629 // Documentation text should end with a period (false positive)

        /// <summary>
        /// Checks whether the given DType contains a property of an unsupported type.
        /// </summary>
        /// <param name="argType">Type DType to check.</param>
        /// <param name="supportsLazyTypes">Supports LazyRecord and LazyTable types.</param>
        /// <param name="unsupportedType">If the function returns. <code>true</code>, the unsupported type.</param>
        /// <param name="unsupportedColumnName">If the function returns <code>true</code>, the column name with the unsupported type.</param>
        /// <returns><code>true</code> if the given type contains a nested property of an unsupported type; <code>false</code> otherwise.</returns>
        internal static bool HasUnsupportedType(DType argType, bool supportsLazyTypes, out DType unsupportedType, out string unsupportedColumnName)
        {
            return HasUnsupportedTypeInternal(argType, supportsLazyTypes, 0, out unsupportedType, out unsupportedColumnName);
        }

        private static bool HasUnsupportedTypeInternal(DType argType, bool supportsLazyTypes, int depth, out DType unsupportedType, out string unsupportedColumnName)
        {
            bool isLazyRecordOrTable = supportsLazyTypes && (argType.Kind == DKind.LazyRecord || argType.Kind == DKind.LazyTable);

            if (depth > 40 || (!isLazyRecordOrTable && _unsupportedTypes.Contains(argType.Kind)))
            {
                unsupportedType = argType;
                unsupportedColumnName = null; // root
                return true;
            }

            if (isLazyRecordOrTable || argType.Kind == DKind.Record || argType.Kind == DKind.Table)
            {
                foreach (TypedName child in argType.GetAllNames(DPath.Root))
                {
                    if (HasUnsupportedTypeInternal(child.Type, supportsLazyTypes, depth + 1, out unsupportedType, out unsupportedColumnName))
                    {
                        unsupportedColumnName = child.Name.Value;
                        return true;
                    }
                }
            }

            unsupportedType = default;
            unsupportedColumnName = null;
            return false;
        }

        /// <summary>
        /// Checks whether the given DType contains a media property.
        /// </summary>
        /// <param name="argType">Tye DType to check for the presence of a media property.</param>
        /// <returns><code>true</code> if the given type contains media type columns; <code>false</code> otherwise.</returns>
        internal static bool DataHasMedia(DType argType)
        {
            switch (argType.Kind)
            {
                case DKind.Media:
                case DKind.Blob:
                case DKind.LegacyBlob:
                case DKind.Image:
                    return true;
                case DKind.Record:
                case DKind.Table:
                    foreach (TypedName child in argType.GetAllNames(DPath.Root))
                    {
                        if (DataHasMedia(child.Type))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

#pragma warning restore SA1629 
    }
}
