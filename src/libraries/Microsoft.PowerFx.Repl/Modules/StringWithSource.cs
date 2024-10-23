// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Repl
{
    /// <summary>
    /// This is a string object, but also tagged with a solution location for where the string contents start. 
    /// This is restircted to Inline strings (and rejects other formats like fodling, quotes, etc). 
    /// Use a custom converter to populate the source location of the string. See <see cref="StringWithSourceConverter"/> .
    /// </summary>
    internal class StringWithSource
    {
        /// <summary>
        /// The contents of the string from the yaml. 
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The location in the file for where the contents start. Useful for error reporting. 
        /// </summary>
        public FileLocation Location { get; set; }
    }
}
