// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    public class SignatureInformation : IEquatable<SignatureInformation>
    {
        public string Label { get; set; }

        public string Documentation { get; set; }

        public ParameterInformation[] Parameters { get; set; }

        // If non-null, then show an disclaimer after the description. 
        public Func<MarkdownString> GetDisclaimerMarkdown { get; set; }

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

        public override bool Equals(object obj)
        {
            return obj is SignatureInformation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Label, Documentation).GetHashCode();
        }
    }
}
