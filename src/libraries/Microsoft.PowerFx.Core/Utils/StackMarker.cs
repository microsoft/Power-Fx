// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Core.Utils
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

            return new StackMarker(_maxCallDepth, _depth + 1);
        }

        public StackMarker(int maxCallDepth, int depth = 0)
        {
            _depth = depth;
            _maxCallDepth = maxCallDepth;
        }
    }
}
