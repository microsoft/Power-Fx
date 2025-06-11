// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Connectors
{
    /// <summary>
    /// Interface for CDP aggregate metadata.
    /// </summary>
    public interface ICDPAggregateMetadata
    {
        /// <summary>
        /// Tries to get sensitivity label information.
        /// </summary>
        /// <param name="cdpSensitivityLabelInfo">The sensitivity label info if available.</param>
        /// <returns>True if sensitivity label info is available; otherwise, false.</returns>
        bool TryGetSensitivityLabelInfo(out IEnumerable<CDPSensitivityLabelInfo> cdpSensitivityLabelInfo);
    }
}
