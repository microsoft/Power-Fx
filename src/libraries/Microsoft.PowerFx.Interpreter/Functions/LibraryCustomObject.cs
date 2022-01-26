﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        internal class JsonCustomObject : ICustomObject
        {
            private static readonly FormulaType _objectType = new ExternalType(ExternalTypeKind.Object);
            private static readonly FormulaType _arrayType = new ExternalType(ExternalTypeKind.Array);

            private readonly JsonElement _element;

            public JsonCustomObject(JsonElement element)
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
                            return _objectType;
                        case JsonValueKind.Array:
                            return _arrayType;
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

            public ICustomObject this[int index] => new JsonCustomObject(_element[index]);

            public bool IsArray => _element.ValueKind == JsonValueKind.Array;

            public bool IsNull => _element.ValueKind == JsonValueKind.Null;

            public bool IsObject => _element.ValueKind == JsonValueKind.Object;

            public bool IsString => _element.ValueKind == JsonValueKind.String;

            public bool IsNumber => _element.ValueKind == JsonValueKind.Number;

            public bool IsBoolean => _element.ValueKind == JsonValueKind.True || _element.ValueKind == JsonValueKind.False;

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

            public object ToObject()
            {
                return _element;
            }

            public bool TryGetProperty(string value, out ICustomObject result)
            {
                var res = _element.TryGetProperty(value, out var je);
                result = new JsonCustomObject(je);
                return res;
            }
        }

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

                return new CustomObjectValue(irContext, new JsonCustomObject(result));
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

        public static FormulaValue Index_CO(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (CustomObjectValue)args[0];
            var arg1 = (NumberValue)args[1];

            var element = arg0.Impl;

            var len = element.GetArrayLength();
            var index = (int)arg1.Value;

            if (index <= len)
            {
                var result = element[index - 1]; // 1-based index

                // Map null to blank
                if (result.Type == FormulaType.Blank)
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

        public static FormulaValue Value_CO(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, CustomObjectValue[] args)
        {
            var impl = args[0].Impl;
            double number;

            if (impl.Type == FormulaType.String)
            {
                if (!double.TryParse(impl.GetString(), out number))
                {
                    return CommonErrors.InvalidNumberFormatError(irContext);
                }
            }
            else if (impl.Type == FormulaType.Blank)
            {
                return new BlankValue(irContext);
            }
            else if (impl.Type == FormulaType.Boolean)
            {
                number = impl.GetBoolean() ? 1 : 0;
            }
            else
            {
                number = impl.GetDouble();
            }

            return new NumberValue(irContext, number);
        }

        public static FormulaValue Text_CO(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, CustomObjectValue[] args)
        {
            var impl = args[0].Impl;
            string str;

            if (impl.Type == FormulaType.String)
            {
                str = impl.GetString();
            }
            else if (impl.Type == FormulaType.Blank)
            {
                str = string.Empty;
            }
            else if (impl.Type == FormulaType.Boolean)
            {
                str = impl.GetBoolean() ? "true" : "false";
            }
            else
            {
                str = impl.GetDouble().ToString();
            }

            return new StringValue(irContext, str);
        }

        public static FormulaValue Table_CO(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, CustomObjectValue[] args)
        {
            var tableType = (TableType)irContext.ResultType;
            var resultType = tableType.ToRecord();
            var itemType = resultType.GetFieldType(BuiltinFunction.ColumnName_ValueStr);

            var resultRows = new List<DValue<RecordValue>>();

            var len = args[0].Impl.GetArrayLength();

            for (var i = 0; i < len; i++)
            {
                var element = args[0].Impl[i];

                var namedValue = new NamedValue(BuiltinFunction.ColumnName_ValueStr, new CustomObjectValue(IRContext.NotInSource(itemType), element));
                var record = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue });
                resultRows.Add(DValue<RecordValue>.Of(record));
            }

            return new InMemoryTableValue(irContext, resultRows);
        }

        private static FormulaValue CustomObjectPrimitiveChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is CustomObjectValue cov)
            {
                if (cov.Impl.Type == FormulaType.Number)
                {
                    var number = cov.Impl.GetDouble();
                    if (IsInvalidDouble(number))
                    {
                        return CommonErrors.ArgumentOutOfRange(irContext);
                    }
                }
                else if (cov.Impl.Type is ExternalType)
                {
                    return CommonErrors.RuntimeTypeMismatch(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue CustomObjectArrayChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is CustomObjectValue cov)
            {
                if (!(cov.Impl.Type is ExternalType et && et.Kind == ExternalTypeKind.Array))
                {
                    return new ErrorValue(irContext, new ExpressionError()
                    {
                        Message = "The CustomObject does not represent an array",
                        Span = irContext.SourceContext,
                        Kind = ErrorKind.InvalidFunctionUsage
                    });
                }
            }

            return arg;
        }
    }
}
