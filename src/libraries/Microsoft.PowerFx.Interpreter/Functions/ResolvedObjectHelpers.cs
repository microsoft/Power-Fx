// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Functions
{
    // Core operations for turning ResolvedObjects into PowerFx values
    internal static class ResolvedObjectHelpers
    {
        public static FormulaValue RecalcFormulaInfo(RecalcFormulaInfo fi)
        {
            return fi._value;
        }

        // This allows option sets to be standalone identifiers, like:
        //   Choices(TimeUnit). 
        //
        // That it also lets OptionSets act like Records in ways we really don't want: 
        //   TimeUnit.Hours
        //   If(true, TimeUnit).Hours
        //   If(true, TextPosition, Align).Left
        public static FormulaValue OptionSet(IExternalOptionSet optionSet, IRContext irContext)
        {
            var options = new List<NamedValue>();
            foreach (var optionName in optionSet.OptionNames)
            {
                if (!optionSet.TryGetValue(optionName, out var osValue))
                {
                    // This is iterating the Options in the option set
                    // so we already know TryGetValue will succeed, making this unreachable.
                    return CommonErrors.UnreachableCodeError(irContext);
                }

                options.Add(new NamedValue(optionName, osValue));
            }

            // When evaluating an option set ResolvedObjectNode, we convert the options into a record
            // This allows the use of the FieldAccess operator to get specific option values.
            return FormulaValue.NewRecordFromFields(options);
        }

        public static FormulaValue ResolvedObjectError(ResolvedObjectNode node)
        {
            return new ErrorValue(node.IRContext, new ExpressionError()
            {
                Message = $"Unrecognized symbol {node?.Value?.GetType()?.Name}".Trim(),
                Span = node.IRContext.SourceContext,
                Kind = ErrorKind.Validation
            });
        }
    }
}
