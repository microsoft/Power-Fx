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
            
            // The following line is just debug to keep track of call depth. Its painful to have to google it everytime I test recursion so I'm leaving it here commented out.
            // If this breaks some sort of convention I'll remove the comment.
            //System.Diagnostics.Debug.WriteLine($"CurrentCallDepth: {_currentCallDepth}");
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
