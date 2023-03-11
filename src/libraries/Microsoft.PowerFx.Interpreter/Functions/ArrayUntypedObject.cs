// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class ArrayUntypedObject : ISupportsArray
    {
        private readonly List<IUntypedObject> _list;

        public ArrayUntypedObject(List<IUntypedObject> list)
        {
            _list = list;
        }

        public IUntypedObject this[int index] => _list[index];

        public int Length => _list.Count;

        public bool IsBlank()
        {
            return _list == null;
        }
    }
}
