// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Errors
{
    internal abstract class BaseError : IDocumentError
    {
        /// <summary>
        /// The inner error, if any. This may be null.
        /// </summary>
        public IDocumentError InnerError { get; }

        /// <summary>
        /// The kind of error.
        /// </summary>
        public DocumentErrorKind ErrorKind { get; }

        /// <summary>
        /// Returns the short error message to be consumed by UI.
        /// </summary>
        public string ShortMessage { get; }

        /// <summary>
        /// Returns a longer explanation of the error.
        /// </summary>
        public string LongMessage { get; }

        /// <summary>
        /// Returns the key of the error message to be consumed by UI.
        /// </summary>
        public string MessageKey { get; }

        public ErrorResourceKey ErrorResourceKey => new ErrorResourceKey(MessageKey);

        /// <summary>
        /// Returns the args of the error message. Used for building new errors out of existing ones, in some cases.
        /// </summary>
        public object[] MessageArgs { get; private set; }

        /// <summary>
        /// Strings describing potential fixes to the error.
        /// This can be null if no information exists, or one
        /// or more strings.
        /// </summary>
        public IList<string> HowToFixMessages { get; private set; }

        /// <summary>
        /// A description about why the error should be addressed.
        /// This may be null if no information exists.
        /// </summary>
        public string WhyToFixMessage { get; private set; }

        /// <summary>
        /// A set of URLs to support articles for this error.
        /// Used by client UI to display helpful links.
        /// </summary>
        public IList<IErrorHelpLink> Links { get; private set; }

        /// <summary>
        /// Returns the name of the entity for the error.
        /// </summary>
        public string Entity { get; protected set; }

        /// <summary>
        /// Returns the ID of the entity for the error.
        /// </summary>
        public string EntityId { get; protected set; }

        /// <summary>
        /// Returns the name of the parent of the entity if there is one.
        /// This could be empty string if this is not a rule error.
        /// </summary>
        public string Parent { get; protected set; }

        /// <summary>
        /// Returns the property this error is for to be consumed by UI.
        /// This could be empty string if this is not a rule error.
        /// </summary>
        public string PropertyName { get; protected set; }

        /// <summary>
        /// TextSpan for the rule error.
        /// This could be null if this is not a rule error.
        /// </summary>
        public virtual Span TextSpan { get; }

        /// <summary>
        /// SinkTypeErrors for the rule error.
        /// This could be null if this is not a rule error.
        /// </summary>
        public virtual IEnumerable<string> SinkTypeErrors { get; }

        /// <summary>
        /// The error severity.
        /// </summary>
        public DocumentErrorSeverity Severity { get; }

        /// <summary>
        /// The internal exception, which can be null
        /// This is for diagnostic purposes only, NOT for display to the end-user.
        /// </summary>
        public Exception InternalException { get; }

        private const string HowToFixSuffix = "_HowToFix";

        internal BaseError(IDocumentError innerError, Exception internalException, DocumentErrorKind kind, DocumentErrorSeverity severity, ErrorResourceKey errKey, params object[] args)
            : this(innerError, internalException, kind, severity, errKey, textSpan: null, sinkTypeErrors: null, args: args)
        {
        }

        internal BaseError(IDocumentError innerError, Exception internalException, DocumentErrorKind kind, DocumentErrorSeverity severity, ErrorResourceKey errKey, Span textSpan, IEnumerable<string> sinkTypeErrors, params object[] args)
        {
            Contracts.AssertValueOrNull(innerError);
            Contracts.AssertValueOrNull(args);
            Contracts.AssertValueOrNull(internalException);
            Contracts.AssertValueOrNull(textSpan);
            Contracts.AssertValueOrNull(sinkTypeErrors);

            InnerError = innerError;
            ErrorKind = kind;
            Entity = string.Empty;
            PropertyName = string.Empty;
            Parent = string.Empty;
            Severity = severity;
            InternalException = internalException;
            TextSpan = textSpan;
            SinkTypeErrors = sinkTypeErrors;
            MessageKey = errKey.Key;
            MessageArgs = args;

            // We expect errKey to be the key for an error resource object within string resources.
            // We fall back to using a basic content string within string resources, for errors
            // that haven't yet been converted to an ErrorResource in the Resources.pares file.
            string shortMessage;
            string longMessage;
            if (!StringResources.TryGetErrorResource(errKey, out var errorResource))
            {
                errorResource = null;
                shortMessage = StringResources.Get(errKey.Key);
                longMessage = null;
            }
            else
            {
                shortMessage = errorResource.GetSingleValue(ErrorResource.ShortMessageTag);
                Contracts.AssertValue(shortMessage);
                longMessage = errorResource.GetSingleValue(ErrorResource.LongMessageTag);
            }

            ShortMessage = FormatMessage(shortMessage, args);
            LongMessage = FormatMessage(longMessage, args);
            HowToFixMessages = errorResource?.GetValues(ErrorResource.HowToFixTag) ?? GetHowToFix(errKey.Key);
            WhyToFixMessage = errorResource?.GetSingleValue(ErrorResource.WhyToFixTag) ?? string.Empty;
            Links = errorResource?.HelpLinks;
        }

        private string FormatMessage(string message, params object[] args)
        {
            if (message == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            if (args != null && args.Length > 0)
            {
                try
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, message, args);
                }
                catch (FormatException)
                {
                    // Just in case we let a poorly escaped format string (eg a column name with {}s in it) get this far
                    // we will degrade the quality of the error report, but keep running at least
                    sb.Append(message);
                }
            }
            else
            {
                sb.Append(message);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Retrieves the "HowToFix" messages for a particular message key. These messages should either
        /// be named with the suffix "_HowToFix" or "_HowToFix1, _HowToFix2..." if multiple exist.
        ///
        /// NOTE: Usage of this pattern is deprecated. New errors should use the StringResources.ErrorResouce
        /// format to specify HowToFix messages.
        /// </summary>
        /// <param name="messageKey">Key for the error message.</param>
        /// <param name="locale"></param>
        /// <returns>List of how to fix messages. Null if none exist.</returns>
        internal static IList<string> GetHowToFix(string messageKey, string locale = null)
        {
            Contracts.AssertNonEmpty(messageKey);

            // Look for singular Message_HowToFix
            var howToFixSingularKey = messageKey + HowToFixSuffix;
            if (StringResources.TryGet(howToFixSingularKey, out var howToFixSingularMessage, locale))
            {
                return new List<string> { howToFixSingularMessage };
            }

            // Look for multiple how to fix messages: Message_HowToFix1, Message_HowToFix2...
            var messages = new List<string>();
            for (var messageIndex = 1; StringResources.TryGet(howToFixSingularKey + messageIndex, out var howToFixMessage, locale); messageIndex++)
            {
                messages.Add(howToFixMessage);
            }

            return messages.Count == 0 ? null : messages;
        }

        private void Format(StringBuilder sb)
        {
#if DEBUG
            var lenStart = sb.Length;
#endif
            FormatCore(sb);
#if DEBUG
            Contracts.Assert(sb.Length > lenStart);
#endif
            FormatInnerError(sb);
        }

        internal virtual void FormatCore(StringBuilder sb)
        {
            Contracts.AssertValue(sb);

            sb.Append(TexlStrings.InfoMessage);
            sb.Append(ShortMessage);
        }

        private void FormatInnerError(StringBuilder sb)
        {
            Contracts.AssertValue(sb);

            if (InnerError == null)
            {
                return;
            }

            sb.AppendLine();
            var innerError = InnerError as BaseError;
            Contracts.AssertValue(innerError);
            innerError?.Format(sb);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Format(sb);
            return sb.ToString();
        }
    }
}
