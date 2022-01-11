// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Localization
{
    internal class ErrorResource
    {
        public const string XmlType = "errorResource";

        // The default error message.
        public const string ShortMessageTag = "shortMessage";
        // Optional: A longer explanation of the error. There is currently no UI (or DocError) support for this.
        public const string LongMessageTag = "longMessage";
        // Optional: A series of messages explaining how to fix the error.
        public const string HowToFixTag = "howToFixMessage";
        // Optional: A series of messages explaining why to fix the error. Used primarily for accessibility errors.
        public const string WhyToFixTag = "whyToFixMessage";
        // Optional: A series of links to help documents. There is currently no UI (or DocError) support for this.
        public const string LinkTag = "link";
        public const string LinkTagDisplayTextTag = "value";
        public const string LinkTagUrlTag = "url";



        internal const string ReswErrorResourcePrefix = "ErrorResource_";
        internal static readonly Dictionary<string, string> ErrorResourceTagToReswSuffix = new Dictionary<string, string>()
        {
            { ShortMessageTag, "_ShortMessage" },
            { LongMessageTag, "_LongMessage" },
            { HowToFixTag, "_HowToFix" },
            { WhyToFixTag, "_WhyToFix" },
            { LinkTag, "_Link" },
        };

        internal static bool IsTagMultivalue(string tag) => tag == HowToFixTag || tag == LinkTag;


        private readonly Dictionary<string, IList<string>> TagToValues;
        public IList<IErrorHelpLink> HelpLinks { get; }

        private ErrorResource()
        {
            TagToValues = new Dictionary<string, IList<string>>();
            HelpLinks = new List<IErrorHelpLink>();
        }

        public static ErrorResource Parse(XElement errorXml)
        {
            Contracts.AssertValue(errorXml);

            var errorResource = new ErrorResource();

            // Parse each sub-element into the TagToValues dictionary.
            foreach (var tag in errorXml.Elements())
            {
                var tagName = tag.Name.LocalName;

                // Links are specialized because they are a two-part resource.
                if (tagName == LinkTag)
                {
                    errorResource.AddHelpLink(tag);
                }
                else
                {
                    if (!errorResource.TagToValues.ContainsKey(tagName))
                    {
                        errorResource.TagToValues[tagName] = new List<string>();
                    }

                    errorResource.TagToValues[tagName].Add(tag.Element("value").Value);
                }
            }

            return errorResource;
        }

        public static ErrorResource Reassemble(Dictionary<string, Dictionary<int, string>> members)
        {
            Contracts.AssertAllNonEmpty(members.Keys);
            Contracts.AssertAllValues(members.Values);

            var errorResource = new ErrorResource();

            // Reassemble link 2-part resources first
            // They need to match up. Because these resources are loaded for almost all tests,
            // The asserts here will fail during unit tests if they're incorrectly defined
            if (members.TryGetValue(LinkTag, out var linkValues))
            {
                members.TryGetValue(LinkTagUrlTag, out var urls).Verify();
                Contracts.Assert(linkValues.Count == urls.Count);

                foreach (var kvp in linkValues)
                {
                    urls.TryGetValue(kvp.Key, out var correspondingUrl).Verify();
                    errorResource.HelpLinks.Add(new ErrorHelpLink(kvp.Value, correspondingUrl));
                }

                members.Remove(LinkTag);
                members.Remove(LinkTagUrlTag);
            }


            foreach (var tag in members)
            {
                if (!errorResource.TagToValues.ContainsKey(tag.Key))
                {
                    errorResource.TagToValues[tag.Key] = new List<string>();
                }

                foreach (var value in tag.Value.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
                {
                    errorResource.TagToValues[tag.Key].Add(value);
                }
            }

            return errorResource;
        }

        private void AddHelpLink(XElement linkTag)
        {
            Contracts.AssertValue(linkTag);

            HelpLinks.Add(new ErrorHelpLink(linkTag.Element(LinkTagDisplayTextTag).Value, linkTag.Element(LinkTagUrlTag).Value));
        }

        public string GetSingleValue(string tag)
        {
            Contracts.AssertValue(tag);

            if (!TagToValues.ContainsKey(tag))
            {
                return null;
            }

            Contracts.Assert(TagToValues[tag].Count == 1);

            return TagToValues[tag][0];
        }

        public IList<string> GetValues(string tag)
        {
            if (!TagToValues.ContainsKey(tag))
            {
                return null;
            }

            return TagToValues[tag];
        }
    }
}
