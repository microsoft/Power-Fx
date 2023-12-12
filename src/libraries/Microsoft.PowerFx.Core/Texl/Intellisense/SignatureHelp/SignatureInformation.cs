// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerFx.Intellisense.SignatureHelp
{
    public class SignatureInformation : IEquatable<SignatureInformation>
    {
        public string Label { get; set; }

        private string _documentation = string.Empty;

        public string Documentation
        {
            get
            {
                if (SignatureInformation.IsAIFunction(Label, out var name))
                {
                    return "This is " + name + " Function;  **Disclaimer**: AI-generated content can have mistakes. Make sure it's accurate and appropriate before using it. [See terms](https://powerplatform.microsoft.com/en-us/legaldocs/supp-powerplatform-preview/)";
                }

                return _documentation;
            }

            set => _documentation = value;
        }

        public ParameterInformation[] Parameters { get; set; }

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

        private static bool IsAIFunction(string label, out string name)
        {
            var names = new string[] { "AIClassify", "AIExtract", "AIModelPublish", "AIReply", "AISentiment", "AISummarize", "AISummarizeRecord", "AITranslate" };
            foreach (var func in names)
            {
                if (label.Contains(func))
                {
                    name = func;
                    return true;
                }
            }

            name = null;
            return false;
        }
    }

    public class MarkdownDocumentation
    {
        public string Value { get; set; }
    }

    public class MarkdownSignatureInfomation
    {
        public string Label { get; set; }

        public MarkdownDocumentation Documentation { get; set; }

        public ParameterInformation[] Parameters { get; set; }

        public MarkdownSignatureInfomation(SignatureInformation signatureInformation)
        {
            Label = signatureInformation.Label;
            Documentation = new MarkdownDocumentation() { Value = signatureInformation.Documentation };
            Parameters = signatureInformation.Parameters;
        }
    }
}
