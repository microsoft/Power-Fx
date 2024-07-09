// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class FloatUntypedObject : IUntypedObject
    {
        private readonly double _value;

        public FloatUntypedObject(double value)
        {
            _value = value;
        }

        public FormulaType Type => FormulaType.Number;

        public IUntypedObject this[int index] => throw new NotImplementedException();

        public int GetArrayLength()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            return _value;
        }

        public decimal GetDecimal()
        {
            throw new NotImplementedException();
        }

        public string GetUntypedNumber()
        {
            throw new NotImplementedException();
        }

        public string GetString()
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean()
        {
            throw new NotImplementedException();
        }

        public bool TryGetProperty(string value, out IUntypedObject result)
        {
            throw new NotImplementedException();
        }

        public bool TryGetPropertyNames(out IEnumerable<string> result)
        {
            result = null;
            return false;
        }

        public bool TrySetProperty(string name, IUntypedObject value)
        {
            throw new NotImplementedException();
        }
    }
}
