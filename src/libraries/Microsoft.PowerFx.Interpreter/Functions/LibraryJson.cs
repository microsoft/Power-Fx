// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        internal class JsonUntypedObject : IUntypedObject
        {
            private readonly JsonElement _element;

            public JsonUntypedObject(JsonElement element)
            {
                _element = element;
            }

            public FormulaType Type
            {
                get
                {
                    switch (_element.ValueKind)
                    {
                        case JsonValueKind.Object:
                            return ExternalType.ObjectType;
                        case JsonValueKind.Array:
                            return ExternalType.ArrayType;
                        case JsonValueKind.String:
                            return FormulaType.String;
                        case JsonValueKind.Number:
                            return FormulaType.Number;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            return FormulaType.Boolean;
                    }

                    return FormulaType.Blank;
                }
            }

            public IUntypedObject this[int index] => new JsonUntypedObject(_element[index]);

            public int GetArrayLength()
            {
                return _element.GetArrayLength();
            }

            public double GetDouble()
            {
                return _element.GetDouble();
            }

            public string GetString()
            {
                return _element.GetString();
            }

            public bool GetBoolean()
            {
                return _element.GetBoolean();
            }

            public bool TryGetProperty(string value, out IUntypedObject result)
            {
                var res = _element.TryGetProperty(value, out var je);
                result = new JsonUntypedObject(je);
                return res;
            }
        }

        public static FormulaValue ParseJSON(IRContext irContext, StringValue[] args)
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

                return new UntypedObjectValue(irContext, new JsonUntypedObject(result));
            }
            catch (JsonException ex)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"The Json could not be parsed: {ex.Message}",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }
        }
    }
}
