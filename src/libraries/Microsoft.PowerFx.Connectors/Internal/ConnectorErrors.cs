// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Connectors
{
    internal class ConnectorErrors : SupportsConnectorErrors
    {
        public ConnectorErrors()
            : base()
        { 
        }

        public ConnectorErrors(string error, ExpressionError warning = default)
            : base(error, warning)
        {            
        }
    }
}
