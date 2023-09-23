// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Microsoft.PowerFx
{
    // Override to control formatting. 
    // - will get very interesting for dataverse values. 
    public class ValueFormatter
    {
        public FormulaValueSerializerSettings Settings { get; set; } = new FormulaValueSerializerSettings
        {
             UseCompactRepresentation = true
        };

        public virtual string Format(FormulaValue value)
        {
            var sb = new StringBuilder();
            value.ToExpression(sb, this.Settings);
            return sb.ToString();
        }
    }
}
