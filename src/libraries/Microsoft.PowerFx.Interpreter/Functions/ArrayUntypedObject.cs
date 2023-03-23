// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal class ArrayUntypedObject : UntypedArray
    {
        private readonly List<IUntypedObject> _list;

        public ArrayUntypedObject(List<IUntypedObject> list)
        {
            _list = list;
        }

        public override IUntypedObject this[int index] => _list[index];

        public override int Length => _list.Count;

        public override bool IsBlank()
        {
            return _list == null;
        }
    }
}
