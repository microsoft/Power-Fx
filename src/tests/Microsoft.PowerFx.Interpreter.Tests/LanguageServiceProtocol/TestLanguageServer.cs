// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.LanguageServerProtocol;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Tests.LanguageServiceProtocol
{
    public class TestLanguageServer : LanguageServer
    {
        public TestLanguageServer(ITestOutputHelper output, SendToClient sendToClient, IPowerFxScopeFactory scopeFactory)
            : base(sendToClient, scopeFactory)
        {
            SetLogger((string s) => output.WriteLine(s));
        }

        public int TestGetCharPosition(string expression, int position) => GetCharPosition(expression, position);

        public int TestGetPosition(string expression, int line, int character) => GetPosition(expression, line, character);
    }
}
