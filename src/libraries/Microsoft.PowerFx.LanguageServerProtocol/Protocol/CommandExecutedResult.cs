// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// The command executed result.
    /// </summary>
    public class CommandExecutedParams
    {
        /// <summary>
        /// Gets or sets the command name.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        public string Argument { get; set; }
    }
}
