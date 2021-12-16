// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class Range
    {
        public Range()
        {
            Start = new Position();
            End = new Position();
        }

        /// <summary>
        /// The range's start position.
        /// </summary>
        public Position Start { get; set; }

        /// <summary>
        /// The range's end position.
        /// </summary>
        public Position End { get; set; }
    }
}
