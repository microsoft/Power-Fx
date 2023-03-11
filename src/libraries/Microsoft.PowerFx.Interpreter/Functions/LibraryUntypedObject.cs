// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        private static bool IsValidDateTimeUO(string s)
        {
            return Regex.IsMatch(s, @"^[0-9]{4,4}-[0-1][0-9]-[0-3][0-9](T[0-2][0-9]:[0-5][0-9]:[0-5][0-9](\.[0-9]{0,7})?Z?)?$");
        }

        public static FormulaValue Index_UO(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (NumberValue)args[1];

            if (arg0.Implementation is ISupportsArray array)
            {
                var len = array.Length;
                var index1 = (int)arg1.Value;
                var index0 = index1 - 1; // 1-based index

                // Error pipeline already caught cases of too low. 
                if (index0 < len)
                {
                    IUntypedObject result = array[index0];

                    // Map null to blank
                    if (result == null || result.IsBlank())
                    {
                        return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                    }

                    return new UntypedObjectValue(irContext, result);
                }
                else
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static FormulaValue First_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];

            if (arg0.Implementation is ISupportsArray array)
            {
                var len = array.Length;

                if (len == 0)
                {
                    return new BlankValue(irContext);
                }

                var result = array[0];

                return new UntypedObjectValue(irContext, result);
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static FormulaValue Last_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];

            if (arg0.Implementation is ISupportsArray array)
            {
                var len = array.Length;

                if (len == 0)
                {
                    return new BlankValue(irContext);
                }

                IUntypedObject result = array[len - 1];

                // Map null to blank
                if (result == null || result.IsBlank())
                {
                    return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                }

                return new UntypedObjectValue(irContext, result);
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static FormulaValue FirstN_UO(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (NumberValue)args[1];

            if (arg0.Implementation is ISupportsArray array)
            {
                var len = array.Length;

                var list = new List<IUntypedObject>();
                for (int i = 0; i < (int)arg1.Value && i < len; i++)
                {
                    list.Add(array[i]);
                }

                IUntypedObject result = new ArrayUntypedObject(list);

                return new UntypedObjectValue(irContext, result);
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static FormulaValue LastN_UO(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (NumberValue)args[1];

            if (arg0.Implementation is ISupportsArray array)
            {
                var len = array.Length;

                var list = new List<IUntypedObject>();
                var takeCount = (int)arg1.Value;
                for (int i = 0; i < takeCount; i++)
                {
                    var takeIndex = len - takeCount + i;
                    if (takeIndex >= 0)
                    {
                        list.Add(array[takeIndex]);
                    }
                }

                var result = new ArrayUntypedObject(list);

                return new UntypedObjectValue(irContext, result);
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static FormulaValue Value_UO(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var uo = args[0] as UntypedObjectValue;

            if (uo.Implementation is SupportsFxValue fxValue)
            {
                if (fxValue.Type == FormulaType.String)
                {
                    FormulaValue str = fxValue.Value;
                    if (args.Length > 1)
                    {
                        return Value(runner, context, irContext, new FormulaValue[] { str, args[1] });
                    }

                    return Value(runner, context, irContext, new FormulaValue[] { str });
                }
                else if (fxValue.Type == FormulaType.Number)
                {
                    NumberValue numberValue = (NumberValue)fxValue.Value;
                    if (IsInvalidDouble(numberValue.Value))
                    {
                        return CommonErrors.ArgumentOutOfRange(irContext);
                    }

                    return numberValue;
                }
                else if (fxValue.Type == FormulaType.Boolean)
                {
                    BooleanValue b = (BooleanValue)fxValue.Value;
                    return BooleanToNumber(irContext, new BooleanValue[] { b });
                }
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.Value_UO.Name, DType.Number.GetKindString(), uo.Implementation);
        }

        public static FormulaValue Text_UO(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, UntypedObjectValue[] args)
        {
            IUntypedObject uo = args[0].Implementation;

            if (uo is SupportsFxValue fxValue)
            {
                if (fxValue.Type == FormulaType.String)
                {
                    return fxValue.Value;
                }
                else if (fxValue.Type == FormulaType.Number)
                {
                    var n = fxValue.Value;
                    return Text(runner, context, irContext, new FormulaValue[] { n });
                }
                else if (fxValue.Type == FormulaType.Boolean)
                {
                    BooleanValue b = (BooleanValue)fxValue.Value;
                    return new StringValue(irContext, PowerFxBooleanToString(b.Value));
                }
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.Text_UO.Name, DType.String.GetKindString(), uo);
        }

        public static FormulaValue Table_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var tableType = (TableType)irContext.ResultType;
            var resultType = tableType.ToRecord();
            var itemType = resultType.GetFieldType(BuiltinFunction.ColumnName_ValueStr);

            var resultRows = new List<DValue<RecordValue>>();

            if (args[0].Implementation is ISupportsArray array)
            {
                var len = array.Length;

                for (var i = 0; i < len; i++)
                {
                    IUntypedObject element = array[i];

                    var namedValue = new NamedValue(BuiltinFunction.ColumnName_ValueStr, new UntypedObjectValue(IRContext.NotInSource(itemType), element));
                    var record = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue });
                    resultRows.Add(DValue<RecordValue>.Of(record));
                }

                return new InMemoryTableValue(irContext, resultRows);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        private static FormulaValue UntypedObjectArrayChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is UntypedObjectValue uov)
            {
                if (uov.Implementation.IsBlank())
                {
                    return new BlankValue(irContext);
                }

                if (!(uov.Implementation is ISupportsArray))
                {
                    return new ErrorValue(irContext, new ExpressionError()
                    {
                        Message = "The UntypedObject does not represent an array",
                        Span = irContext.SourceContext,
                        Kind = ErrorKind.InvalidArgument
                    });
                }
            }

            return arg;
        }

        public static FormulaValue Boolean_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            IUntypedObject uo = args[0].Implementation;

            if (uo is SupportsFxValue fxValue)
            {
                if (fxValue.Type == FormulaType.String)
                {
                    StringValue str = (StringValue)fxValue.Value;
                    return TextToBoolean(irContext, new StringValue[] { str });
                }
                else if (fxValue.Type == FormulaType.Number)
                {
                    NumberValue n = (NumberValue)fxValue.Value;
                    return NumberToBoolean(irContext, new NumberValue[] { n });
                }
                else if (fxValue.Type == FormulaType.Boolean)
                {
                    return fxValue.Value;
                }
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.Boolean_UO.Name, DType.Boolean.GetKindString(), uo);
        }

        public static FormulaValue CountRows_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            IUntypedObject uo = args[0].Implementation;

            if (uo is ISupportsArray array)
            {
                return new NumberValue(irContext, array.Length);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.CountRows_UO.Name, DType.EmptyTable.GetKindString(), uo);
        }

        public static FormulaValue DateValue_UO(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, UntypedObjectValue[] args)
        {
            IUntypedObject uo = args[0].Implementation;

            if (uo is SupportsFxValue fxValue)
            {
                if (fxValue.Type == FormulaType.String)
                {
                    StringValue s = (StringValue)fxValue.Value;

                    if (IsValidDateTimeUO(s.Value) && DateTime.TryParse(s.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime datetime))
                    {
                        var timeZoneInfo = runner.TimeZoneInfo;
                        datetime = MakeValidDateTime(runner, datetime, timeZoneInfo);
                        datetime = DateTimeValue.GetConvertedDateTimeValue(datetime, timeZoneInfo);

                        return new DateValue(irContext, datetime.Date);
                    }

                    return CommonErrors.InvalidDateTimeParsingError(irContext);
                }
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.DateValue_UO.Name, DType.String.GetKindString(), uo);
        }

        public static FormulaValue TimeValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var uo = args[0].Implementation;

            if (uo is SupportsFxValue fxValue && fxValue.Type == FormulaType.String)
            {
                StringValue s = (StringValue)fxValue.Value;

                if (TimeSpan.TryParseExact(s.Value, @"hh\:mm\:ss\.FFFFFFF", CultureInfo.InvariantCulture, TimeSpanStyles.None, out var res) ||
                    TimeSpan.TryParseExact(s.Value, @"hh\:mm\:ss", CultureInfo.InvariantCulture, TimeSpanStyles.None, out res))
                {
                    return new TimeValue(irContext, res);
                }

                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.TimeValue_UO.Name, DType.String.GetKindString(), uo);
        }

        public static FormulaValue DateTimeValue_UO(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, UntypedObjectValue[] args)
        {
            var uo = args[0].Implementation;

            if (uo is SupportsFxValue fxValue && fxValue.Type == FormulaType.String)
            {
                StringValue s = (StringValue)fxValue.Value;

                if (IsValidDateTimeUO(s.Value) && DateTime.TryParse(s.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime datetime))
                {
                    datetime = MakeValidDateTime(runner, datetime, runner.TimeZoneInfo);
                    datetime = DateTimeValue.GetConvertedDateTimeValue(datetime, runner.TimeZoneInfo);

                    return new DateTimeValue(irContext, datetime);
                }

                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.DateTimeValue_UO.Name, DType.String.GetKindString(), uo);
        }

        public static FormulaValue Guid_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var uo = args[0].Implementation;

            if (uo is SupportsFxValue fxValue && fxValue.Type == FormulaType.String)
            {
                StringValue str = (StringValue)fxValue.Value;
                return Guid(irContext, new StringValue[] { str });
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.GUID_UO.Name, DType.String.GetKindString(), uo);
        }

        private static ErrorValue GetTypeMismatchError(IRContext irContext, string functionName, string expectedType, IUntypedObject actualValue)
        {
            return new ErrorValue(irContext, new ExpressionError
            {
                Kind = ErrorKind.InvalidArgument,
                Span = irContext.SourceContext,
                Message = $"The untyped object argument to the '{functionName}' function has an incorrect type. Expected: {expectedType}, Actual: {actualValue.GetType().FullName}."
            });
        }

        public static async ValueTask<FormulaValue> ForAll_UO(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var items = new List<DValue<UntypedObjectValue>>();

            if (arg0.Implementation is ISupportsArray array)
            {
                var len = array.Length;

                for (var i = 0; i < len; i++)
                {
                    runner.CheckCancel();

                    var element = array[i];
                    var item = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), element);

                    items.Add(DValue<UntypedObjectValue>.Of(item));
                }

                var rowsAsync = LazyForAll(runner, context, items, arg1);

                var rows = new List<FormulaValue>();

                foreach (var row in rowsAsync)
                {
                    rows.Add(await row);
                }

                var errorRows = rows.OfType<ErrorValue>();
                if (errorRows.Any())
                {
                    return ErrorValue.Combine(irContext, errorRows);
                }

                return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray(), forceSingleColumn: false));
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue ColorValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var uo = args[0].Implementation;

            if (uo is SupportsFxValue fxValue && fxValue.Type == FormulaType.String)
            {
                StringValue str = (StringValue)fxValue.Value;

                if (Regex.IsMatch(str.Value, @"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$"))
                {
                    return ColorValue(irContext, new StringValue[] { str });
                }
                else
                {
                    return CommonErrors.InvalidColorFormatError(irContext);
                }
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.ColorValue_UO.Name, DType.String.GetKindString(), uo);
        }
    }
}
