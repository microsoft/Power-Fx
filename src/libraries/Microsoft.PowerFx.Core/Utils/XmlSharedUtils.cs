// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static partial class XmlSharedUtils
    {
        [System.Diagnostics.DebuggerHidden]
        public static Guid? GetOptionalGuidAttribute(this XElement element, XName name)
        {
            Contracts.AssertValue(element);
            Contracts.AssertValue(name);

            return element.Attribute(name) == null ? (Guid?)null : element.GetRequiredGuidAttribute(name);
        }

        [System.Diagnostics.DebuggerHidden]
        public static Guid GetRequiredGuidAttribute(this XElement element, XName name)
        {
            Contracts.AssertValue(element);
            Contracts.AssertValue(name);

            XAttribute xattr = element.GetRequiredNonEmptyAttribute(name);
            Guid guid;
            if (!Guid.TryParse((string)xattr, out guid))
                throw new InvalidXmlException(string.Format(StringResources.Get("InvalidXml_AttributeValueInvalidGuid_AttrName_Value"), xattr.Name, xattr.Value), xattr);

            return guid;
        }

        /// <summary>
        /// Gets the <see cref="XAttribute"/> and throws an <see cref="InvalidXmlException"/> exception if it doesn't exist or it has an empty value.
        /// </summary>
        [System.Diagnostics.DebuggerHidden]
        public static XAttribute GetRequiredNonEmptyAttribute(this XElement element, XName name)
        {
            Contracts.AssertValue(element);
            Contracts.AssertValue(name);

            XAttribute xattr = element.GetRequiredAttribute(name);
            if (string.IsNullOrEmpty(xattr.Value))
                throw new InvalidXmlException(string.Format(StringResources.Get("InvalidXml_AttributeCannotBeEmpty_AttrName"), xattr.Name), xattr);

            return xattr;
        }

        /// <summary>
        /// Gets the <see cref="XAttribute"/> and throws an <see cref="InvalidXmlException"/> exception if it has an empty value.
        /// </summary>
        [System.Diagnostics.DebuggerHidden]
        public static XAttribute GetOptionalNonEmptyAttribute(this XElement element, XName name)
        {
            Contracts.AssertValue(element);
            Contracts.AssertValue(name);

            return element.Attribute(name) == null ? null : element.GetRequiredNonEmptyAttribute(name);
        }

        /// <summary>
        /// Gets the <see cref="XAttribute"/> and throws an <see cref="InvalidXmlException"/> exception if it doesn't exist.
        /// </summary>
        [System.Diagnostics.DebuggerHidden]
        public static XAttribute GetRequiredAttribute(this XElement element, XName name)
        {
            Contracts.AssertValue(element);
            Contracts.AssertValue(name);

            XAttribute xattr = element.Attribute(name);
            if (xattr == null)
                throw new InvalidXmlException(string.Format(StringResources.Get("InvalidXml_ElementMissingAttribute_ElemName_AttrName"), element.Name, name), element);

            return xattr;
        }

        /// <summary>
        /// Queries for an Xml element with the given name.
        /// </summary>
        /// <param name="element">This XElement that should be queried.</param>
        /// <param name="childElementName">Name of the element that should be queried for.</param>
        /// <param name="childElement">Element if the query was successful. Null otherwise.</param>
        /// <returns>True if the query was successful. False otherwise.</returns>
        public static bool TryGetElement(this XElement element, XName childElementName, out XElement childElement)
        {
            Contracts.CheckValue(element, "element");
            Contracts.CheckValue(childElementName, "childElementName");

            childElement = element.Element(childElementName);

            return childElement != null;
        }

        /// <summary>
        /// Queries for a non-empty attribute value with a given name in the current Xml element.
        /// </summary>
        /// <param name="element">The XElement that should be queried.</param>
        /// <param name="attributeName">Name of the attribute that should be queried for.</param>
        /// <param name="value">String value if non-empty attribute was found with the given name. Else null.</param>
        /// <returns>True if a non-empty attribute was found. False otherwise.</returns>
        public static bool TryGetNonEmptyAttributeValue(this XElement element, XName attributeName, out string value)
        {
            Contracts.CheckValue(element, "element");
            Contracts.CheckValue(attributeName, "attributeName");

            XAttribute attribute = element.Attribute(attributeName);
            if (attribute == null)
            {
                value = null;
                return false;
            }

            if (string.IsNullOrEmpty(attribute.Value))
            {
                value = null;
                return false;
            }

            value = attribute.Value;
            return true;
        }

        /// <summary>
        /// Queries for an integer attribute value with a given name in the current Xml element.
        /// </summary>
        /// <param name="element">The XElement that should be queried.</param>
        /// <param name="attributeName">Name of the attribute that should be queried for.</param>
        /// <param name="value">Integer value if valid attribute was found with the given name. Else int.MinValue.</param>
        /// <returns>True if a valid int attribute was found. False otherwise.</returns>
        public static bool TryGetIntegerAttributeValue(this XElement element, XName attributeName, out int value)
        {
            Contracts.CheckValue(element, "element");
            Contracts.CheckValue(attributeName, "attributeName");

            string attributeValue;
            if (element.TryGetNonEmptyAttributeValue(attributeName, out attributeValue)
                && int.TryParse(attributeValue, out value))
            {
                return true;
            }

            value = int.MinValue;
            return false;
        }

        /// <summary>
        /// Queries for a boolean attribute value with a given name in the current XML element.
        /// </summary>
        /// <param name="element">The XElement that should be queried.</param>
        /// <param name="attributeName">Name of the attribute that should be queried for.</param>
        /// <param name="value">boolean value if valid attribute was found with the given name. Else false.</param>
        /// <returns>True if a valid boolean attribute was found. False otherwise.</returns>
        public static bool TryGetBoolAttributeValue(this XElement element, XName attributeName, out bool value)
        {
            Contracts.CheckValue(element, "element");
            Contracts.CheckValue(attributeName, "attributeName");

            value = false;
            string attributeValue;
            return element.TryGetNonEmptyAttributeValue(attributeName, out attributeValue) && bool.TryParse(attributeValue, out value);
        }
    }
}
