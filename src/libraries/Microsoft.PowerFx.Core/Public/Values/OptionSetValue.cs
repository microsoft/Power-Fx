// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.PowerFx.Core.IR;

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
            return Option;
        }

        public override string ToString()
        {
            return $"OptionSetValue ({Option})";
        }

        public override void Visit(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
