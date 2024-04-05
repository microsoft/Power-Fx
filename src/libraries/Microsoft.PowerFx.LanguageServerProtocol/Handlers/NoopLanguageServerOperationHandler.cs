// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    public class NoopLanguageServerOperationHandler : ILanguageServerOperationHandler
    {
        private const string NotSupportedMessage = "Requested Operation is not supported";

        public bool IsRequest => false;

        public string LspMethod => string.Empty;

        public void Handle(LanguageServerOperationContext operationContext)
        {
            throw new System.NotImplementedException(NotSupportedMessage);
        }

        public Task HandleAsync(LanguageServerOperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException(NotSupportedMessage);
        }
    }
}
