// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Convert value to target format.
    /// </summary>
    public static class TypeCoercionProvider
    {
        internal static FormattingInfo CreateFormattingInfo() => new FormattingInfo()
        {
            // $$$ can't use current culture
            CultureInfo = CultureInfo.CurrentCulture,
            CancellationToken = CancellationToken.None,
            TimeZoneInfo = TimeZoneInfo.Local
        };

        internal static FormattingInfo CreateFormattingInfo(RuntimeConfig runtimeConfig, CancellationToken cancellationToken) => new FormattingInfo()
        {
            CultureInfo = runtimeConfig.GetService<CultureInfo>(),
            CancellationToken = cancellationToken,
            TimeZoneInfo = runtimeConfig.GetService<TimeZoneInfo>()
        };

        /// <summary>
        /// Can convert value to source format to target or not.
        /// </summary>
        /// <param name="source">Source type format.</param>
        /// <param name="target">Target type format.</param>
        /// <returns>True/False based on whether function can convert from source type to target type.</returns> 
        public static bool CanPotentiallyCoerceTo(this FormulaType source, FormulaType target)
        {
            if (source is RecordType recordType && target is RecordType targetRecordType)
            {
                return recordType.CanPotentiallyCoerceToRecordType(targetRecordType);
            }
            else if (source is TableType tableType && target is TableType targetTableType)
            {
                return tableType.CanPotentiallyCoerceToTableType(targetTableType);
            }

            return CanPotentiallyCoerceToTargetType(source, target);
        }

        /// <summary>
        /// Try to convert value to target format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type format.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to target type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, FormulaType targetType, out FormulaValue result)
        {
            bool canCoerce = false;
            if (value is RecordValue recordValue && targetType is RecordType recordType)
            {
                canCoerce = recordValue.TryCoerceToRecord(recordType, out RecordValue recordResult);
                result = recordResult;
            }
            else if (value is TableValue tableValue && targetType is TableType tableType)
            {
                canCoerce = tableValue.TryCoerceToTable(tableType, out TableValue tableResult);
                result = tableResult;
            }
            else
            {
                canCoerce = TryCoerceToTargetType(value, targetType, out result);
            }

            return canCoerce;
        }
        
        /// <summary>
        /// Can convert value to String format or not.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        public static bool CanCoerceToStringValue(this FormulaValue value)
        {
            return StringValue.AllowedListConvertToString.Contains(value.Type);
        }

        /// <summary>
        /// Try to convert value to Boolean format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Boolean type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, out BooleanValue result)
        {
            return TryGetBoolean(IRContext.NotInSource(FormulaType.Boolean), value, out result);
        }

        /// <summary>
        /// Try to convert value to String format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, out StringValue result)
        {
            return TryCoerceTo(value, CreateFormattingInfo(), out result);
        }

        /// <summary>
        /// Try to convert value to String format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="runtimeConfig">Runtime Config.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, RuntimeConfig runtimeConfig, out StringValue result)
        {
            return TryCoerceTo(value, runtimeConfig, CancellationToken.None, out result);
        }

        /// <summary>
        /// Try to convert value to String format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="runtimeConfig">Runtime Config.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, RuntimeConfig runtimeConfig, CancellationToken cancellationToken, out StringValue result)
        {
            return TryCoerceTo(value, CreateFormattingInfo(runtimeConfig, cancellationToken), out result);
        }

        /// <summary>
        /// Try to convert value to String format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        internal static bool TryCoerceTo(this FormulaValue value, FormattingInfo formatInfo, out StringValue result)
        {
            return TryText(formatInfo, IRContext.NotInSource(FormulaType.String), value, false, false, null, -1, -1, out result);
        }

        /// <summary>
        /// Try to convert value to Number format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Number type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, out NumberValue result)
        {
            return TryCoerceTo(value, CreateFormattingInfo(), out result);
        }

        /// <summary>
        /// Try to convert value to Number format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Number type.</returns> 
        internal static bool TryCoerceTo(this FormulaValue value, FormattingInfo formatInfo, out NumberValue result)
        {
            return TryFloat(formatInfo, IRContext.NotInSource(FormulaType.Number), value, out result);
        }

        /// <summary>
        /// Try to convert value to Number format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Number type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, out DecimalValue result)
        {
            return TryCoerceTo(value, CreateFormattingInfo(), out result);
        }

        /// <summary>
        /// Try to convert value to Number format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Number type.</returns> 
        internal static bool TryCoerceTo(this FormulaValue value, FormattingInfo formatInfo, out DecimalValue result)
        {
            return TryDecimal(formatInfo, IRContext.NotInSource(FormulaType.Decimal), value, out result);
        }

        /// <summary>
        /// Try to convert value to DateTime format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to DateTime type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, out DateTimeValue result)
        {
            return TryCoerceTo(value, CreateFormattingInfo(), out result);
        }

        /// <summary>
        /// Try to convert value to DateTime format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to DateTime type.</returns> 
        internal static bool TryCoerceTo(this FormulaValue value, FormattingInfo formatInfo, out DateTimeValue result)
        {
            return TryGetDateTime(formatInfo, IRContext.NotInSource(FormulaType.DateTime), value, out result);
        }

        /// <summary>
        /// Try to convert value to Record format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Record type.</returns> 
        public static bool TryCoerceToRecord(this RecordValue value, RecordType targetType, out RecordValue result)
        {
            result = null;
            int n = value.Fields.Count();
            var recordResult = new NamedValue[n];

            for (int i = 0; i < n; i++)
            {
                var fieldName = value.Fields.ElementAt(i).Name;
                var fieldValue = value.GetField(fieldName);
                if (!targetType.TryGetFieldType(fieldName, out FormulaType fieldType))
                {
                    return false;
                }

                if (fieldType is RecordType recordType)
                {
                    if (!TryCoerceTo(fieldValue, fieldType, out FormulaValue fieldRecordResult))
                    {
                        return false;
                    }

                    recordResult[i] = new NamedValue(fieldName, fieldRecordResult);
                }
                else
                {
                    if (!TryCoerceToTargetType(fieldValue, fieldType, out FormulaValue fieldResult))
                    {
                        return false;
                    }

                    recordResult[i] = new NamedValue(fieldName, fieldResult);
                }
            }

            result = FormulaValue.NewRecordFromFields(recordResult);
            return true;
        }

        /// <summary>
        /// Try to convert value to Table format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Table type.</returns> 
        public static bool TryCoerceToTable(this TableValue value, TableType targetType, out TableValue result)
        {
            result = null;
            int n = value.Rows.Count();
            var records = new RecordValue[n];

            for (int i = 0; i < n; i++)
            {
                if (!value.Rows.ElementAt(i).Value.TryCoerceToRecord(targetType.ToRecord(), out RecordValue recordResult))
                {
                    return false;
                }

                records[i] = recordResult;
            }
            
            result = FormulaValue.NewTable(targetType.ToRecord(), records.ToArray());
            return true;
        }

        /// <summary>
        /// Can convert value to source format to record target or not.
        /// </summary>
        /// <param name="source">Source type format.</param>
        /// <param name="target">Target type format.</param>
        /// <returns>True/False based on whether function can convert from source type to record target type.</returns> 
        public static bool CanPotentiallyCoerceToRecordType(this RecordType source, RecordType target)
        {
            foreach (var field in source.GetFieldTypes())
            {
                if (!target.TryGetFieldType(field.Name, out FormulaType targetFieldType))
                {
                    return false;
                }

                if (!CanPotentiallyCoerceTo(field.Type, targetFieldType))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Can convert value to source format to table target or not.
        /// </summary>
        /// <param name="source">Source type format.</param>
        /// <param name="target">Target type format.</param>
        /// <returns>True/False based on whether function can convert from source type to record target type.</returns> 
        public static bool CanPotentiallyCoerceToTableType(this TableType source, TableType target)
        {
            return CanPotentiallyCoerceToRecordType(source.ToRecord(), target.ToRecord());
        }

        /// <summary>
        /// Can convert value to source format to target or not that not include record or table.
        /// </summary>
        /// <param name="source">Source type format.</param>
        /// <param name="target">Target type format.</param>
        /// <returns>True/False based on whether function can convert from source type to target type.</returns> 
        public static bool CanPotentiallyCoerceToTargetType(FormulaType source, FormulaType target)
        {
            if (source == FormulaType.Boolean)
            {
                return BooleanValue.AllowedListConvertToBoolean.Contains(target);
            }
            else if (source == FormulaType.String)
            {
                return StringValue.AllowedListConvertToString.Contains(target);
            }
            else if (source == FormulaType.Number)
            {
                return NumberValue.AllowedListConvertToNumber.Contains(target);
            }
            else if (source == FormulaType.DateTime)
            {
                return DateTimeValue.AllowedListConvertToDateTime.Contains(target);
            }

            return false;
        }

        /// <summary>
        /// Try to convert value to target type that not include record or table.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type format.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to target type.</returns> 
        private static bool TryCoerceToTargetType(FormulaValue value, FormulaType targetType, out FormulaValue result)
        {
            result = null;
            bool canCoerce = false;

            if (targetType == FormulaType.Boolean)
            {
                canCoerce = value.TryCoerceTo(out BooleanValue boolResult);
                result = boolResult;
            }
            else if (targetType == FormulaType.String)
            {
                canCoerce = value.TryCoerceTo(out StringValue stringResult);
                result = stringResult;
            }
            else if (targetType == FormulaType.Number)
            {
                canCoerce = value.TryCoerceTo(out NumberValue numResult);
                result = numResult;
            }
            else if (targetType == FormulaType.DateTime)
            {
                canCoerce = value.TryCoerceTo(out DateTimeValue dateTimeResult);
                result = dateTimeResult;
            }
            else if (targetType == FormulaType.Decimal)
            {
                canCoerce = value.TryCoerceTo(out DecimalValue decimalResult);
                result = decimalResult;
            }

            return canCoerce;
        }
    }
}
