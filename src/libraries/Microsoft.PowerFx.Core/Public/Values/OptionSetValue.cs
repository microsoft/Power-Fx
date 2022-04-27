// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Public.Values
{
    [DebuggerDisplay("OptionSetValue ({Option})")]
    public class OptionSetValue : FormulaValue
    {
        /// <summary>
        /// Logical name for this option set value.
        /// </summary>
        public readonly string Option;

        internal OptionSetValue(string option, IRContext irContext)
            : base(irContext)
        {
            Option = option;
        }

        public override object ToObject()
        {
            // $$$ Potential break?
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

        // $$$ Add Type property that returns OptionSetValueType
        // $$$ add ctor that skips IRContext.

        /// <summary>
        /// Get the display name for this value. If no display name is available, 
        /// returns the logical name <see cref="Option"/>.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var osvt = (OptionSetValueType)Type;
                var info = osvt._type.OptionSetInfo;

                var dp = info.DisplayNameProvider;

                var logicalName = Option;
                if (dp.TryGetDisplayName(new DName(logicalName), out var displayName))
                {
                    return displayName.Value;
                }

                return logicalName;
            }
        }        
    }
}
