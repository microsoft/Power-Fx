﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    /// <summary>
    /// Code action request parameters.
    /// </summary>
    public class CodeActionParams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeActionParams"/> class.
        /// ctor.
        /// </summary>
        public CodeActionParams()
        {
            Context = new CodeActionContext();
        }

        /// <summary>
        /// Gets or sets text document object.
        /// </summary>
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets current editor value range object.
        /// </summary>
        public Range Range { get; set; }

        /// <summary>
        /// Code action context carries additonal information.
        /// </summary>
        public CodeActionContext Context { get; set; }
    }
}
