// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Functions.OData;

namespace Microsoft.PowerFx.Connectors.Internal
{
    // Processes OData commands static analysis during Check operation
    internal class ODataCommandProcessor
    {
        internal TabularDType TabularDType;

        internal List<ODataCommand> ODataCommands;

        internal ODataCommandProcessor(TabularDType tdt)
        {
            TabularDType = tdt;
            ODataCommands = new List<ODataCommand>();
        }

        public bool TryAddODataCommand(ODataCommand command)
        {

            if (command == null)
            {
                return false;
            }

            switch (TabularDType.Protocol)
            {                
                case TabularProtocol.Cdp:
                    ODataCommands.Add(command);
                    return true;

                // No delegation
                case TabularProtocol.None:
                    return false;

                // Not supported yet
                case TabularProtocol.OData:
                case TabularProtocol.Sql:
                default:
                    return false;
            }
        }
    }
}
