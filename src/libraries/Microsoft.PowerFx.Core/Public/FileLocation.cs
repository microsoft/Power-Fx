// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Syntax
{
    /// <summary>
    /// File-aware, Multi-line source span with Line and Column. 
    /// Wheras <see cref="Span"/> is a character span within a single expression.
    /// </summary>
    public class FileLocation
    {
        public string Filename { get; init; }

        // 1-based index
        public int LineStart { get; init; }

        public int ColStart { get; init; }

        /// <summary>
        /// Apply a span (which is a character offset within a single expression) 
        /// to this file offset. 
        /// </summary>
        /// <param name="originalText"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public FileLocation Apply(string originalText, Span s2)
        {
            // Convert index span to line span. 
            int iLine = this.LineStart;
            int iCol = ColStart;

            for (int i = 0; i < s2.Min; i++)
            {
                if (originalText[i] == '\r')
                {
                    // ignore
                }
                else if (originalText[i] == '\n')
                {
                    iLine++;
                    iCol = ColStart; // reset 
                }
                else
                {
                    iCol++;
                }
            }

            return new FileLocation
            {
                Filename = Filename,
                ColStart = iCol,
                LineStart = iLine
            };
        }
    }
}
