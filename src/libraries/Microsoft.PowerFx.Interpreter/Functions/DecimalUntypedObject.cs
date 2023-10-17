// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class DecimalUntypedObject : IUntypedObject
    {
        private readonly decimal _value;

        public DecimalUntypedObject(decimal value)
        {
            _value = value;
        }

        public FormulaType Type => FormulaType.Decimal;

        public IUntypedObject this[int index] => throw new NotImplementedException();

        public int GetArrayLength()
        {
            throw new NotImplementedException();
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal()
        {
            return _value;
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
    }
}
