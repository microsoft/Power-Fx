// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.LanguageServerProtocol.Handlers;
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

        // Language Server class (parent of this) marks OnDataReceived Obsolete
        // This caused a lot of compiler warnings in LanguageServerTests.cs
        // since all of them use this OnDataReceived 
        // To avoid many supress, hide the base OnDataRecieved
        // With this one below which in turn calls OnDataRecieved from base
        // And suppresses the call at one place only
        public new void OnDataReceived(string jsonRpcPayload)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            base.OnDataReceived(jsonRpcPayload);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
