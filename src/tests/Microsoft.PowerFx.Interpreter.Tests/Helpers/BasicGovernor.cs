// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Xunit;

namespace Microsoft.PowerFx.Tests
{
    // Custom exception thrown by Governor object. 
    // Host implements the Governor and can control what's thrown. 
    // Host also calls eval and controls what's caught. 
    internal class GovernorException : Exception
    {
        public GovernorException(string msg)
            : base(msg)
        {
        }
    }

    internal class BasicGovernor : Governor
    {
        private readonly long _memStart;
        private readonly long _maxAllowedBytes;

        public BasicGovernor(long maxAllowedBytes)
        {
            // This API requires NetStandard 2.1.
            // Interpreter is built against 2.0.
            _memStart = GC.GetAllocatedBytesForCurrentThread();
            _maxAllowedBytes = maxAllowedBytes;
        }

        // Beware - this is a thread-local. But evals are stuck on a single thread anyways. 
        // So we can't evaluate this property in the debugger. 
        // This API requires at least NetStandard 2.1. 
        public long CurrentBytesUsed => GC.GetAllocatedBytesForCurrentThread() - _memStart;

        public override void PollMemory(long allocateBytes)
        {
            var used = CurrentBytesUsed;
            if (used + allocateBytes > _maxAllowedBytes)
            {
                throw new GovernorException($"memory overuse");
            }
        }

        public override void Poll()
        {
            PollMemory(0);
        }
    }

    // Illustrate invariants about governor
    public class GovernorTests
    {
        [Fact]
        public void BasicGovernorTest()
        {
            var governor = new BasicGovernor(10 * 1000);
            governor.Poll(); // safe

            Assert.Throws<GovernorException>(() => governor.PollMemory(20 * 1000));

            var bytes = new byte[40 * 1000];

            // We've now crossed the limit. 
            Assert.Throws<GovernorException>(() => governor.Poll());
            bytes = null; // GC will free it up, process can continue.

            // New counter is fine. 
            var governor2 = new BasicGovernor(10 * 1000);
            governor2.Poll(); // safe
        }
    }
}
