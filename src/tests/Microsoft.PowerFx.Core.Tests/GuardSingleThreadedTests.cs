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

        // Gaurd detect code that is called on multiple threads. 
        [Fact]
        public void GuardTestFail()
        {
            var w = new Worker();

            // Single threaded is ok. 
            for (int i = 0; i < 5; i++)
            {
                w.SingleThreadedOp();
            }

            // Multiple threads will hit guard. 
            try
            {
                Parallel.For(0, 10, (i) => w.SingleThreadedOp());
            }
            catch
            {
                // ok 
                return;
            }

            Assert.Null("Guard should have thrown exception");
        }

        private class Worker
        {
            private readonly GuardSingleThreaded _guard = new GuardSingleThreaded();

            private int _count = 0;

            public void SingleThreadedOp()
            {
                using (_guard.Enter())
                {
                    _count++; // single threaded op
                    Thread.Sleep(1); // 1 ms
                }
            }
        }
    }
}
