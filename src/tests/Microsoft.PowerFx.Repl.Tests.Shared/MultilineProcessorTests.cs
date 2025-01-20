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

        // Each of these cases should "continue" (returning null) until the last line is input
        [Theory]
        [InlineData(false, "Sum(1,", "2)")]
        [InlineData(false, "Sum(1,", "2", ",3)")]
        [InlineData(false, "{x:3", ",", "y:4}")]
        [InlineData(false, "Mid(\"a\", 2,)")] // parse error, still completes. 
        [InlineData(false, "a = ", "3")]
        [InlineData(false, "a = // end of line comment", "3")]
        [InlineData(false, "a = /* inline comment */", "3")]
        [InlineData(false, "$\" { \"hi\"", " } \"")]
        [InlineData(false, "$\" { $\" \"\" { 1 ", " + ", "2 } ", "\"", "}", "\"")]
        [InlineData(false, "$\" { $\" { 1 /* } ", " + } ", "2 } ", "\"", " */ }", "\"", "}", "more \"")]
        [InlineData(false, "$\" { $\" { 1 // } ", " + ", "2 } ", "\"", " */ }", "\"")] // ending inline comment ignored
        [InlineData(false, "[ 1, 2, 3", "]")]
        [InlineData(false, "{ a: [ 1, 2, 3", "]", "}")]
        [InlineData(false, "First( [1,2,3]", "/* ) */", "// )", ")")]
        [InlineData(false, "First( { a: [ 1, 2, 3", "]", "}", ")")]
        [InlineData(false, "First( { a: [ 1, 2, 3", ")")] // error that returns complete for wrong delimiter
        [InlineData(false, "First( { a: [ 1, 2, 3", "}")] // error that returns complete for wrong delimiter
        [InlineData(false, "'asdfasdf\"asdfasdf'")]
        [InlineData(false, "'asdfasdf''asdfasdf'")]
        [InlineData(false, "'asdfasdf\"asdfasdf' =", "4")]
        [InlineData(false, "'asdfasdf''asdfasdf' =", "4")]
        [InlineData(false, "'asdfasdf''asdfasdf' =", " ")] // empty line terminates
        [InlineData(true, "123(")]
        [InlineData(true, "123{")]
        [InlineData(true, "123[")]
        [InlineData(true, "123${", "4", "}")]
        [InlineData(true, "123${", "4", "} {")]
        [InlineData(true, "123${", "4", "} ${", "5", "}")]
        [InlineData(true, "123${ // }", "}")]
        public void ExpectContinue(bool textFirst, params string[] lines)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var isLast = i == lines.Length - 1;

                bool isFirstLine = i == 0;
                Assert.Equal(isFirstLine, _processor.IsFirstLine);

                var result = _processor.HandleLine(line, new ParserOptions() { TextFirst = textFirst });
                sb.AppendLine(line);

                if (!isLast)
                {
                    Assert.Null(result);

                    Assert.False(_processor.IsFirstLine); // always false after HandleLine();
                } 
                else
                {
                    // Last line completes
                    // Processor may remove the last newline, compare without them
                    Assert.Equal(sb.ToString().TrimEnd(), result.TrimEnd());

                    // Reset, back to first line. 
                    Assert.True(_processor.IsFirstLine); 
                }
            }
        }   
    }
}
