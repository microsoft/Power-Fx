// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
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
            return Regex.IsMatch(s, @"^[0-9]{4,4}-[0-1][0-9]-[0-3][0-9](T[0-2][0-9]:[0-5][0-9]:[0-5][0-9](\.[0-9]{3,3})?Z?)?$");
        }

        public static FormulaValue Index_UO(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (NumberValue)args[1];

            var element = arg0.Impl;

            var len = element.GetArrayLength();
            var index1 = (int)arg1.Value;
            var index0 = index1 - 1; // 1-based index

            // Error pipeline already caught cases of too low. 
            if (index0 < len)
            {
                var result = element[index0];

                // Map null to blank
                if (result == null || result.Type == FormulaType.Blank)
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

        public static FormulaValue Value_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.Number)
            {
                var number = impl.GetDouble();
                if (IsInvalidDouble(number))
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }

                return new NumberValue(irContext, number);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.Value_UO.Name, DType.Number.GetKindString(), impl);
        }

        public static FormulaValue Text_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var str = impl.GetString();
                return new StringValue(irContext, str);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.Text_UO.Name, DType.String.GetKindString(), impl);
        }

        public static FormulaValue Table_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var tableType = (TableType)irContext.ResultType;
            var resultType = tableType.ToRecord();
            var itemType = resultType.GetFieldType(BuiltinFunction.ColumnName_ValueStr);

            var resultRows = new List<DValue<RecordValue>>();

            var len = args[0].Impl.GetArrayLength();

            for (var i = 0; i < len; i++)
            {
                var element = args[0].Impl[i];

                var namedValue = new NamedValue(BuiltinFunction.ColumnName_ValueStr, new UntypedObjectValue(IRContext.NotInSource(itemType), element));
                var record = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue });
                resultRows.Add(DValue<RecordValue>.Of(record));
            }

            return new InMemoryTableValue(irContext, resultRows);
        }

        private static FormulaValue UntypedObjectArrayChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is UntypedObjectValue cov)
            {
                if (!(cov.Impl.Type is ExternalType et && et.Kind == ExternalTypeKind.Array))
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
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.Boolean)
            {
                var b = impl.GetBoolean();
                return new BooleanValue(irContext, b);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.Boolean_UO.Name, DType.Boolean.GetKindString(), impl);
        }

        public static FormulaValue CountRows_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type is ExternalType externalType && externalType.Kind == ExternalTypeKind.Array)
            {
                return new NumberValue(irContext, impl.GetArrayLength());
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.CountRows_UO.Name, DType.EmptyTable.GetKindString(), impl);
        }

        public static FormulaValue DateValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var s = impl.GetString();

                if (IsValidDateTimeUO(s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
                {
                    return new DateValue(irContext, res.Date);
                }

                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.DateValue_UO.Name, DType.String.GetKindString(), impl);
        }

        public static FormulaValue TimeValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var s = impl.GetString();
                if (TimeSpan.TryParseExact(s, @"hh\:mm\:ss\.FFF", CultureInfo.InvariantCulture, TimeSpanStyles.None, out TimeSpan res))
                {
                    return new TimeValue(irContext, res);
                }

                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.TimeValue_UO.Name, DType.String.GetKindString(), impl);
        }

        public static FormulaValue DateTimeValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var s = impl.GetString();

                if (IsValidDateTimeUO(s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
                {
                    return new DateTimeValue(irContext, res);
                }

                return CommonErrors.InvalidDateTimeParsingError(irContext);
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.DateTimeValue_UO.Name, DType.String.GetKindString(), impl);
        }

        public static FormulaValue Guid_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var str = new StringValue(IRContext.NotInSource(FormulaType.String), impl.GetString());
                return Guid(irContext, new StringValue[] { str });
            }

            return GetTypeMismatchError(irContext, BuiltinFunctionsCore.GUID_UO.Name, DType.String.GetKindString(), impl);
        }

        private static ErrorValue GetTypeMismatchError(IRContext irContext, string functionName, string expectedType, IUntypedObject actualValue)
        {
            return new ErrorValue(irContext, new ExpressionError
            {
                Kind = ErrorKind.InvalidArgument,
                Span = irContext.SourceContext,
                Message = $"The untyped object argument to the '{functionName}' function has an incorrect type. Expected: {expectedType}, Actual: {actualValue.Type}."
            });
        }

        public static async ValueTask<FormulaValue> ForAll_UO(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (UntypedObjectValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var items = new List<DValue<UntypedObjectValue>>();

            var len = arg0.Impl.GetArrayLength();

            for (var i = 0; i < len; i++)
            {
                runner.CheckCancel();

                var element = arg0.Impl[i];
                var item = new UntypedObjectValue(IRContext.NotInSource(FormulaType.UntypedObject), element);

                items.Add(DValue<UntypedObjectValue>.Of(item));
            }

            var rowsAsync = LazyForAll(runner, context, items, arg1);

            var rows = await Task.WhenAll(rowsAsync);

            return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows, forceSingleColumn: false));
        }
    }
}
