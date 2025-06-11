// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Functions.Delegation;

namespace Microsoft.PowerFx.Connectors
{
    internal class Utilities
    {
        public static IEnumerable<DelegationOperator> ToDelegationOp(IEnumerable<string> filterFunctionList)
        {
            if (filterFunctionList == null)
            {
                return null;
            }

            List<DelegationOperator> list = new List<DelegationOperator>();

            foreach (string str in filterFunctionList)
            {
                if (Enum.TryParse(str, true, out DelegationOperator op))
                {
                    list.Add(op);
                }
            }

            return list;
        }
    }
}
