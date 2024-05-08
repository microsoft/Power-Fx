// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.PowerFx.LanguageServerProtocol;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestHostCancelationHandler : IHostCancelationHandler
    {
        private readonly Dictionary<string, CancellationTokenSource> _sources = new ();

        public CancellationTokenSource Create(string id)
        {
            var source = new CancellationTokenSource();
            _sources.TryAdd(id, source);
            return source;
        }

        public void CancelByRequestId(string requestId)
        {
            if (_sources.TryGetValue(requestId, out var source))
            {
                source.Cancel();
            }
        }
    }
}
