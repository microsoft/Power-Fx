// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
    /// <summary>
    /// Power Fx type for an enum-like things such as OptionSets. 
    /// </summary>
    public class OptionSetValueType : FormulaType
    {
        /// <summary>
        /// The name of the source Option Set for this type.
        /// </summary>
        public DName OptionSetName => _type.OptionSetInfo?.EntityName ?? default;

        internal OptionSetValueType(IExternalOptionSet optionSet)
            : base(DType.CreateOptionSetValueType(optionSet))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionSetValueType"/> class.
        /// Internal use only. Used by legacy clients to represent an un-backed option set, and should be removed.
        /// </summary>
        internal OptionSetValueType()
            : base(new DType(DKind.OptionSetValue))
        {
        }

        public override void Visit(ITypeVistor vistor)
        {
            vistor.Visit(this);
        }

        /// <summary>
        /// List of logical names within this option set. 
        /// </summary>
        public IEnumerable<DName> LogicalNames => _type.OptionSetInfo.OptionNames; 

        /// <summary>
        /// Try to get a value given the logical name. 
        /// </summary>
        /// <param name="logicalName"></param>
        /// <param name="osValue"></param>
        /// <returns>False if logical name is not in the option set. </returns>
        public bool TryGetValue(string logicalName, out OptionSetValue osValue)
        {
            var info = _type.OptionSetInfo;

            // Verify this value exists in the option set. 
            if (info.DisplayNameProvider.TryGetDisplayName(new DName(logicalName), out var displayName))
            {
                osValue = new OptionSetValue(logicalName, this);
                return true;
            }

            osValue = null;
            return false;
        }
    }
}
