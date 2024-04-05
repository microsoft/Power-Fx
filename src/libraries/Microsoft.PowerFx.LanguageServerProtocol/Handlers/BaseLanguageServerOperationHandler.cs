// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    public abstract class BaseLanguageServerOperationHandler : ILanguageServerOperationHandler
    { 
        public abstract string LspMethod { get; }

        public abstract Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken);

        public void Handle(LanguageServerOperationContext operationContext)
        {
            HandleAsync(operationContext, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
