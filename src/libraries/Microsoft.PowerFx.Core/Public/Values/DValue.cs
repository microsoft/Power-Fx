// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Types
{
    /// <summary>
    /// Represents the type (T | Blank | Error), where T is neither Blank nor Error
    /// TableValue stores this class (Rows), and it can be used in any situation
    /// where a value can be either a known type or Blank/Error.
    /// </summary>
    public sealed class DValue<T>
        where T : ValidFormulaValue
    {
        private DValue(T value, BlankValue blank, ErrorValue error)
        {
            Value = value;
            Blank = blank;
            Error = error;
        }

        public static DValue<T> Of(T t)
        {
            return new DValue<T>(t, null, null);
        }

        public static DValue<T> Of(BlankValue blank)
        {
            return new DValue<T>(null, blank, null);
        }

        public static DValue<T> Of(ErrorValue error)
        {
            return new DValue<T>(null, null, error);
        }

        public bool IsValue => Value != null;

        public bool IsBlank => Blank != null;

        public bool IsError => Error != null;

        public T Value { get; }

        public BlankValue Blank { get; }

        public ErrorValue Error { get; }

        public FormulaValue ToFormulaValue()
        {
            if (IsValue)
            {
                return Value;
            }
            else if (IsBlank)
            {
                return Blank;
            }
            else
            {
                return Error;
            }
        }
    }
}
