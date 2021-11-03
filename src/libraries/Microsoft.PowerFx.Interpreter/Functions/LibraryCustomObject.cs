using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public static FormulaValue ParseJson(IRContext irContext, StringValue[] args)
        {
            var json = args[0].Value;
            JsonElement result;

            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    // Clone must be used here because the original element will be disposed
                    result = document.RootElement.Clone();
                }

                // Map null to blank
                if (result.ValueKind == JsonValueKind.Null)
                {
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                }

                return new CustomObjectValue(irContext, result);
            }
            catch (JsonException ex)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"The Json could not be parsed: {ex.Message}",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidFunctionUsage
                });
            }
        }

        public static FormulaValue GetAt(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (CustomObjectValue)args[0];
            var arg1 = (NumberValue)args[1];

            var element = arg0.Element;

            if (element.ValueKind == JsonValueKind.Array)
            {
                var len = element.GetArrayLength();
                var index = (int)arg1.Value;

                if (index < len)
                {
                    var result = element[index];

                    // Map null to blank
                    if (result.ValueKind == JsonValueKind.Null)
                    {
                        return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                    }

                    return new CustomObjectValue(irContext, result);
                }
                else
                {
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                }
            }
            else
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = "Invalid GetAt: The CustomObject does not represent an array",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidFunctionUsage
                });
            }
        }

        public static FormulaValue GetField(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (CustomObjectValue)args[0];
            var arg1 = (StringValue)args[1];

            var element = arg0.Element;

            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(arg1.Value, out var result))
                {
                    // Map null to blank
                    if (result.ValueKind == JsonValueKind.Null)
                    {
                        return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                    }

                    return new CustomObjectValue(irContext, result);
                }
                else
                {
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                }
            }
            else
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = "Invalid GetField: The CustomObject does not represent an object",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidFunctionUsage
                });
            }
        }
    }
}
