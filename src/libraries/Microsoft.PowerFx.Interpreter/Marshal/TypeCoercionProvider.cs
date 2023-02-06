// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Interpreter.Marshal
{
    /// <summary>
    /// Convert value to target format.
    /// </summary>
    internal static class TypeCoercionProvider
    {
        /// <summary>
        /// Try to convert value to target format.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to target type.</returns> 
        public static bool TryCoerceTo(FormulaValue value, FormulaType targetType, out FormulaValue result)
        {
            var formatInfo = new FormattingInfo()
            {
                CultureInfo = CultureInfo.CurrentCulture,
                CancellationToken = CancellationToken.None,
                TimeZoneInfo = TimeZoneInfo.Local
            };

            return TryCoerceTo(formatInfo, value, targetType, out result);
        }

        /// <summary>
        /// Try to convert value to target format.
        /// </summary>
        /// <param name="formatInfo">Formatting info.</param>
        /// <param name="value">Input value.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="result">Result value.</param>
        /// <returns>True/False based on whether function can convert from original type to target type.</returns> 
        public static bool TryCoerceTo(FormattingInfo formatInfo, FormulaValue value, FormulaType targetType, out FormulaValue result)
        {
            if (value == null)
            {
                result = FormulaValue.NewBlank(targetType);
                return true;
            }

            result = null;
            IRContext irContext = IRContext.NotInSource(targetType);

            if (targetType == FormulaType.Boolean)
            {
                result = GetBoolean(irContext, value);
            }
            else if (targetType == FormulaType.String)
            {
                result = Text(formatInfo, irContext, new FormulaValue[] { value });
            }
            else if (targetType == FormulaType.Number)
            {
                result = Value(formatInfo, irContext, new FormulaValue[] { value });
            }
            else if (targetType == FormulaType.DateTime)
            {
                result = GetDateTime(formatInfo, irContext, value);
            }
            else
            {
                result = CommonErrors.NotYetImplementedError(irContext, $"Do not support {targetType.GetType().Name} Type.");
                return false;
            }

            if (result is ErrorValue errorValue)
            {
                return false;
            }

            return result != null;
        }
    }
}
