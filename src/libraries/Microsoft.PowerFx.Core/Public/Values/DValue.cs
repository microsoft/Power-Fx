﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Public.Values
{
    /// <summary>
    /// Represents the type (T | Blank | Error), where T is neither Blank nor Error
    /// TableValue stores this class (Rows), and it can be used in any situation
    /// where a value can be either a known type or Blank/Error
    /// </summary>
    public class DValue<T> where T : ValidFormulaValue
    {
        private T _value;
        private BlankValue _blank;
        private ErrorValue _error;

        private DValue(T value, BlankValue blank, ErrorValue error)
        {
            _value = value;
            _blank = blank;
            _error = error;
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

        public bool IsValue => _value != null;
        public bool IsBlank => _blank != null;
        public bool IsError => _error != null;

        public T Value => _value;
        public BlankValue Blank => _blank;
        public ErrorValue Error => _error;

        public FormulaValue ToFormulaValue()
        {
            if (IsValue)
                return Value;
            else if (IsBlank)
                return Blank;
            else
                return Error;
        }
    }
}
