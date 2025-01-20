// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Texl.Builtins;
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

        private int ParseFormulaToClose(int i, bool closeOnCurly, ref bool complete, ref bool error)
        {
            var brackets = new Stack<char>();       // stack of [, {, ( to ensure proper matching
            var close = false;                      // exit the loop as we've found the closing } if cloneOnCurly is true
            var leftOpen = false;                   // an identfier, string, or inline comment was left open when the end of the string was encountered
            var lastChar = '\0';                    // the last character seen, not including whitespace and comments, for detecting a trailing =

            if (closeOnCurly)
            {
                brackets.Push('}');
            }

            for (; !close && !error && i < _commandBuffer.Length; i++)
            {
                var thisChar = _commandBuffer[i];

                switch (thisChar)
                {
                    case '"':
                    case '\'':
                        var stringInterpolation = thisChar == '"' && i > 0 && _commandBuffer[i - 1] == '$';

                        for (i++; i < _commandBuffer.Length; i++) 
                        {
                            if (_commandBuffer[i] == thisChar)
                            {
                                if (i + 1 < _commandBuffer.Length && _commandBuffer[i + 1] == thisChar)
                                {
                                    // skip repeated quote
                                    i++;
                                }
                                else
                                {
                                    // end delimiter reached
                                    break;
                                }
                            }
                            else if (stringInterpolation && _commandBuffer[i] == '{')
                            {
                                if (i + 1 < _commandBuffer.Length && _commandBuffer[i + 1] == '{')
                                {
                                    // skip repeated {
                                    i++;        
                                }
                                else
                                {
                                    // recurse in for string interpolation island
                                    i = ParseFormulaToClose(i + 1, true, ref complete, ref error);
                                }
                            }
                        }

                        // reached end of string before we found our ending delimiter
                        if (i == _commandBuffer.Length)
                        {
                            leftOpen = true;
                        }

                        lastChar = thisChar;
                        break;

                    case '/':
                        if (i + 1 < _commandBuffer.Length)
                        {
                            if (_commandBuffer[i + 1] == '/')
                            {
                                for (i += 2; i < _commandBuffer.Length && _commandBuffer[i] != '\n' && _commandBuffer[i] != '\r'; i++)
                                {
                                }

                                // the comment is closed by the end of the buffer
                            }
                            else if (_commandBuffer[i + 1] == '*')
                            {
                                for (i += 2; i + 1 < _commandBuffer.Length && !(_commandBuffer[i] == '*' && _commandBuffer[i + 1] == '/'); i++)
                                {
                                }

                                // reached end of string before we found our ending delimiter
                                if (i + 1 == _commandBuffer.Length)
                                {
                                    leftOpen = true;
                                }
                            }
                        }

                        // lastChar not updated, comment ignored
                        break;

                    case '[':
                        brackets.Push(']');

                        // lastChar not updated, stack will be up and won't be complete
                        break;

                    case '(':
                        brackets.Push(')');

                        // lastChar not updated, stack will be up and won't be complete
                        break;

                    case '{':
                        brackets.Push('}');

                        // lastChar not updated, stack will be up and won't be complete
                        break;

                    case ']':
                    case ')':
                    case '}':
                        if (brackets.Count == 0 || thisChar != brackets.Pop())
                        {
                            error = true;
                        }
                        
                        if (brackets.Count == 0 && thisChar == '}' && closeOnCurly)
                        {
                            if (leftOpen || lastChar == '=')
                            {
                                error = true;
                            }

                            close = true;
                        }

                        lastChar = thisChar;
                        break;

                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        // lastChar not updated, whitepace ignored
                        break;

                    default:
                        lastChar = thisChar;
                        break;
                }
            }

            complete &= brackets.Count == 0 && !leftOpen && lastChar != '=';

            return i;
        }

        // Return null if we need more input. 
        // else return string containing multiple lines together. 
        public virtual string HandleLine(string line, ParserOptions parserOptions)
        {
            _commandBuffer.AppendLine(line);

            var error = false;
            var complete = true;

            // An empty line (possibly with spaces) will also finish the command.
            // This is the ultimate escape hatch for the user if our closing detection logic above fails.
            var emptyLine = line.Trim() == string.Empty;

            if (!emptyLine)
            {
                if (parserOptions.TextFirst)
                {
                    for (int i = 0; complete && i >= 0 && i < _commandBuffer.Length; i++)
                    {
                        if ((i == 0 || _commandBuffer[i - 1] != '$') && _commandBuffer[i] == '$' && i + 1 < _commandBuffer.Length && _commandBuffer[i + 1] == '{')
                        {
                            i = ParseFormulaToClose(i + 2, true, ref complete, ref error);
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
