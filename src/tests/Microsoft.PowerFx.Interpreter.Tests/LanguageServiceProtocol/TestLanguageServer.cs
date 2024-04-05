// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.LanguageServerProtocol;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestLanguageServer : LanguageServer
    {
        public TestLanguageServer(ITestOutputHelper output, SendToClient sendToClient, IPowerFxScopeFactory scopeFactory, INLHandlerFactory nlHandlerFactory = null)
#pragma warning disable CS0618 // Type or member is obsolete
            : base(sendToClient, scopeFactory, (string s) => output.WriteLine(s))
#pragma warning restore CS0618 // Type or member is obsolete
        {            
            NLHandlerFactory = nlHandlerFactory;
        }

        public int TestGetCharPosition(string expression, int position) => PositionRangeHelper.GetCharPosition(expression, position);

        public int TestGetPosition(string expression, int line, int character) => PositionRangeHelper.GetPosition(expression, line, character);
    }
}
