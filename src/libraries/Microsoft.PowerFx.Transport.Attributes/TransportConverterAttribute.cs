// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.AppMagic.Transport
{
    /// <summary>
    /// Applied to a converter class. Provides a conversion for a custom type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TransportConverterAttribute : Attribute
    {
        public TransportConverterAttribute(
            Type originalType,
            Type surrogateType,
            string alternateTypescriptType = null,
            string alternateTypescriptConverter = null,
            string cSharpPredicateForOmittingFromDto = null,
            string csharpQuotedJsonStringExpression = null,
            string typescriptPropertyStateSyncExpression = null,
            string typescriptValueExpressionForUndefinedValueAppearingInDto = null)
        {
            OriginalType = originalType;
            SurrogateType = surrogateType;
            AlternateTypescriptType = alternateTypescriptType;
            AlternateTypescriptConverter = alternateTypescriptConverter;
            PropertyStateSyncDefaultValue = new PropertyStateSyncDefaultValueExpression
            {
                CSharpQuotedJsonStringExpression = csharpQuotedJsonStringExpression,
                TypescriptPropertyStateSyncExpression = typescriptPropertyStateSyncExpression
            };
            MethodResultDefaultValue = new DtoUndefinedValueMapping
            {
                CSharpPredicateForOmittingFromDto = cSharpPredicateForOmittingFromDto,
                TypescriptValueExpressionForUndefinedValueAppearingInDto = typescriptValueExpressionForUndefinedValueAppearingInDto
            };
        }

        /// <summary>
        /// Type being converted from. This is the type that will appear in the user-visible api.
        /// </summary>
        public Type OriginalType { get; }

        /// <summary>
        /// Type being converted to. This will be used as the wire format when OriginalType is passed across transport.
        /// </summary>
        public Type SurrogateType { get; }

        /// <summary>
        /// Allows choosing a different name in Typescript for 'OriginalType'. If null, uses the C# type name.
        /// </summary>
        public string AlternateTypescriptType { get; }

        /// <summary>
        /// Allows choosing a different name in Typescript for the converter type. If null, uses the C# type name.
        /// </summary>
        public string AlternateTypescriptConverter { get; }

        /// <summary>
        /// If set, indicates that property state synchronization should assume a particular default wire value in CSharp and Typescript for this type,
        /// instead of null.
        /// </summary>
        public PropertyStateSyncDefaultValueExpression PropertyStateSyncDefaultValue { get; }

        /// <summary>
        /// If set, indicates that method result should assume a particular default wire value in CSharp and Typescript for this type,
        /// instead of null.
        /// </summary>
        public DtoUndefinedValueMapping MethodResultDefaultValue { get; }
    }
}
