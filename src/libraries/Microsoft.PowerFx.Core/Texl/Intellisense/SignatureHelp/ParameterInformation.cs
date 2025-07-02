// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    /// <summary>
    /// Represents information about a function parameter for signature help.
    /// </summary>
    public class ParameterInformation
    {
        /// <summary>
        /// Gets or sets the label of the parameter.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the documentation for the parameter.
        /// </summary>
        public string Documentation { get; set; }
    }
}
