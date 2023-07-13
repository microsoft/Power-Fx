// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Utils;
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

        public static bool TryCoerceTo(this FormulaValue value, FormulaType targetType, out FormulaValue result)
        {
            return value.TryCoerceTo(targetType, FormattingInfoHelper.CreateFormattingInfo(), out result, CancellationToken.None);
        }

        /// <summary>
        /// Try to convert value to target format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type format.</param>
        /// <param name="formattingInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True/False based on whether function can convert from original type to target type.</returns> 
        internal static bool TryCoerceTo(this FormulaValue value, FormulaType targetType, FormattingInfo formattingInfo, out FormulaValue result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool canCoerce = false;
            if (value is RecordValue recordValue && targetType is RecordType recordType)
            {
                canCoerce = recordValue.TryCoerceToRecord(recordType, formattingInfo, out RecordValue recordResult, cancellationToken);
                result = recordResult;
            }
            else if (value is TableValue tableValue && targetType is TableType tableType)
            {
                canCoerce = tableValue.TryCoerceToTable(tableType, formattingInfo, out TableValue tableResult, cancellationToken);
                result = tableResult;
            }
            else
            {
                canCoerce = TryCoerceToTargetType(value, targetType, formattingInfo, out result, cancellationToken);
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
            return TryCoerceTo(value, FormattingInfoHelper.CreateFormattingInfo(), out result, CancellationToken.None);
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
            cancellationToken.ThrowIfCancellationRequested();
            return TryCoerceTo(value, FormattingInfoHelper.FromServiceProvider(runtimeConfig.ServiceProvider), out result, cancellationToken);
        }

        /// <summary>
        /// Try to convert value to String format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        internal static bool TryCoerceTo(this FormulaValue value, FormattingInfo formatInfo, out StringValue result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var textFormatArgs = new TextFormatArgs
            {
                FormatCultureName = null,
                FormatArg = null,
                HasDateTimeFmt = false,
                HasNumericFmt = false
            };

            return TryText(formatInfo, IRContext.NotInSource(FormulaType.String), value, textFormatArgs, out result, cancellationToken);
        }

        /// <summary>
        /// Try to convert value to Number format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Number type.</returns> 
        public static bool TryCoerceTo(this FormulaValue value, out NumberValue result)
        {
            return TryCoerceTo(value, FormattingInfoHelper.CreateFormattingInfo(), out result);
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
            return TryCoerceTo(value, FormattingInfoHelper.CreateFormattingInfo(), out result);
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
            return TryCoerceTo(value, FormattingInfoHelper.CreateFormattingInfo(), out result);
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

        public static bool TryCoerceToRecord(this RecordValue value, RecordType targetType, out RecordValue result)
        {
            return value.TryCoerceToRecord(targetType, FormattingInfoHelper.CreateFormattingInfo(), out result, CancellationToken.None);
        }

        /// <summary>
        /// Try to convert value to Record format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="formattingInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True/False based on whether function can convert from original type to Record type.</returns> 
        internal static bool TryCoerceToRecord(this RecordValue value, RecordType targetType, FormattingInfo formattingInfo, out RecordValue result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            result = null;
            var recordResult = new List<NamedValue>();

            foreach (var targetField in targetType.GetFieldTypes())
            {
                var fieldName = targetField.Name;
                var fieldType = targetField.Type;
                var fieldSourceValue = value.GetField(fieldName);

                if (!value.Type.TryGetFieldType(fieldName, out FormulaType sourceFieldType))
                {
                    recordResult.Add(new NamedValue(fieldName, FormulaValue.NewBlank(fieldType)));
                    continue;
                }

                if (fieldType is RecordType recordType)
                {
                    if (!TryCoerceTo(fieldSourceValue, fieldType, formattingInfo, out FormulaValue fieldRecordResult, cancellationToken))
                    {
                        return false;
                    }

                    recordResult.Add(new NamedValue(fieldName, fieldRecordResult));
                }
                else
                {
                    if (!TryCoerceToTargetType(fieldSourceValue, fieldType, formattingInfo, out FormulaValue fieldResult, cancellationToken))
                    {
                        return false;
                    }

                    recordResult.Add(new NamedValue(fieldName, fieldResult));
                }
            }

            result = FormulaValue.NewRecordFromFields(recordResult);
            return true;
        }

        public static bool TryCoerceToTable(this TableValue value, TableType targetType, out TableValue result)
        {
            return value.TryCoerceToTable(targetType, FormattingInfoHelper.CreateFormattingInfo(), out result, CancellationToken.None);
        }

        /// <summary>
        /// Try to convert value to Table format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="formattingInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True/False based on whether function can convert from original type to Table type.</returns> 
        internal static bool TryCoerceToTable(this TableValue value, TableType targetType, FormattingInfo formattingInfo, out TableValue result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            result = null;
            int n = value.Rows.Count();
            var records = new RecordValue[n];

            for (int i = 0; i < n; i++)
            {
                if (!value.Rows.ElementAt(i).Value.TryCoerceToRecord(targetType.ToRecord(), formattingInfo, out RecordValue recordResult, cancellationToken))
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
                if (target.TryGetFieldType(field.Name, out FormulaType targetFieldType) && !CanPotentiallyCoerceTo(field.Type, targetFieldType))
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
            if (source == target)
            {
                return true;
            }

            if (target == FormulaType.Boolean)
            {
                return BooleanValue.AllowedListConvertToBoolean.Contains(source);
            }
            else if (target == FormulaType.String)
            {
                return StringValue.AllowedListConvertToString.Contains(source);
            }
            else if (target == FormulaType.Number)
            {
                return NumberValue.AllowedListConvertToNumber.Contains(source);
            }
            else if (target == FormulaType.Decimal)
            {
                return DecimalValue.AllowedListConvertToDecimal.Contains(source);
            }
            else if (target == FormulaType.DateTime)
            {
                return DateTimeValue.AllowedListConvertToDateTime.Contains(source);
            }

            return false;
        }

        /// <summary>
        /// Try to convert value to target type that not include record or table.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type format.</param>
        /// <param name="formattingInfo">Formatting Info.</param>
        /// <param name="result">Result value.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True/False based on whether function can convert from original type to target type.</returns> 
        private static bool TryCoerceToTargetType(FormulaValue value, FormulaType targetType, FormattingInfo formattingInfo, out FormulaValue result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            result = null;
            bool canCoerce = false;

            if (targetType == FormulaType.Boolean)
            {
                canCoerce = value.TryCoerceTo(out BooleanValue boolResult);
                result = boolResult;
            }
            else if (targetType == FormulaType.String)
            {
                canCoerce = value.TryCoerceTo(formattingInfo, out StringValue stringResult, cancellationToken);
                result = stringResult;
            }
            else if (targetType == FormulaType.Number)
            {
                canCoerce = value.TryCoerceTo(formattingInfo, out NumberValue numResult);
                result = numResult;
            }
            else if (targetType == FormulaType.DateTime)
            {
                canCoerce = value.TryCoerceTo(formattingInfo, out DateTimeValue dateTimeResult);
                result = dateTimeResult;
            }
            else if (targetType == FormulaType.Decimal)
            {
                canCoerce = value.TryCoerceTo(formattingInfo, out DecimalValue decimalResult);
                result = decimalResult;
            }

            return canCoerce;
        }
    }
}
