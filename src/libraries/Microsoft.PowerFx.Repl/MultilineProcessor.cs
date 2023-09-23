// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;

namespace Microsoft.PowerFx
{
    // Handle accepting partial lines and determining when the command is complete. 
    public class MultilineProcessor
    {
        private readonly StringBuilder _commandBuffer = new StringBuilder();

        // Useful for generating a prompt:
        // true if we're on the first line. 
        // false if we're on a subsequent line. 
        public bool IsFirstLine => _commandBuffer.Length == 0;

        // Return null if we need more input. 
        // else return string containing multiple lines together. 
        public virtual string HandleLine(string line)
        {
            _commandBuffer.AppendLine(line);

            // $$$ fix this check and apply ReadFormula logic.
            bool complete = !line.TrimEnd('\r', '\n').EndsWith(" ", StringComparison.Ordinal); 

            if (complete)
            {
                var cmd = _commandBuffer.ToString();
                _commandBuffer.Clear();
                return cmd;
            }

            return null;
        }
    }
}
