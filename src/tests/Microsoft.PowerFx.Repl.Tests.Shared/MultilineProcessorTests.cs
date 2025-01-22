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

        [Theory]

        // basics
        [InlineData(false, "Sum(1,", "2)")]
        [InlineData(false, "Sum(1,", "")] // empty line stops continuation
        [InlineData(false, "Sum(1,", "  ")] // empty line stops continuation
        [InlineData(false, "Sum(1,", " \t ")] // empty line stops continuation
        [InlineData(false, "Sum(1,", "2", ",3)")]
        [InlineData(false, "{x:3", ",", "y:4}")]
        [InlineData(false, "Mid(\"a\", 2,)")] // parse error, still completes. 
        [InlineData(false, "[ 1, 2, 3", "]")]
        [InlineData(false, "{ a: [ 1, 2, 3", "]", "}")]
        [InlineData(false, "First( { a: [ 1, 2, 3", "]", "}", ")")]
        [InlineData(false, "First( { a: [ 1, 2, 3", ")")] // error that returns complete for wrong delimiter
        [InlineData(false, "First( { a: [ 1, 2, 3", "}")] // error that returns complete for wrong delimiter
        [InlineData(false, "\"string", "\"")]
        [InlineData(false, "/* text", "more", "end */")]
        [InlineData(false, "/* text", "more", " \t ")] // empty line stops continuation
        [InlineData(false, "/* text", "more", "  ")] // empty line stops continuation
        [InlineData(false, "/* text", "more", "")] // empty line stops continuation
        [InlineData(false, "( // ) text", ")")]
        [InlineData(false, "( /* ) text", "*/ )")]
        [InlineData(false, "[ // ] text", "]")]
        [InlineData(false, "[ /* ] text", "*/ ]")]
        [InlineData(false, "{ // } text", "}")]
        [InlineData(false, "{ /* } text", "*/ }")]
        [InlineData(false, "IsMatch( \"asdf\", \"\\w+", "\")")]

        // errors
        [InlineData(false, "[ ( // ) text", "}")] // incorrect close
        [InlineData(false, "[ ( /* ) text", "*/ ]")] // incorrect close
        [InlineData(false, "{ [ // ] text", "}")] // incorrect close
        [InlineData(false, "{ [ /* ] text", "*/ )")] // incorrect close
        [InlineData(false, "( { // } text", ")")] // incorrect close
        [InlineData(false, "( { /* } text", "*/ ]")] // incorrect close
        [InlineData(false, "{ ( 1 + 2 ) )")] // error, incorrect close
        [InlineData(false, "( 1 + 2 ) )")] // error, too many closes

        // udfs
        [InlineData(false, "func( a: Number ) : ", "Text = ", "Text( a + 4)")]
        [InlineData(false, "func( a: Number ) : Text = ", "Text( a + 4)")]
        [InlineData(false, "func( a: Number ) : Text = {", "Text( a + 4) }")]

        // ends with oeprator
        [InlineData(false, "a =", "3")]
        [InlineData(false, "a = // end of line comment", "3")]
        [InlineData(false, "a = /* inline comment */", "3")]
        [InlineData(false, "a >", "3")]
        [InlineData(false, "a > // end of line comment", "3")]
        [InlineData(false, "a > /* inline comment */", "3")]
        [InlineData(false, "a <", "3")]
        [InlineData(false, "a < // end of line comment", "3")]
        [InlineData(false, "a < /* inline comment */", "3")]
        [InlineData(false, "{ a:", "3 }")]
        [InlineData(false, "{ a : // end of line comment", "3 }")]
        [InlineData(false, "{ a : /* inline comment */", "3 }")]
        [InlineData(false, "a +", "3 ")]
        [InlineData(false, "a + // end of line comment", "3 ")]
        [InlineData(false, "a + /* inline comment */", "3 ")]
        [InlineData(false, "a -", "3 ")]
        [InlineData(false, "a - // end of line comment", "3 ")]
        [InlineData(false, "a - /* inline comment */", "3 ")]
        [InlineData(false, "a *", "3 ")]
        [InlineData(false, "a * // end of line comment", "3 ")]
        [InlineData(false, "a * /* inline comment */", "3 ")]
        [InlineData(false, "a /", "3 ")]
        [InlineData(false, "a / // end of line comment", "3 ")]
        [InlineData(false, "a / /* inline comment */", "3 ")]
        [InlineData(false, "a ^", "3 ")]
        [InlineData(false, "a ^ // end of line comment", "3 ")]
        [InlineData(false, "a ^ /* inline comment */", "3 ")]
        [InlineData(false, "\"happy \" & ", " \"days\"")]
        [InlineData(false, "\"happy \" & // end of line comment", "\"days \" ")]
        [InlineData(false, "\"happy \" & /* inline comment */", "\"days \" ")]
        [InlineData(false, "true && ", "false ")]
        [InlineData(false, "true && // end of line comment", "false ")]
        [InlineData(false, "true && /* inline comment */", "false ")]
        [InlineData(false, "true || ", "false ")]
        [InlineData(false, "true || // end of line comment", "false ")]
        [InlineData(false, "true || /* inline comment */", "false ")]
        [InlineData(false, "!", "false ")]
        [InlineData(false, "! // end of line comment", "false ")]
        [InlineData(false, "! /* inline comment */", "false ")]
        [InlineData(false, "a!b")]
        [InlineData(false, "a && ! ", "b")]

        // string interpolation
        [InlineData(false, "$\" { \"hi\"", " } \"")]
        [InlineData(false, "$\" { $\" \"\" { 1 ", " + ", "2 } ", "\"", "}", "\"")]
        [InlineData(false, "$\" { $\" { 1 /* } ", " + } ", "2 } ", "\"", " */ }", "\"", "}", "more \"")]
        [InlineData(false, "$\" { $\" { 1 // } ", " + ", "2 } ", "\"", " & ", "\"a\" }", "\"")] // ending inline comment ignored
        [InlineData(false, "$\" { $\" { 1 /* } ", " + } ", "2 } ", "\"", " */ }", "\"", "")] // empty line stops continuation
        [InlineData(false, "$\" { $\" { 1 // } ", " + ", "2 } ", "\"", " & ", "\"a\" }", "")] // empty line stops continuation
        [InlineData(false, "[ $\" { 4 + }\"")] // error, unclosed expresion in island
        [InlineData(false, "[ $\" { 4 * /* inline */ }\"")] // error, unclosed expresion in island
        [InlineData(false, "[ $\" { 4 * //", "}\"")] // error, unclosed expresion in island

        // comments
        [InlineData(false, "4 /* a *//", "3")]
        [InlineData(false, "4 / //", "3")]
        [InlineData(false, "First( [1,2,3]", "/* ) */", "// )", ")")]
        [InlineData(false, "( 1 /* error ) ", "*/ + 2 // error )", "/*) error", "4 error )*/", "5", "/* one */)")]

        // identifiers
        [InlineData(false, "'asdfasdf\"asdfasdf'")]
        [InlineData(false, "'asdfasdf''asdfasdf'")]
        [InlineData(false, "'asdfasdf\"asdfasdf' =", "4")]
        [InlineData(false, "'asdfasdf''asdfasdf' =", "4")]
        [InlineData(false, "'asdfasdf''asdfasdf' =", " ")] // empty line terminates
        [InlineData(false, "Set( '\t")] // tab is an illegal character in an identifier name
        [InlineData(false, "Set( '\n")] // newline is an illegal character in an identifier name
        [InlineData(false, "Set( '\r")] // carriage return is an illegal character in an identifier name
        [InlineData(false, "Set( '',", "3)")] // empty identifier is illegal but not worth catching at this level
        [InlineData(false, "Set( 'happy /* ' */ )")] // tick closes identifier despite looking like it is in a comment
        [InlineData(false, "'a''a'+", "'b''b'")]

        // text first mode
        [InlineData(true, "123(")]
        [InlineData(true, "123{")]
        [InlineData(true, "123[")]
        [InlineData(true, "123'asdf")]
        [InlineData(true, "${}")] // minimal
        [InlineData(true, "${ 4 ", "}")] // at beginning
        [InlineData(true, "${ 4 ","} more")] // at beginning
        [InlineData(true, "more ${", "}")] // at end
        [InlineData(true, "more ${}")] // at end
        [InlineData(true, "123${", "4", "}")]
        [InlineData(true, "123$${")] // escaped $
        [InlineData(true, "$${")] // escaped $
        [InlineData(true, "123${", "4", "} {")]
        [InlineData(true, "123${", "4", "} ${", "5", "}")]
        [InlineData(true, "123${", "4", "} {{ ${", "5", "}")]
        [InlineData(true, "123${ // }", "}")]
        [InlineData(true, "123 ${ { a: [ 1, 2, 3 }")] // error, incorrect closing bracket
        [InlineData(true, "123 ${ { a: [ 1, 2, 3 )")] // error, incorrect closing bracket
        [InlineData(true, "123 ${ [ 4 + ] }")] // error, dangling operator
        [InlineData(true, "123 ${ 1 + 2 } }")] // not an error

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
