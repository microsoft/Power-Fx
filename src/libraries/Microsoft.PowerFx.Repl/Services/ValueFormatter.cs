// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Repl.Services
{
    /// <summary>
    /// Format <see cref="FormulaValue"/> results to show in output. 
    /// </summary>
    public class ValueFormatter
    {
        public FormulaValueSerializerSettings Settings { get; set; } = new FormulaValueSerializerSettings
        {
            UseCompactRepresentation = true
        };

        public virtual string Format(FormulaValue value)
        {
            var sb = new StringBuilder();
            value.ToExpression(sb, Settings);
            return sb.ToString();
        }
    }
}
