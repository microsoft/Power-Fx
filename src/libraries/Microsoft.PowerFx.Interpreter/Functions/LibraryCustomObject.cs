using System;
using System.Collections.Generic;
using System.Text;
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
            private readonly JsonElement _element;

            public JsonCustomObject(JsonElement element)
            {
                _element = element;
            }

            public ICustomObject this[int index] => new JsonCustomObject(_element[index]);

            public bool IsArray => _element.ValueKind == JsonValueKind.Array;

            public bool IsNull => _element.ValueKind == JsonValueKind.Null;

            public bool IsObject => _element.ValueKind == JsonValueKind.Object;

            public bool IsString => _element.ValueKind == JsonValueKind.String;

            public bool IsNumber => _element.ValueKind == JsonValueKind.Number;

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

            public object ToObject()
            {
                return _element.GetRawText();
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

            if (index < len)
            {
                var result = element[index - 1]; // 1-based index

                // Map null to blank
                if (result.IsNull)
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

        public static FormulaValue GetField(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (CustomObjectValue)args[0];
            var arg1 = (StringValue)args[1];

            var element = arg0.Impl;

            if (element.TryGetProperty(arg1.Value, out var result))
            {
                // Map null to blank
                if (result.IsNull)
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
            return new NumberValue(irContext, args[0].Impl.GetDouble());
        }

        public static FormulaValue Text_CO(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, CustomObjectValue[] args)
        {
            return new StringValue(irContext, args[0].Impl.GetString());
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

        private static FormulaValue CustomObjectNumberChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is CustomObjectValue cov)
            {
                if (cov.Impl.IsNumber)
                {
                    var number = cov.Impl.GetDouble();
                    if (IsInvalidDouble(number))
                    {
                        return CommonErrors.ArgumentOutOfRange(irContext);
                    }
                }
                else
                {
                    return CommonErrors.RuntimeTypeMismatch(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue CustomObjectStringChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is CustomObjectValue cov)
            {
                if (!cov.Impl.IsString)
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
                if (!cov.Impl.IsArray)
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

        private static FormulaValue CustomObjectObjectChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is CustomObjectValue cov)
            {
                if (!cov.Impl.IsObject)
                {
                    return new ErrorValue(irContext, new ExpressionError()
                    {
                        Message = "The CustomObject does not represent an object",
                        Span = irContext.SourceContext,
                        Kind = ErrorKind.InvalidFunctionUsage
                    });
                }
            }

            return arg;
        }
    }
}
