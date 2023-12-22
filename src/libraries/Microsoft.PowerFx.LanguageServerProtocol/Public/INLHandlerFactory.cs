// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol.Public
{
    public interface INLHandlerFactory
    {
        public NLHandler GetNlHandler(IPowerFxScope uri);
    }
}
