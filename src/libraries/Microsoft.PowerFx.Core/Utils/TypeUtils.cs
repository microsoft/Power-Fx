// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class TypeUtils
    {
        public static bool AggregateHasExpandedType(this DType self)
        {
            var ret = false;

            if (self.IsAggregate)
            {
                var record = self.ToRecord();

                ret = record.GetAllNames(DPath.Root).Any(name => name.Type.IsExpandEntity);
            }

            return ret;
        }
    }
}
