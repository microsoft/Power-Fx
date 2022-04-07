// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.PowerFx.Tests
{
    // Async(N) returns N. Does not complete until Async(N-1) completes. 
    // Async(0) is completed.
    internal class AsyncFunctionsHelper : AsyncFunctionHelperBase
    {
        public AsyncFunctionsHelper(AsyncVerify verify)
            : base(verify, "Async")
        {
        }

        protected override async Task OnWaitPolicyAsync(int x)
        {
            // all previous instances must have finished before this completes.
            // But it's fine for them to start in paralle. 
            await WaitFor(x);            
        }
    }
}
