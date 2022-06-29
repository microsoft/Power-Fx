// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.PowerFx.Core.Tests
{
    /// <summary>
    /// Ensures only one instance can exist at a time.
    /// </summary>
    public abstract class ExclusiveRunner : IDisposable
    {
        private static readonly Mutex _instanceLock = new (false);
        private bool _disposedValue;

        internal ExclusiveRunner()
        {
            _instanceLock.WaitOne();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _instanceLock.ReleaseMutex();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
