// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.UtilityDataStructures;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    public abstract class AggregateType : FormulaType
    {
        internal AggregateType(DType type, Dictionary<string, string> displayNameSet = null) : base(type, displayNameSet)
        {
        }

        // Enumerate fields
        public IEnumerable<NamedFormulaType> GetNames()
        {
            IEnumerable<TypedName> names = _type.GetAllNames(DPath.Root);
            return from name in names select new NamedFormulaType(name);
        }
    }
}
