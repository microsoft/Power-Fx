// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.LanguageServerProtocol.Protocol
{
    public class ScalarRange
    {
        public bool IsValid()
        {
            return Start <= End;
        }

        /// <summary>
        /// The range's start position.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The range's end position.
        /// </summary>
        public int End { get; set; }
    }
}
