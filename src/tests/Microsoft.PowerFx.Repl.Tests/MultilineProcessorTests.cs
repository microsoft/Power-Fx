// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Repl.Services;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Repl.Tests
{
    public class MultilineProcessorTests
    {
        private readonly MultilineProcessor _processor = new MultilineProcessor();

        // After case is multiple lines. Last line finally completes.
        [Theory]
        [InlineData("Sum(1,", "2)")]
        [InlineData("Sum(1,", "2", ",3)")]
        [InlineData("{x:3", ",", "y:4}")]
        [InlineData("Mid(\"a\", 2,)")] // parse error, still compeltes. 
        public void ExpectContinue(params string[] lines)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var isLast = i == lines.Length - 1;

                bool isFirstLine = i == 0;
                Assert.Equal(isFirstLine, _processor.IsFirstLine);

                var result = _processor.HandleLine(line);
                sb.AppendLine(line);

                if (!isLast)
                {
                    Assert.Null(result);

                    Assert.False(_processor.IsFirstLine); // always false after HandleLine();
                } 
                else
                {
                    // Last line completes
                    Assert.Equal(sb.ToString(), result);

                    // Reset, back to first line. 
                    Assert.True(_processor.IsFirstLine); 
                }
            }
        }   
    }
}
