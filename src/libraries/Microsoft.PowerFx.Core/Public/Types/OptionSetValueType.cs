// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Types
{
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

        // $$$ Add this:
        // public static bool TryGetValue(this OptionSetValueType type, string logicalName, out OptionSetValue osValue)
    }
}
