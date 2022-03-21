// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represent a value in the formula expression. 
    /// </summary>
    [DebuggerDisplay("{ToObject().ToString()} ({Type})")]
    public abstract partial class FormulaValue
    {
        // We place the .New*() methods on FormulaValue for discoverability. 
        // If we're "marshalling" a T, we need a TypeMarshallerCache
        // Else, if we're "constructing" a Table/Record from existing FormulaValues, we don't need a marshaller.
        // We can use C# overloading to resolve. 

        // IR contextual information flows from Binding >> IR >> Values
        // In general the interpreter should trust that the binding had
        // the correct runtime types for all values.
        internal IRContext IRContext { get; }

        public FormulaType Type => IRContext.ResultType;

        internal FormulaValue(IRContext irContext)
        {
            IRContext = irContext;
        }

        #region Host Utility API

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
            return new StringValue(IRContext.NotInSource(FormulaType.String), value);
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

        // Marshal an arbitray object (scalar, record, table, etc) into a FormulaValue. 
        public static FormulaValue New(object obj, Type type, TypeMarshallerCache cache = null)
        {
            if (cache == null)
            {
                cache = new TypeMarshallerCache();
            }

            // Have New() wrapper for discoverability. 
            return cache.Marshal(obj, type);   
        }

        #endregion
              
        /// <summary>
        /// Converts to a .net object so host can easily consume the value. 
        /// Primitives (string, boolean, numbers, etc) convert directly to their .net type. 
        /// Records convert to a strongly typed or dynamic object so field notation works. 
        /// Tables convert to an enumerable of records. 
        /// </summary>
        /// <returns></returns>
        public abstract object ToObject();

        public abstract void Visit(IValueVisitor visitor);
    }
}
