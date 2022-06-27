// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.PowerFx.Interpreter
{
    internal class EvalContext
    {
        private int _currentCallDepth = 0;

        // We can set this up to read from powerfx config, but it isn't nessecary yet.
        private readonly int _maxCallDepth;
        private readonly Mutex _mutex = new Mutex();

        internal EvalContext(int maxCallDepth = 100)
        {
            _maxCallDepth = maxCallDepth;
        }

        /// <summary>
        /// Increments the runtime call depth counter.
        /// Is threadsafe.
        /// </summary>
        /// <returns>False when you go over the recursive depth limit, true normally.</returns>
        public bool IncrementCallDepth()
        {
            _mutex.WaitOne();
            _currentCallDepth++;
            if (_currentCallDepth >= _maxCallDepth)
            {
                _mutex.ReleaseMutex();
                return false;
            }

            _mutex.ReleaseMutex();
            
            return true;
        }

        public void DecrementCallDepth()
        {
            _mutex.WaitOne();
            _currentCallDepth--;
            _mutex.ReleaseMutex();
        }
    }
}
