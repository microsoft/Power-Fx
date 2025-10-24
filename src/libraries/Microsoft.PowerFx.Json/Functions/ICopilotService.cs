// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.Core
{
    [ThreadSafeImmutable]
    public interface ICopilotService : IDisposable
    {
        Task<string> AskTextAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
