// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Localization
{
    /// <summary>
    /// Represents an exception that occurs while reading and validating an xml document.
    /// It's intended to be safe to display the message back to the user.
    /// </summary>
    internal sealed class InvalidXmlException : XmlException
    {
        /// <param name="reason">The user-friendly reason why the xml document is invalid.</param>
        public InvalidXmlException(string reason)
            : base(reason)
        {
            Contracts.AssertNonEmpty(reason);
        }

        public InvalidXmlException(string reason, XAttribute invalidAttr)
            : base(reason, null, GetLineNumber(invalidAttr), GetLinePosition(invalidAttr))
        {
            Contracts.AssertNonEmpty(reason);
            Contracts.AssertValue(invalidAttr);
        }

        public InvalidXmlException(string reason, XElement invalidElem)
            : base(reason, null, GetLineNumber(invalidElem), GetLinePosition(invalidElem))
        {
            Contracts.AssertNonEmpty(reason);
            Contracts.AssertValue(invalidElem);
        }

        public InvalidXmlException(string reason, XObject invalidXObject)
            : base(reason, null, GetLineNumber(invalidXObject), GetLinePosition(invalidXObject))
        {
            Contracts.AssertNonEmpty(reason);
            Contracts.AssertValue(invalidXObject);
        }

        private static int GetLineNumber(XObject xobj)
        {
            Contracts.AssertValue(xobj, "The ctor chosen requires the invalid XObject argument to be non-null.");
            return ((IXmlLineInfo)xobj).LineNumber;
        }

        private static int GetLinePosition(XObject xobj)
        {
            Contracts.AssertValue(xobj, "The ctor chosen requires the invalid XObject argument to be non-null.");
            return ((IXmlLineInfo)xobj).LinePosition;
        }

        public InvalidXmlException()
        {
        }

        public InvalidXmlException(string message, System.Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
