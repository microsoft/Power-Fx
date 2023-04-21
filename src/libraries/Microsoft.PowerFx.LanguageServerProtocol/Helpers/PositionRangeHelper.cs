// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.LanguageServerProtocol.Protocol;

namespace Microsoft.PowerFx.LanguageServerProtocol
{
    /// <summary>
    /// An entity with helper methods to convert ranges to positions, and positions to ranges and other logic around ranges and positions.
    /// </summary>
    internal static class PositionRangeHelper
    {
        /// <summary>
        /// Converts the given range into coressponding start and end indexes.
        /// </summary>
        /// <param name="range">Range to convert to flat positions.</param>
        /// <param name="expression">Expression from which range was created.</param>
        /// <param name="eol">End of line character.</param>
        /// <param name="oneBasedRange">Optional: Indicates whether range is one based or zero based.</param>
        /// <returns>Start and end indexes.</returns>
        public static (int startIndex, int endIndex) ConvertRangeToPositions(Range range, string expression, string eol, bool oneBasedRange = true)
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
    }
}
