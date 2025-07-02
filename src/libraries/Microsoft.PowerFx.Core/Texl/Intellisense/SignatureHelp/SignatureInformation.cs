// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    /// <summary>
    /// Represents the signature of something callable, such as a function, including its label, documentation, and parameters.
    /// </summary>
    public class SignatureInformation : IEquatable<SignatureInformation>
    {
        /// <summary>
        /// Gets or sets the label of this signature. Will be shown in the UI.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the human-readable documentation comment of this signature. Will be shown in the UI but can be omitted.
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// Gets or sets the parameters of this signature.
        /// </summary>
        public ParameterInformation[] Parameters { get; set; }

        /// <summary>
        /// If non-null, provides a disclaimer to show after the description.
        /// </summary>
        public Func<MarkdownString> GetDisclaimerMarkdown { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="SignatureInformation"/> is equal to the current <see cref="SignatureInformation"/>.
        /// </summary>
        /// <param name="other">The <see cref="SignatureInformation"/> to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public bool Equals(SignatureInformation other)
        {
            if (other == null)
            {
                return false;
            }

            // Label is not enough to compare two signature information
            if (Label != other.Label)
            {
                return false;
            }

            // Functions like Boolean, and Left have similar signature/label but different documentation
            // For example Boolean(text) => "Converts a 'text' that represents a boolean to a boolean value
            //                        And
            //             Boolean(text) =>  "Converts a 'number' to a boolean value."
            // This requires us to consider documentation as well
            if (Documentation != other.Documentation)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is SignatureInformation other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return (Label, Documentation).GetHashCode();
        }
    }
}
