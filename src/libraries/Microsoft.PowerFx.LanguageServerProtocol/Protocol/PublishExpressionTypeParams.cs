// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class PublishExpressionTypeParams
    {
        /// <summary>
        /// The URI for which the expression type is reported.
        /// </summary>
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// The detected type of the expression.
        /// </summary>
        public FormulaType Type { get; set; }
    }
}
