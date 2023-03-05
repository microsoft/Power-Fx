// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.PowerFx.Core.IR;

namespace Microsoft.PowerFx.Types
{
    // Basc new operators. 
    public partial class FormulaValue
    {
        // Host utility creation methods, listed here for discoverability.
        // NOT FOR USE IN THE INTERPRETER! When creating new instances in
        // the interpreter, call the constructor directly and pass in the
        // IR context from the IR node.
        public static NumberValue New(double number)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), number);
        }

        public static DecimalValue New(decimal number)
        {
            return new DecimalValue(IRContext.NotInSource(FormulaType.Decimal), number);
        }

        public static FormulaValue New(double? number)
        {
            if (number.HasValue)
            {
                return New(number.Value);
            }

            return new BlankValue(IRContext.NotInSource(FormulaType.Number));
        }

        public static DecimalValue New(long number)
        {
            return new DecimalValue(IRContext.NotInSource(FormulaType.Decimal), (decimal)number);
        }

        public static NumberValue New(int number)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), number);
        }

        public static NumberValue New(float number)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), number);
        }

        public static GuidValue New(Guid guid)
        {
            return new GuidValue(IRContext.NotInSource(FormulaType.Guid), guid);
        }

        public static StringValue New(string value)
        {
            var ir = IRContext.NotInSource(FormulaType.String);
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new StringValue(ir, value);
        }

        public static BooleanValue New(bool value)
        {
            return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), value);
        }

        public static DateValue NewDateOnly(DateTime value)
        {
            if (value.TimeOfDay != TimeSpan.Zero)
            {
                throw new ArgumentException("Invalid DateValue, the provided DateTime contains a non-zero TimeOfDay");
            }

            return new DateValue(IRContext.NotInSource(FormulaType.Date), value);
        }

        public static DateTimeValue New(DateTime value)
        {
            return new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), value);
        }

        public static TimeValue New(TimeSpan value)
        {
            return new TimeValue(IRContext.NotInSource(FormulaType.Time), value);
        }

        public static BlankValue NewBlank(FormulaType type = null)
        {
            if (type == null)
            {
                type = FormulaType.Blank;
            }

            return new BlankValue(IRContext.NotInSource(type));
        }

        public static ErrorValue NewError(ExpressionError error)
        {
            return NewError(error, FormulaType.Blank);
        }

        public static ErrorValue NewError(ExpressionError error, FormulaType type)
        {
            return new ErrorValue(IRContext.NotInSource(type), error);
        }

        public static ErrorValue NewError(IEnumerable<ExpressionError> error, FormulaType type)
        {
            return new ErrorValue(IRContext.NotInSource(type), error.ToList());
        }

        public static UntypedObjectValue New(IUntypedObject untypedObject)
        {
            return new UntypedObjectValue(
                IRContext.NotInSource(new UntypedObjectType()),
                untypedObject);
        }

        public static ColorValue New(Color value)
        {
            return new ColorValue(IRContext.NotInSource(FormulaType.Color), value);
        }
    }
}
