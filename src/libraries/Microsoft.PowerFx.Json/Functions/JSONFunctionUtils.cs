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
        private static readonly FormulaValueJsonSerializerSettings DefaultFormulaValueSerializerSettings = new FormulaValueJsonSerializerSettings { AllowUnknownRecordFields = false, ResultTimeZone = TimeZoneInfo.Local };

        public static FormulaValue ConvertUnTypedObjectToFormulaValue(IRContext irContext, FormulaValue input, StringValue typeString)
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

            return FormulaValueJSON.FromJson(jsElement, DefaultFormulaValueSerializerSettings, FormulaType.Build(dtype));
        }

        public static FormulaValue ConvertJSONStringToFormulaValue(IRContext irContext, FormulaValue input, StringValue typeString)
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

            return FormulaValueJSON.FromJson(json, DefaultFormulaValueSerializerSettings, FormulaType.Build(dtype));
        }
    }
}
