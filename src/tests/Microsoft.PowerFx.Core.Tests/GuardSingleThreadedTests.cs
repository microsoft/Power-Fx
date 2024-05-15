// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Annotations;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    // Do static analysis to look for potential threading issues. 
    public class GuardSingleThreadedTests : PowerFxTest
    {
        [Fact]
        public void GuardTestOk()
        {
            var r = new GuardSingleThreaded();

            r.VerifyNoWriters();

            using (r.Enter())
            {
                // In middle of a write block. 
                Assert.Throws<InvalidOperationException>(() => r.VerifyNoWriters());
            } // Dispose called             

            r.VerifyNoWriters();
        }

        // Gaurd detect code that is called on multiple threads. 
        [Fact]
        public void SingleThreadedUsageIsOk()
        {
            var w = new Worker();

            // Single threaded is ok. 
            for (int i = 0; i < 5; i++)
            {
                w.SingleThreadedOp();
            }
        }

        private class Worker
        {
            private readonly GuardSingleThreaded _guard = new GuardSingleThreaded();

            private int _count = 0;

            public void SingleThreadedOp()
            {
                using (_guard.Enter())
                {
                    int x = _count;
                    Thread.Sleep(1); // 1 ms

                    _count = x + 1; // single threaded op
                }
            }
        }

        // Helper to run fp() on a 2nd thread, and assert it throws. 
        private static void Assert2ndThreadThrows(Action fp)
        {
            bool exceptionThrown = false;

            Thread t = new Thread(() =>
            {
                try
                {
                    fp(); // should throw
                }
                catch
                {
                    // Success - expected failure 
                    exceptionThrown = true;
                }
            });
            t.Start();
            t.Join();

            Assert.True(exceptionThrown);
        }

        [Fact]
        public void GuardForbidWriters()
        {
            var r = new GuardSingleThreaded();

            r.ForbidWriters();

            // Ok. 
            r.VerifyNoWriters();

            // Fails - can't have writers. 
            Assert.Throws<InvalidOperationException>(() => r.Enter());
        }

        [Fact]
        public void GuardTestNotReentrant()
        {
            var r = new GuardSingleThreaded();
            using (r.Enter())
            {
                // Throws, not reentrant
                Assert.Throws<InvalidOperationException>(() => r.Enter());
            } // Dispose called             
        }

        // Ensure we fail if 2nd thread tries to enter region. 
        [Fact]
        public void EnterOn2ndThreadFails()
        {
            GuardSingleThreaded guard = new GuardSingleThreaded();

            var i1 = guard.Enter();

            Assert2ndThreadThrows(() =>
            {
                var i2 = guard.Enter(); // should fail!
            });
        }

        // Dispose on wrong thread!
        [Fact]
        public void DisposeOnWrongThreadFails()
        {
            GuardSingleThreaded guard = new GuardSingleThreaded();

            var i1 = guard.Enter();

            Assert2ndThreadThrows(() =>
            {
                // Wrong thread!
                i1.Dispose();
            });
        }

        // Extra dipose
        [Fact]
        public void ExtraDisposeFails()
        {
            GuardSingleThreaded guard = new GuardSingleThreaded();

            var i1 = guard.Enter();
            i1.Dispose();

            // Extra dispose. 
            Assert.Throws<InvalidOperationException>(() => i1.Dispose());
        }
    }
}
