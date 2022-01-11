// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public abstract class AggregateType : FormulaType
    {
        internal AggregateType(DType type)
            : base(type)
        {
        }

        // Enumerate fields
        public IEnumerable<NamedFormulaType> GetNames()
        {
            var names = _type.GetAllNames(DPath.Root);
            return from name in names select new NamedFormulaType(name);
        }
    }
}
