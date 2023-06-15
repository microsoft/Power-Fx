// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static async ValueTask<FormulaValue> Language(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            return Language(runner, irContext);
        }

        public static StringValue Language(EvalVisitor runner, IRContext irContext)
        {
            return new StringValue(irContext, Language(runner.CultureInfo));
        }

        public static string Language(CultureInfo culture)
        {
            return string.IsNullOrEmpty(culture?.Name) ? "en-US" : culture.Name;
        }
    }
}
