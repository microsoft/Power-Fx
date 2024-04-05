// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol.Handlers
{
    /// <summary>
    /// A factory that always returns the same NLHandler instance.
    /// Firstly, langauge server sdk took the nl handler to perform nl2fx and fx2nl from the host which was not thread safe.
    /// So, a second approach using INlHandlerFactory was introduced to create a new instance of NLHandler for each request.
    /// But with the new handler archritecture, we don't need both of them.
    /// But we need to backwards compatible with the old code.
    /// So instead of two backward compatible approaches, this factory unifies first and second so we only have one backwards compatible approach.
    /// </summary>
    internal class BackwardsCompatibleNLHandlerFactory : INLHandlerFactory
    {
        private readonly NLHandler _nlHandler;

        public BackwardsCompatibleNLHandlerFactory(NLHandler nlHandler)
        {
            _nlHandler = nlHandler;
        }

        public NLHandler GetNLHandler(IPowerFxScope scope, BaseNLParams nlParams)
        {
            return _nlHandler;
        }
    }
}
