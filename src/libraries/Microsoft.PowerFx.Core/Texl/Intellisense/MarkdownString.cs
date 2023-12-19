// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Intellisense
{
    /// <summary>
    /// A string that represents Github flavored markdown. 
    /// For use in displaying in intellisense. 
    /// </summary>
    public class MarkdownString
    {
        public MarkdownString(string markdown)
        {
            this.Markdown = markdown;
        }

        public string Markdown { get; }
    }
}
