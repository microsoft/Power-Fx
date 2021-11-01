// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerFx.Core.Functions
{
    /// <summary>
    /// This class is used to help limit signature length for variadic function. e.g.
    /// FuncName(arg1,arg1,...,arg1,...),
    /// FuncName(arg1,arg2,arg2,...,arg2,...)
    /// FuncName(arg1,arg2,arg1,arg2,...,arg1,arg2,...)
    /// FuncName(arg1,arg2,arg2,...,arg2,...,arg3,...)
    /// </summary>
    internal sealed class SignatureConstraint
    {
        public readonly int OmitStartIndex;
        public readonly int RepeatSpan;
        public readonly int EndNonRepeatCount;
        public readonly int RepeatTopLength;

        public SignatureConstraint(int omitStartIndex, int repeatSpan, int endNonRepeatCount, int repeatTopLength)
        {
            OmitStartIndex = omitStartIndex;
            RepeatSpan = repeatSpan;
            EndNonRepeatCount = endNonRepeatCount;
            RepeatTopLength = repeatTopLength;
        }

        /// <summary>
        /// Determines if argIndex needs highlight.
        /// </summary>
        /// <param name="argCount">arg count in script</param>
        /// <param name="argIndex">arg index cursor focuses on in script</param>
        /// <param name="signatureCount">arg count in signature</param>
        /// <param name="signatureIndex">signature index in funcDisplayString</param>
        public bool ArgNeedsHighlight(int argCount, int argIndex, int signatureCount, int signatureIndex)
        {
            if (argCount <= RepeatTopLength || signatureIndex <= OmitStartIndex)
                return signatureIndex == argIndex;

            if (signatureCount > RepeatTopLength && IsIndexInRange(signatureIndex, RepeatTopLength, signatureCount))
                return signatureCount - signatureIndex == argCount - argIndex;

            if (EndNonRepeatCount > 0)
            {
                /// FuncName(arg1,arg2,arg2,...,arg2,...,arg3,...)
                var tailArgRange = new[] { argCount - EndNonRepeatCount - RepeatSpan, argCount - EndNonRepeatCount };
                var tailSignatureRange = new[] { signatureCount - EndNonRepeatCount - RepeatSpan, signatureCount - EndNonRepeatCount };
                if (IsIndexInRange(argIndex, tailArgRange[0], tailArgRange[1]))
                    return IsIndexInRange(signatureIndex, tailSignatureRange[0], tailSignatureRange[1]);
            }

            return argIndex >= signatureIndex &&
                argIndex > OmitStartIndex &&
                (signatureIndex - OmitStartIndex) % RepeatSpan == (argIndex - OmitStartIndex) % RepeatSpan;
        }

        /// <summary>
        /// Determines if param can omit.
        /// </summary>
        /// <param name="argCount">arg count in script</param>
        /// <param name="argIndex">arg index cursor focuses on in script</param>
        /// <param name="signatureCount">arg count in signature</param>
        /// <param name="signatureIndex">signature index in funcDisplayString</param>
        public bool canParamOmit(int argCount, int argIndex, int signatureCount, int signatureIndex)
        {
            if (signatureCount > RepeatTopLength && IsIndexInRange(signatureIndex, RepeatTopLength, signatureCount))
                return false;

            // headOmitRange: [startIndex, endIndex)
            var headOmitRange = new[] { OmitStartIndex, OmitStartIndex + RepeatSpan };
            var tailOmitRange = new[] { signatureCount - EndNonRepeatCount - RepeatSpan, signatureCount - EndNonRepeatCount };
            if (EndNonRepeatCount > 0 && IsIndexInRange(signatureIndex, tailOmitRange[0], tailOmitRange[1]))
            {
                var tailArgRange = new[] { argCount - EndNonRepeatCount - RepeatSpan, argCount - EndNonRepeatCount };
                return !IsIndexInRange(argIndex, tailArgRange[0], tailArgRange[1]);
            }

            // return true if argIndex is out of headOmitRange and signatureIndex is within headOmitRange
            return IsIndexInRange(signatureIndex, headOmitRange[0], headOmitRange[1]) &&
                !IsIndexInRange(argIndex, headOmitRange[0], headOmitRange[1]);
        }

        private bool IsIndexInRange(int index, int startIndex, int endIndex)
        {
            return index >= startIndex && index < endIndex;
        }
    }

    // Abstract base class for all language functions.
}
