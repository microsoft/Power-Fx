// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// A value within an option set. 
    /// </summary>
    [DebuggerDisplay("{ToString()})")]
    public class OptionSetValue : ValidFormulaValue
    {
        /// <summary>
        /// Logical name for this option set value.
        /// </summary>
        public string Option { get; private set; }

        internal OptionSetValue(string option, OptionSetValueType type)
            : base(IRContext.NotInSource(type))
        {
            Option = option;
        }

        public new OptionSetValueType Type => (OptionSetValueType)base.Type;

        public override object ToObject()
        {
            return DisplayName;
        }

        public override string ToString()
        {
            return $"OptionSetValue ({Option}={DisplayName})";
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
                
        /// <summary>
        /// Get the display name for this value. If no display name is available, 
        /// returns the logical name <see cref="Option"/>.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var info = Type.Type.OptionSetInfo;

                var displayNameProvider = info.DisplayNameProvider;

                var logicalName = Option;
                if (displayNameProvider.TryGetDisplayName(new DName(logicalName), out var displayName))
                {
                    return displayName.Value;
                }

                return logicalName;
            }
        }        
    }
}
