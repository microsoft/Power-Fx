// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Tests
{
    // WaitFor(N) hard fails if WaitFor(N-1) hasn't complete yet. 
    internal class WaitForFunctionsHelper : AsyncFunctionHelperBase
    {
        public WaitForFunctionsHelper(AsyncVerify verify)
            : base(verify, "WaitFor")
        {
        }

        protected override Task OnWaitPolicyAsync(int x)
        {
            // Fail if any previous instance started before this one.
            var t = WaitFor(x);
            if (!t.IsCompleted)
            {
                throw new InvalidOperationException($"Task {x} has not completed. Bad parallelism");
            }

            return t;
        }
    }
}
