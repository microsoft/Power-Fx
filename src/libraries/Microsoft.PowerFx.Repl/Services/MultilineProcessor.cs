// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Repl.Functions;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Repl.Services
{
    // Handle accepting partial lines and determining when the command is complete. 
    public class MultilineProcessor
    {
        protected readonly StringBuilder _commandBuffer = new StringBuilder();

        // Useful for generating a prompt:
        // true if we're on the first line. 
        // false if we're on a subsequent line. 
        public bool IsFirstLine => _commandBuffer.Length == 0;

        // Recursively parses a formula to see if there are any open (, {, [, comments, strings, or if the formula ends with a unary prefix or binary operator
        // Recursion happens for string interoplation
        // An earlier version of this routine attempted to use the Power Fx parser directly, but interpreting erorr messages to determine continuation situations was not accurate enough
        private int ParseFormulaToClose(int bufferIndex, bool closeOnCurly, ref bool complete, ref bool error)
        {
            var brackets = new Stack<char>();       // stack of [, {, ( to ensure proper matching
            var close = false;                      // exit the loop as we've found the closing } if cloneOnCurly is true
            var leftOpen = false;                   // an identfier, string, or inline comment was left open when the end of the string was encountered
            var lastOperator = false;               // was the last non-whitespace, non-comment character an operator?

            if (closeOnCurly)
            {
                brackets.Push('}');
            }

            for (; !close && !error && bufferIndex < _commandBuffer.Length; bufferIndex++)
            {
                switch (_commandBuffer[bufferIndex])
                {
                    // text string
                    case '"':
                        var stringInterpolation = bufferIndex > 0 && _commandBuffer[bufferIndex - 1] == '$';

                        for (bufferIndex++; bufferIndex < _commandBuffer.Length; bufferIndex++)
                        {
                            if (_commandBuffer[bufferIndex] == '"')
                            {
                                if (bufferIndex + 1 < _commandBuffer.Length && _commandBuffer[bufferIndex + 1] == '"')
                                {
                                    // skip repeated quote
                                    bufferIndex++;
                                }
                                else
                                {
                                    // end delimiter reached
                                    break;
                                }
                            }
                            else if (stringInterpolation && _commandBuffer[bufferIndex] == '{')
                            {
                                if (bufferIndex + 1 < _commandBuffer.Length && _commandBuffer[bufferIndex + 1] == '{')
                                {
                                    // skip repeated {
                                    bufferIndex++;
                                }
                                else
                                {
                                    // recurse in for string interpolation island
                                    bufferIndex = ParseFormulaToClose(bufferIndex + 1, true, ref complete, ref error);
                                }
                            }
                        }

                        // reached end of string before we found our ending delimiter
                        if (bufferIndex == _commandBuffer.Length)
                        {
                            leftOpen = true;
                        }

                        lastOperator = false;
                        break;

                    // delimited identifier, which can't span lines but may have other characters within that should be ignored
                    case '\'':
                        for (bufferIndex++; bufferIndex < _commandBuffer.Length; bufferIndex++)
                        {
                            if (_commandBuffer[bufferIndex] == '\'')
                            {
                                if (bufferIndex + 1 < _commandBuffer.Length && _commandBuffer[bufferIndex + 1] == '\'')
                                {
                                    // skip repeated quote
                                    bufferIndex++;
                                }
                                else
                                {
                                    // end delimiter reached
                                    break;
                                }
                            }
                            else if (CharacterUtils.IsTabulation(_commandBuffer[bufferIndex]) || CharacterUtils.IsLineTerm(_commandBuffer[bufferIndex]))
                            {
                                // invalid in identifier names
                                error = true;
                                break;
                            }
                        }

                        // reached end of string before we found our ending delimiter
                        if (bufferIndex == _commandBuffer.Length)
                        {
                            leftOpen = true;
                        }

                        lastOperator = false;
                        break;

                    // comments or division operator
                    case '/':
                        if (bufferIndex + 1 < _commandBuffer.Length)
                        {
                            if (_commandBuffer[bufferIndex + 1] == '/')
                            {
                                for (bufferIndex += 2; bufferIndex < _commandBuffer.Length && _commandBuffer[bufferIndex] != '\n' && _commandBuffer[bufferIndex] != '\r'; bufferIndex++)
                                {
                                }

                                // the comment is closed by the end of the buffer
                            }
                            else if (_commandBuffer[bufferIndex + 1] == '*')
                            {
                                for (bufferIndex += 2; bufferIndex + 1 < _commandBuffer.Length && !(_commandBuffer[bufferIndex] == '*' && _commandBuffer[bufferIndex + 1] == '/'); bufferIndex++)
                                {
                                }

                                // reached end of string before we found our ending delimiter
                                if (bufferIndex + 1 < _commandBuffer.Length)
                                {
                                    bufferIndex++;
                                }
                                else
                                {
                                    leftOpen = true;
                                }
                            }
                            else
                            {
                                // division operator
                                lastOperator = true;
                            }
                        }
                        else
                        {
                            // division operator at end of buffer
                            lastOperator = true;
                        }

                        // if it was indeed a comment and not division, lastOperator not updated, comments ignored
                        break;

                    // table notation
                    case '[':
                        brackets.Push(']');
                        lastOperator = false;
                        break;

                    // function parameters and grouping notation
                    case '(':
                        brackets.Push(')');
                        lastOperator = false;
                        break;

                    // record notation
                    case '{':
                        brackets.Push('}');
                        lastOperator = false;
                        break;

                    // closing notation
                    case ']':
                    case ')':
                    case '}':
                        if (brackets.Count == 0 || _commandBuffer[bufferIndex] != brackets.Pop())
                        {
                            error = true;
                        }

                        if (brackets.Count == 0 && _commandBuffer[bufferIndex] == '}' && closeOnCurly)
                        {
                            if (leftOpen || lastOperator)
                            {
                                error = true;
                            }

                            close = true;
                        }

                        lastOperator = false;
                        break;

                    // whitespace
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        // lastOperator not updated, whitepace ignored
                        break;

                    // binary and unary prefix operators that can't end a formula and may well be continued on the next line                    
                    case '=':
                    case '>':
                    case '<':
                    case ':':
                    case '+':
                    case '-':
                    case '*': // division is handled above with comments
                    case '^':
                    case '&': // string concatenation and &&
                    case '|': // ||
                    case '!': // not and old property selector
                        lastOperator = true;
                        break;

                    // everything else
                    default:
                        lastOperator = false;
                        break;
                }
            }

            complete &= brackets.Count == 0 && !leftOpen && !lastOperator;

            return bufferIndex;
        }

        // Return null if we need more input. 
        // else return string containing multiple lines together. 
        public virtual string HandleLine(string line, ParserOptions parserOptions)
        {
            _commandBuffer.AppendLine(line);
            
            var complete = true;    // the buffer is complete, no longer continue, any open phrase will make this false
            var error = false;      // an error was encountered, no longer continue, overriding a false complete

            // An empty line (possibly with spaces) will also finish the command.
            // This is the ultimate escape hatch if our closing detection logic above fails.
            var emptyLine = line.Trim() == string.Empty;

            if (!emptyLine)
            {
                if (parserOptions.TextFirst)
                {
                    for (int bufferIndex = 0; complete && bufferIndex >= 0 && bufferIndex < _commandBuffer.Length; bufferIndex++)
                    {
                        if ((bufferIndex == 0 || _commandBuffer[bufferIndex - 1] != '$') && _commandBuffer[bufferIndex] == '$' && bufferIndex + 1 < _commandBuffer.Length && _commandBuffer[bufferIndex + 1] == '{')
                        {
                            bufferIndex = ParseFormulaToClose(bufferIndex + 2, true, ref complete, ref error);
                        }
                    }
                }
                else
                {
                    ParseFormulaToClose(0, false, ref complete, ref error);
                }
            }

            if (complete || error || emptyLine)
            {
                string commandBufferString = _commandBuffer.ToString();

                // Removes one newline from the end (\r\n or just \n) for the enter provided by the user.
                // Important for TextFirst lexer mode where a newline would be significant.
                if (commandBufferString.EndsWith("\r\n", StringComparison.InvariantCulture))
                {
                    commandBufferString = commandBufferString.Substring(0, commandBufferString.Length - 2);
                }
                else if (commandBufferString.EndsWith("\n", StringComparison.InvariantCulture))
                {
                    commandBufferString = commandBufferString.Substring(0, _commandBuffer.Length - 1);
                }

                Clear();

                return commandBufferString;
            }
            else
            {
                return null;
            }
        }

        public void Clear()
        {
            _commandBuffer.Clear();
        }
    }
}
