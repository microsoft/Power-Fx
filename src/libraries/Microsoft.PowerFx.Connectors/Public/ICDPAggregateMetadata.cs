// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Connectors
{
    public interface ICDPAggregateMetadata
    {
        bool TryGetSensitivityLabelInfo(out IEnumerable<CDPSensitivityLabelInfo> cdpSensitivityLabelInfo);

        bool TryGetMetadataItems(out IEnumerable<CDPMetadataItem> cdpMetadataItems);
    }
}
