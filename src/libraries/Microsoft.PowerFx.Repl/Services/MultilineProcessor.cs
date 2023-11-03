// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.PowerFx.Core.Localization;
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

        // Return null if we need more input. 
        // else return string containing multiple lines together. 
        public virtual string HandleLine(string line)
        {
            _commandBuffer.AppendLine(line);

            string commandBufferString = _commandBuffer.ToString();

            // Remove the trailing newline if it exists as it disturbs Intellisense
            if (commandBufferString.EndsWith("\r\n", StringComparison.OrdinalIgnoreCase))
            {
                commandBufferString = commandBufferString.Substring(0, commandBufferString.Length - 2);
            }

            // We use the parser results to determine if the command is complete or more lines are needed.
            // The Engine features and parser options do not need to match what we will actually use,
            // this just needs to be good enough to detect the errors below for multiline processing.
            var result = Engine.Parse(commandBufferString);

            // We will get this error, with this argument, if we are missing closing punctuation.
            var missingClose = result.Errors.Any(error => error.MessageKey == "ErrExpectedFound_Ex_Fnd" &&
                                                    ((TokKind)error.MessageArgs.Last() == TokKind.ParenClose ||
                                                     (TokKind)error.MessageArgs.Last() == TokKind.CurlyClose ||
                                                     (TokKind)error.MessageArgs.Last() == TokKind.BracketClose));

            // However, we will get false positives from the above if the statement is very malformed.
            // For example: Mid("a", 2,) where the second error about ParenClose expected at the end is incorrect.
            // In this case, more characters will not help and we should complete the command and report the errors with what we have.
            var badToken = result.Errors.Any(error => error.MessageKey == "ErrBadToken");

            // An empty line (possibly with spaces) will also finish the command.
            // This is the ultimate escape hatch for the user if our closing detection logic above fails.
            var emptyLine = Regex.IsMatch(commandBufferString, @"\n[ \t]*\r?\n$", RegexOptions.Multiline);

            if (!missingClose || badToken || emptyLine)
            {
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
