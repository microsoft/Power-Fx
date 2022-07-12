// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Interpreter
{
    /// <summary>
    /// This keeps track of how many calls deep we are in a function.
    /// </summary>
    internal struct StackDepthCounter
    {
        private readonly int _depth;

        internal StackDepthCounter Increment()
        {
            if (_depth == 0)
            {
                throw new MaxCallDepthException();
            }

            return new StackDepthCounter(_depth - 1);
        }

        public StackDepthCounter(int maxCallDepth)
        {
            _depth = maxCallDepth;
        }
    }
}
