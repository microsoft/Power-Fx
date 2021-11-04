// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class CodeActionCommand
    {
        public CodeActionCommand()
        {
            Arguments = new object[] { };
        }

        public string Title { get; set; }

        public string Command { get; set; }

        public object[] Arguments { get; set; }
    }
}
