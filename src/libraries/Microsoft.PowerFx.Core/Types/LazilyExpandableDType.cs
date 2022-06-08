// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core.Types
{
    internal class LazilyExpandableDType
    {
        internal delegate DType GetExpandedType();

        private readonly GetExpandedType _expansionFunc;
        private DType _expandedType; 

        public DType ExpandedType
        {
            get
            {
                _expandedType ??= _expansionFunc();
                return _expandedType;
            }
        }

        public LazilyExpandableDType(GetExpandedType expansionFunc)
        {
            _expandedType = null;
            _expansionFunc = expansionFunc;
        }
    }
}
