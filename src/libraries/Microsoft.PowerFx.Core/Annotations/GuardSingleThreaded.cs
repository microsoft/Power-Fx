// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Security.Cryptography;
using System.Threading;

namespace Microsoft.PowerFx.Core.Annotations
{
    // Helper to verify a region is single threaded.
    // Any exception here is a bug in the host - they're calling a single-threaded API on multiple threads. 
    // This is not a lock - a lock will block a thread until it can enter. Whereas the guard will fail (not block),
    // since the host should never even attempt multiple threads. 
    internal class GuardSingleThreaded
    {
        // 0 is open, else set to threadId
        private int _id;

        private class GuardInstance : IDisposable
        {
            private readonly GuardSingleThreaded _parent;
            private readonly int _currentId;

            public GuardInstance(GuardSingleThreaded parent)
            {
                _parent = parent;
                _currentId = Environment.CurrentManagedThreadId;

                var oldId = Interlocked.Exchange(ref parent._id, _currentId);
                if (oldId != 0)
                {
                    // Another thread is already in the region. 
                    // This is a bug in the host. 
                    throw new InvalidOperationException($"Multiple threads attempting to enter a single threaded region. Current {_currentId}. Existing: {oldId}.");
                }
            }

            public void Dispose()
            {
                var thisId = Environment.CurrentManagedThreadId;
                if (thisId != _currentId)
                {
                    // Should never be possible if we use 'using' and keep Dispose() on same thread.
                    throw new InvalidOperationException($"Operation switched thread. Started {_currentId}. Now {thisId}.");
                }

                var oldId = Interlocked.Exchange(ref _parent._id, 0);
                if (oldId != _currentId)
                {
                    throw new InvalidOperationException($"Operation mismatched. Started {oldId}. Current {_currentId}.");
                }
            }
        }

        /// <summary>
        /// Readers can run in parallel. Verify there are no current writes. 
        /// </summary>
        /// <exception cref="InvalidOperationException">If violated.</exception>
        public void VerifyNoWriters()
        {
            if (_id > 0)
            {
                throw new InvalidOperationException($"Can't read while another thread is writing. Other {_id}.");
            }
        }

        // Convenience for calling VerifyNoWriters() in a property getter. 
        public T VerifyNoWriters<T>(T inner)
        {
            this.VerifyNoWriters();
            return inner;
        }

        /// <summary>
        /// Mark this guard as "no writers allowed". Any attempt to enter a write region will fail.
        /// </summary>
        public void ForbidWriters()
        {
            // non-zero will mean any attempt to Enter will fail. 
            // But still let VerifyNoWriters() succeed. 
            _id = -1;
        }

        /// <summary>
        /// Enter a "write" region. Only 1 thread can enter this region at a time. 
        /// </summary>
        /// <returns></returns>
        public IDisposable Enter()
        {            
            var t = new GuardInstance(this);
            return t;
        }
    }
}
