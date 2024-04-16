// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    internal class LanguageServerForTesting : LanguageServer
    {
        private readonly List<Exception> _unhandledExceptions = new List<Exception>();

        public List<Exception> UnhandledExceptions => _unhandledExceptions;

        public LanguageServerForTesting(IPowerFxScopeFactory scopeFactory, ILanguageServerOperationHandlerFactory handlerFactory, IHostTaskExecutor hostTaskExecutor, ILanguageServerLogger logger)
            : base(scopeFactory, hostTaskExecutor, logger)
        {
            HandlerFactory = handlerFactory;
            LogUnhandledExceptionHandler += (e) => _unhandledExceptions.Add(e);
        }
    }
}
