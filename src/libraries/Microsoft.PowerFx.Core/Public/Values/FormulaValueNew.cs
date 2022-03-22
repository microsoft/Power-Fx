// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
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

        public static FormulaValue New(double? number)
        {
            if (number.HasValue)
            {
                return New(number.Value);
            }

            return new BlankValue(IRContext.NotInSource(FormulaType.Number));
        }

        public static NumberValue New(decimal number)
        {
            // $$$ Is this safe? or loss in precision?
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), (double)number);
        }

        public static NumberValue New(long number)
        {
            // $$$ Is this safe? or loss in precision?
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), (double)number);
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

            if (value.Kind == DateTimeKind.Utc)
            {
                throw new ArgumentException("Invalid DateValue, the provided DateTime must be local");
            }

            return new DateValue(IRContext.NotInSource(FormulaType.Date), value);
        }

        public static DateTimeValue New(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
            {
                throw new ArgumentException("Invalid DateTimeValue, the provided DateTime must be local");
            }

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
            return new ErrorValue(IRContext.NotInSource(FormulaType.Blank), error);
        }

        public static UntypedObjectValue New(IUntypedObject untypedObject)
        {
            return new UntypedObjectValue(
                IRContext.NotInSource(new UntypedObjectType()),
                untypedObject);
        }
    }
}
