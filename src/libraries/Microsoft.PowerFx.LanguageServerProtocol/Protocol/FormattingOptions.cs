// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    internal class FormattingOptions
    {
        public uint TabSize { get; set; }

        public bool InsertSpaces { get; set; }

        public bool? TrimTrailingWhiteSpace { get; set; }    

        public bool? InsertFinalNewLine { get; set; }

        public bool? TrimFinalNewLines { get; set; }

        // LSP allows extension data on Formatting Options
        // If this is set then other formatting options would be ignored
        // And we would just remove formatting on the formula
        public bool RemoveFormatting { get; set; } = false;

        public string Eol { get; set; } = "\n";
    }
}
