// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
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
        public static FormulaValue ParameterData(RecalcEngineResolver.ParameterData data, SymbolContext context, IRContext irContext)
        {
            var paramName = data.ParameterName;
            var value = context.Globals.GetField(irContext, paramName);
            return value;
        }

        public static FormulaValue RecalcFormulaInfo(RecalcFormulaInfo fi)
        {
            return fi._value;
        }

        public static FormulaValue OptionSet(OptionSet optionSet, IRContext irContext)
        {
            var recordValue = FormulaValue.RecordFromFields(
                optionSet.Options
                    .Select(option => new NamedValue(
                        option.Key, 
                        optionSet.GetValue(option.Key))));
            return recordValue;
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
