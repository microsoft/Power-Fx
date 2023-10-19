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

        public bool Equals(SignatureInformation other)
        {
            if (other == null)
            {
                return false;
            }

            // For now, label is enough to compare two signature information
            if (Label != other.Label)
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
            return Label.GetHashCode();
        }
    }
}
