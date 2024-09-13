// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Connectors
{
    internal static class ServiceCapabilitiesExtensions
    {
        public static IList<string> GetNonFilterableProperties(this ServiceCapabilities serviceCapabilities)
        {
            return serviceCapabilities == null
                ? new List<string>()
                : serviceCapabilities.FilterRestriction.NonFilterableProperties;
        }

        public static IList<string> GetColumnFilterFunctions(this ServiceCapabilities serviceCapabilities, string columnName)
        {
            if (serviceCapabilities == null || serviceCapabilities._columnsCapabilities == null || !serviceCapabilities._columnsCapabilities.TryGetValue(columnName, out ColumnCapabilitiesBase columnCapabilitiesBase))
            {
                return null;
            }
             
            if (columnCapabilitiesBase is ColumnCapabilities columnCapabilities)
            {
                return columnCapabilities.Capabilities.FilterFunctions;                
            }

            // ComplexColumnCapabilities not supported yet
            return new List<string>();
        }
    }
}
