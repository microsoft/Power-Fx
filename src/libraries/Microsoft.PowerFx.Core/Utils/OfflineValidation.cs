// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerFx.Core.Utils
{
    /// <summary>
    /// The Offline validation class.
    /// </summary>
    public static class OfflineValidation
    {
        /// <summary>
        /// The list of functions not supported offline.
        /// </summary>
        private static string[] _notSupportedFunctions = new string[]
        {
            "SaveData",
            "LoadData",
            "CountRows",
            "Download",
            "Relate",
            "Unrelate",
            "Launch",
            "LastN",
            "Last",
            "First",
            "FirstN",
            "IsMatch",
            "Match",
            "MatchAll",
            "GroupBy",
            "Sum",
            "Min",
            "Max",
            "Avg",
            "CountRows",
            "StartsWith",
            "EndsWith",
            "IsBlank",
            "Search",
            "Filter",
            "Lookup",
            "CountIf",
            "RemoveIf",
            "UpdateIf",
        };

        /// <summary>
        /// The list of Ops not supported offline.
        /// </summary>
        private static string[] _notSupportedOps = new string[]
        {
            "In",
        };

        /// <summary>
        /// Get the array of not supported functions.
        /// </summary>
        public static string[] NotSupportedFunctions
        {
            get
            {
                return _notSupportedFunctions;
            }
        }

        /// <summary>
        /// Get the array of not supported Ops.
        /// </summary>
        public static string[] NotSupportedOps
        {
            get
            {
                return _notSupportedOps;
            }
        }

        /// <summary>
        /// If function is not part of the NotSupportedFunctions array then its supported offline.
        /// </summary>
        /// <param name="functionName">The input function name.</param>
        public static bool IsFunctionSupportedOffline(string functionName)
        {
            return !NotSupportedFunctions.Contains(functionName);
        }

        /// <summary>
        /// If Operator is not part of the NotSupportedOps array then its supported offline.
        /// </summary>
        /// <param name="operatorName">The input operator name.</param>
        public static bool IsOperatorSupportedOffline(string operatorName)
        {
            return !NotSupportedOps.Contains(operatorName);
        }
    }
}
