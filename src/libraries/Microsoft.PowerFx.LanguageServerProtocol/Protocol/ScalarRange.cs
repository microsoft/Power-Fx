// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class ScalarRange
    {
        public bool IsValid()
        {
            return Start >= 0 && End >= 0 && Start <= End;
        }

        /// <summary>
        /// Zero-based offset, the range's start position.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// Zero-based offset, the range's end position.
        /// </summary>
        public int End { get; set; }
    }
}
