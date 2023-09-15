// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Intellisense;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// This interface is applied to Request/Param objects. 
    /// This message has an TextDocumentItem, which can be used to get a <see cref="IPowerFxScope"/>. 
    /// </summary>
    internal interface IHasTextDocument
    {
        public TextDocumentItem TextDocument { get; set; }
    }
}
