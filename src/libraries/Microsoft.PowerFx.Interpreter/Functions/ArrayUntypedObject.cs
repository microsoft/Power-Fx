﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class ArrayUntypedObject : IUntypedObject
    {
        private readonly List<IUntypedObject> _list;

        public ArrayUntypedObject(List<IUntypedObject> list)
        {
            _list = list;
        }

        public FormulaType Type => ExternalType.ArrayType;

        public IUntypedObject this[int index] => _list[index];

        public int GetArrayLength()
        {
            return _list.Count;
        }

        public double GetDouble()
        {
            throw new NotImplementedException();
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
    }
}
