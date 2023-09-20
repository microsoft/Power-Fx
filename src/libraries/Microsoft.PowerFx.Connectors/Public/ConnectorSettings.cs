// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Settings for a connector.
    /// </summary>
    public class ConnectorSettings 
    {        
        public ConnectorSettings(string @namespace)
        {
            Namespace = @namespace;
            Validate();
        }

        /// <summary>
        /// Namespace of the connector.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// NumberIsFloat.
        /// </summary>
        [Obsolete("This shouldn't be used anymore.")]
        public bool NumberIsFloat { get; init; } = false;

        /// <summary>
        /// Maximum number of rows to return, per page.
        /// </summary>
        public int MaxRows { get; init; } = 1000;

        /// <summary>
        /// Unknown extensions in swagger file will be ignored during the validation process.
        /// </summary>
        public bool IgnoreUnknownExtensions { get; init; } = false;

        /// <summary>
        /// Allow using functions that are identified as unsupported.
        /// NotSupportedReason property will still be specified.
        /// </summary>
        public bool AllowUnsupportedFunctions { get; init; } = false;        
        
        public void Validate()
        {
            if (Namespace == null)
            {
                throw new ArgumentNullException(nameof(Namespace), "Namespace cannot be null");
            }

            if (!DName.IsValidDName(Namespace))
            {
                throw new ArgumentException(nameof(Namespace), $"invalid functionNamespace: {Namespace}");
            }
        }
    }
}
