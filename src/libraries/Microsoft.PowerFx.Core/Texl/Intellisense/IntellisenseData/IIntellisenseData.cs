// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Core.Functions;

namespace Microsoft.PowerFx.Core.Texl.Intellisense.IntellisenseData
{
    /// <summary>
    /// A transient runtime representation of data necessary to complete <see cref="Intellisense.Suggest"/>.
    /// Instances of classes that implement these are candidates to become realized as instances of
    /// <see cref="IntellisenseResult"/>.
    /// </summary>
    internal interface IIntellisenseData
    {
        /// <summary>
        /// If an Intellisense suggestion is selected, the is the start index that should be replaced.
        /// </summary>
        public int ReplacementStartIndex { get; }

        /// <summary>
        /// The number of indices from <see cref="ReplacementStartIndex"/> that the replacement should
        /// assume if a suggestion is selected.
        /// </summary>
        public int ReplacementLength { get; }

        /// <summary>
        /// The function that the intellisense data may be associated with.  This value is null if intellisense was
        /// not called from within a valid function signature.
        /// </summary>
        public TexlFunction CurFunc { get; }

        /// <summary>
        /// The number of argument present in the formula when intellisense is called.
        /// </summary>
        public int ArgCount { get; }

        /// <summary>
        /// The current index of the cursor position relative to the <see cref="CurFunc"/>'s other arguments.
        /// </summary>
        public int ArgIndex { get; }

        /// <summary>
        /// The input script for which the Intellisense was called.
        /// </summary>
        public string Script { get; }

        /// <summary>
        /// Called when the signature results of <see cref="IIntellisenseResult"/> are being created.
        /// This method should return true if the signature information has been augmented and the args
        /// matching the output parameters are set accordingly.  It should return false if the signature should
        /// remain unaltered.
        /// </summary>
        /// <param name="func">
        /// Function that pertains to the input signature.
        /// </param>
        /// <param name="argIndex">
        /// The index of the argument for which Intellisense is being calculated.
        /// </param>
        /// <param name="paramName">
        /// The name of the parameter in the signature.
        /// </param>
        /// <param name="highlightStart">
        /// The index in the string , which may be highlighted in the UI
        /// and is thusly named.
        /// </param>
        /// <param name="newHighlightStart">
        /// Should be set to the resultant new highlight start position if the method returns true.  If the method
        /// returns false, this value will be ignored.
        /// </param>
        /// <param name="newHighlightEnd">
        /// Should be set to the resultant new highlight end position if the method returns true.  If the method
        /// returns false, this value will be ignored.
        /// </param>
        /// <param name="newParamName">
        /// Should be set to the resultant new parameter name if the method returns true.  If the method
        /// returns false, this value will be ignored.
        /// </param>
        /// <param name="newInvariantParamName">
        /// Should be set to the resultant new invariant parameter name if the method returns true.  If the
        /// method returns false, this value will be ignored.
        /// </param>
        /// <returns>
        /// True if the method was augmented, false otherwise.
        /// </returns>
        public bool TryAugmentSignature(TexlFunction func, int argIndex, string paramName, int highlightStart, out int newHighlightStart, out int newHighlightEnd, out string newParamName, out string newInvariantParamName);

        /// <summary>
        /// Should return a suffix for the provided <paramref name="function"/> and <paramref name="paramName"/>.
        /// </summary>
        /// <param name="function">
        /// The suffix candidate.
        /// </param>
        /// <param name="paramName">
        /// The parameter of <paramref name="function"/> that may be suffixed.
        /// </param>
        /// <returns>
        /// Just the suffix for the parameter, and empty string if no suffix is intended.
        /// </returns>
        public string GenerateParameterDescriptionSuffix(TexlFunction function, string paramName);
    }
}
