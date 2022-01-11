// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.AppMagic.Transport
{
    public sealed class PropertyStateSyncDefaultValueExpression
    {
        /// <summary>
        /// If set, indicates that property state synchronization should assume a particular default wire value in CSharp for this type,
        /// instead of null.
        /// </summary>
        public string CSharpQuotedJsonStringExpression { get; set; }

        /// <summary>
        /// If set, indicates that property state synchronization should assume a particular default wire value in Typescript for this type,
        /// instead of null.
        /// </summary>
        public string TypescriptPropertyStateSyncExpression { get; set; }
    }
}
