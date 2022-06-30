// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.AppMagic.Transport
{
    public sealed class DtoUndefinedValueMapping
    {
        /// <summary>
        /// If set, indicates the expression to get the default type value in the C#.
        /// </summary>
        public string CSharpPredicateForOmittingFromDto { get; set; }

        /// <summary>
        /// If set, indicates that method result should assume a particular default wire value in Typescript for this type,
        /// instead of null.
        /// </summary>
        public string TypescriptValueExpressionForUndefinedValueAppearingInDto { get; set; }
    }
}
