// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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
        /// Can convert value to String format or not.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        public static bool CanCoerceToStringValue(this FormulaValue value)
        {
            return AllowedListConvertToString.Contains(value.Type);
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
            return TryText(formatInfo, IRContext.NotInSource(FormulaType.String), value, null, out result);
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
    }
}
