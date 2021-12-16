// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation.DelegationMetadata
{
    class ODataOpMetadata : OperationCapabilityMetadata
    {
        private Dictionary<DPath, DPath> _oDataReplacement;

        public ODataOpMetadata(DType schema, Dictionary<DPath, DPath> oDataReplacement)
            : base(schema)
        {
            Contracts.AssertValue(oDataReplacement);

            _oDataReplacement = oDataReplacement;
        }

        public override Dictionary<DPath, DPath> QueryPathReplacement {  get { return _oDataReplacement; } }

        public override DelegationCapability DefaultColumnCapabilities { get { return DelegationCapability.None; } }

        public override DelegationCapability TableCapabilities { get { return DefaultColumnCapabilities; } }

    }
}
