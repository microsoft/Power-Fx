// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.IR
{
    internal struct StackMarker
    {
        private readonly int _depth;
        private readonly int _maxCallDepth;

        internal StackMarker Inc()
        {
            if (_depth >= _maxCallDepth)
            {
                throw new MaxCallDepthException();
            }

            return new StackMarker(_depth + 1, _maxCallDepth);
        }

        public StackMarker(int depth, int maxCallDepth)
        {
            _depth = depth;
            _maxCallDepth = maxCallDepth;
        }
    }
}
