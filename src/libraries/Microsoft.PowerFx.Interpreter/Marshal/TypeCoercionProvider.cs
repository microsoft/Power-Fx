﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Functions.Library;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Interpreter.Marshal
{
    /// <summary>
    /// Convert value to target format.
    /// </summary>
    internal static class TypeCoercionProvider
    {
        private static FormattingInfo CreateFormattingInfo()
        {
            return new FormattingInfo()
            {
                CultureInfo = CultureInfo.CurrentCulture,
                CancellationToken = CancellationToken.None,
                TimeZoneInfo = TimeZoneInfo.Local
            };
        }

        /// <summary>
        /// Try to convert value to Boolean format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Boolean type.</returns> 
        public static bool TryCoerceTo(FormulaValue value, out BooleanValue result)
        {
            return TryGetBoolean(IRContext.NotInSource(FormulaType.Boolean), value, out result);
        }

        /// <summary>
        /// Try to convert value to String format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        public static bool TryCoerceTo(FormulaValue value, out StringValue result)
        {
            return TryCoerceTo(CreateFormattingInfo(), value, out result);
        }

        /// <summary>
        /// Try to convert value to String format.
        /// </summary>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to String type.</returns> 
        public static bool TryCoerceTo(FormattingInfo formatInfo, FormulaValue value, out StringValue result)
        {
            result = Text(formatInfo, IRContext.NotInSource(FormulaType.String), value);
            return result != null;
        }

        /// <summary>
        /// Try to convert value to Number format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Number type.</returns> 
        public static bool TryCoerceTo(FormulaValue value, out NumberValue result)
        {
            return TryCoerceTo(CreateFormattingInfo(), value, out result);
        }

        /// <summary>
        /// Try to convert value to Number format.
        /// </summary>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to Number type.</returns> 
        public static bool TryCoerceTo(FormattingInfo formatInfo, FormulaValue value, out NumberValue result)
        {
            result = Value(formatInfo, IRContext.NotInSource(FormulaType.Number), value);
            return result != null;
        }

        /// <summary>
        /// Try to convert value to DateTime format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to DateTime type.</returns> 
        public static bool TryCoerceTo(FormulaValue value, out DateTimeValue result)
        {
            return TryCoerceTo(CreateFormattingInfo(), value, out result);
        }

        /// <summary>
        /// Try to convert value to DateTime format.
        /// </summary>
        /// <param name="formatInfo">Formatting Info.</param>
        /// <param name="value">Input value.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to DateTime type.</returns> 
        public static bool TryCoerceTo(FormattingInfo formatInfo, FormulaValue value, out DateTimeValue result)
        {
            return TryGetDateTime(formatInfo, IRContext.NotInSource(FormulaType.DateTime), value, out result);
        }
    }
}
