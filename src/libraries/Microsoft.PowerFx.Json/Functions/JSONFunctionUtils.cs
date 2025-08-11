// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Utils
{
    internal class JSONFunctionUtils
    {
        public static FormulaValue ConvertUntypedObjectToFormulaValue(IRContext irContext, FormulaValue input, StringValue typeString, TimeZoneInfo timeZoneInfo)
        {
            if (input is BlankValue || input is ErrorValue)
            {
                return input;
            }

            if (!DType.TryParse(typeString.Value, out DType dtype))
            {
                // This should never happen, as the typeString will be created by the IR
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Internal error: Unable to parse type argument",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Internal
                });
            }

            var untypedObjectValue = (UntypedObjectValue)input;
            var uo = untypedObjectValue.Impl;
            var jsElement = ((JsonUntypedObject)uo)._element;

            var settings = new FormulaValueJsonSerializerSettings { AllowUnknownRecordFields = true, ResultTimeZone = timeZoneInfo };

            return FormulaValueJSON.FromJson(jsElement, settings, FormulaType.Build(dtype));
        }

        public static FormulaValue ConvertJSONStringToFormulaValue(IRContext irContext, FormulaValue input, StringValue typeString, TimeZoneInfo timeZoneInfo)
        {
            if (input is BlankValue || input is ErrorValue)
            {
                return input;
            }
            
            if (input is not StringValue)
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = "Runtime type mismatch",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.InvalidArgument
                });
            }

            if (!DType.TryParse(typeString.Value, out DType dtype))
            {
                // This should never happen, as the typeString will be created by the IR
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"Internal error: Unable to parse type argument",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Internal
                });
            }

            var json = ((StringValue)input).Value;
            var settings = new FormulaValueJsonSerializerSettings { AllowUnknownRecordFields = true, ResultTimeZone = timeZoneInfo };

            return FormulaValueJSON.FromJson(json, settings, FormulaType.Build(dtype));
        }
    }
}
