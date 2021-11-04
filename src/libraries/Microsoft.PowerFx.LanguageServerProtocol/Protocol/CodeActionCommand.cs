// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action command object model
    /// </summary>
    public class CodeActionCommand
    {
        /// <summary>
        /// ctor
        /// </summary>
        public CodeActionCommand()
        {
            Arguments = new object[] { };
        }

        /// <summary>
        /// Gets or sets title of the command.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets command to be executed.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets command arguments.
        /// </summary>
        public object[] Arguments { get; set; }
    }
}
