// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action command object model.
    /// </summary>
    public class CodeActionCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeActionCommand"/> class.
        /// ctor.
        /// </summary>
        public CodeActionCommand()
        {
            Arguments = Array.Empty<object>();
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
        public IEnumerable<object> Arguments { get; set; }
    }
}
