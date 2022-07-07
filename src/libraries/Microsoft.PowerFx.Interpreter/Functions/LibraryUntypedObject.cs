// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
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

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue Text_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var str = impl.GetString();
                return new StringValue(irContext, str);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
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
                        Kind = ErrorKind.InvalidFunctionUsage
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

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue CountRows_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type is ExternalType externalType && externalType.Kind == ExternalTypeKind.Array)
            {
                return new NumberValue(irContext, impl.GetArrayLength());
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue DateValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var s = impl.GetString();
                if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
                {
                    return new DateValue(irContext, res.Date);
                }

                return CommonErrors.InvalidDateTimeError(irContext);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue TimeValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var s = impl.GetString();
                if (TimeSpan.TryParseExact(s, "HH:mm:ss.FFF", CultureInfo.InvariantCulture, out TimeSpan res))
                {
                    return new TimeValue(irContext, res);
                }

                return CommonErrors.InvalidDateTimeError(irContext);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue DateTimeValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var s = impl.GetString();

                // Year-month-date, the literal T, Hours-minutes-seconds
                // F is 10ths of a second if non-zero, K is time zone information
                var iso8601Format = "yyyy-MM-dd'T'HH:mm:ss.FFFK";

                if (DateTime.TryParseExact(s, iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
                {
                    return new DateTimeValue(irContext, res);
                }

                return CommonErrors.InvalidDateTimeError(irContext);
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue ColorValue_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var str = new StringValue(IRContext.NotInSource(FormulaType.String), impl.GetString());
                return ColorValue(irContext, new StringValue[] { str });
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        public static FormulaValue Guid_UO(IRContext irContext, UntypedObjectValue[] args)
        {
            var impl = args[0].Impl;

            if (impl.Type == FormulaType.String)
            {
                var str = new StringValue(irContext, impl.GetString());
                return Guid(irContext, new StringValue[] { str });
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }
    }
}
