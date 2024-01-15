// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorErrors : SupportsConnectorErrors
    {
        public ConnectorErrors()
            : base()
        { 
        }

        public ConnectorErrors(string error)
            : base(error)
        {            
        }
    }
}
