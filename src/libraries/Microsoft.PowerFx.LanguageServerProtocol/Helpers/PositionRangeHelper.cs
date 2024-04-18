// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol.Protocol;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// An entity with helper methods to convert ranges to positions, and positions to ranges and other logic around ranges and positions.
    /// </summary>
    internal static class PositionRangeHelper
    {
        public const char EOL = '\n';

        /// <summary>
        /// Converts the given range into coressponding start and end indexes.
        /// </summary>
        /// <param name="range">Range to convert to flat positions.</param>
        /// <param name="expression">Expression from which range was created.</param>
        /// <param name="eol">End of line character.</param>
        /// <param name="oneBasedRange">Optional: Indicates whether range is one based or zero based.</param>
        /// <returns>Start and end indexes.</returns>
        public static (int startIndex, int endIndex) ConvertRangeToPositions(this Range range, string expression, string eol, bool oneBasedRange = true)
        {
            if (!range.IsValid())
            {
                return (-1, -1);
            }

            var startIndex = -1;
            var endIndex = -1;

            var currentLineNumber = oneBasedRange ? 1 : 0;
            var positionOnCurrentLineNumber = oneBasedRange ? 1 : 0;
            var endOfLineCharMatchIndex = 0;
            for (var i = 0; i < expression.Length; i++)
            {
                if (currentLineNumber == range.Start.Line && positionOnCurrentLineNumber == range.Start.Character)
                {
                    startIndex = i;
                } 

                if (currentLineNumber == range.End.Line && positionOnCurrentLineNumber == range.End.Character)
                {
                    endIndex = i;
                    return startIndex < 0 || endIndex < 0 ? (-1, -1) : (startIndex, endIndex);
                }

                if (endOfLineCharMatchIndex < eol.Length)
                {
                    if (expression[i] == eol[endOfLineCharMatchIndex])
                    {
                        if (++endOfLineCharMatchIndex >= eol.Length)
                        {
                            currentLineNumber++;
                            positionOnCurrentLineNumber = oneBasedRange ? 0 : -1;
                            endOfLineCharMatchIndex = 0;
                        }
                    }
                    else
                    {
                        endOfLineCharMatchIndex = 0;
                    }
                }

                positionOnCurrentLineNumber++;
            }

            if (startIndex > -1 && currentLineNumber == range.End.Line && positionOnCurrentLineNumber == range.End.Character)
            {
                endIndex = expression.Length;
            }

            return startIndex < 0 || endIndex < 0 ? (-1, -1) : (startIndex, endIndex);
        }

        /// <summary>
        /// Get the position offset (starts with 0) in Expression from line/character (starts with 0)
        /// e.g. "123", line:0, char:1 => 1.
        /// <param name="expression">Expression.</param>
        /// <param name="line">Line number in the expression.</param>
        /// <param name="character">Character number in the expression.</param>
        /// <param name="eol"> End of line character.</param>
        /// <returns>Position offset (1D) from line and column/character (2D).</returns>
        /// </summary>
        // TODO: This is a buggy implementation. Would revisit this.
        public static int GetPosition(string expression, int line, int character, char eol = EOL)
        {
            Contracts.AssertValue(expression);
            Contracts.Assert(line >= 0);
            Contracts.Assert(character >= 0);

            var position = 0;
            var currentLine = 0;
            var currentCharacter = 0;
            while (position < expression.Length)
            {
                if (line == currentLine && character == currentCharacter)
                {
                    return position;
                }

                if (expression[position] == EOL)
                {
                    currentLine++;
                    currentCharacter = 0;
                }
                else
                {
                    currentCharacter++;
                }

                position++;
            }

            return position;
        }

        /// <summary>
        /// Construct a Range based on a Span for a given expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="span">The Span.</param>
        /// <returns>Generated Range.</returns>
        public static Range ConvertSpanToRange(this Span span, string expression)
        {
            var startChar = GetCharPosition(expression, span.Min) - 1;
            var endChar = GetCharPosition(expression, span.Lim) - 1;

            var startCode = expression.Substring(0, span.Min);
            var code = expression.Substring(span.Min, span.Lim - span.Min);
            var startLine = startCode.Split(EOL).Length;
            var endLine = startLine + code.Split(EOL).Length - 1;

            var range = new Range()
            {
                Start = new Position()
                {
                    Character = startChar,
                    Line = startLine
                },
                End = new Position()
                {
                    Character = endChar,
                    Line = endLine
                }
            };

            Contracts.Assert(range.IsValid());

            return range;
        }

        /// <summary>
        /// Get the charactor position (starts with 1) from its line.
        /// e.g. "123\n1{2}3" ==> 2 ({x} is the input char at position)
        ///      "12{\n}123" ==> 3 ('\n' belongs to the previous line "12\n", the last char is '2' with index of 3).
        /// </summary>
        /// <param name="expression">The expression content.</param>
        /// <param name="position">The charactor position (starts with 0).</param>
        /// <returns>The charactor position (starts with 1) from its line.</returns>
        public static int GetCharPosition(string expression, int position)
        {
            Contracts.AssertValue(expression);
            Contracts.Assert(position >= 0);

            var column = (position < expression.Length && expression[position] == EOL) ? 0 : 1;
            position--;
            while (position >= 0 && expression[position] != EOL)
            {
                column++;
                position--;
            }

            return column;
        }
    }
}
