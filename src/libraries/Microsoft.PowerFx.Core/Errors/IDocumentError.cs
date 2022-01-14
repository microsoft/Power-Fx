// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.AppMagic.Transport;
using Microsoft.PowerFx.Core.Localization;

namespace Microsoft.PowerFx.Core.Errors
{
    // TASK: 83259 - Document wide error reporting.

    // Base interface for reporting document wide errors. All internal components
    // that need to expose error details publicly should derive from this interface.
    [TransportType(TransportKind.ByValue)]
    [TransportUnion(typeof(IRuleError), typeof(IDocumentError))]
    internal interface IDocumentError
    {
        /// <summary>
        /// The inner error for this document error, or null if there is no inner error.
        /// </summary>
        IDocumentError InnerError { get; }

        /// <summary>
        /// The kind of error.
        /// </summary>
        DocumentErrorKind ErrorKind { get; }

        /// <summary>
        /// The error message to be consumed by UI.
        /// </summary>
        string ShortMessage { get; }

        /// <summary>
        /// Returns a longer explanation of the error.
        /// </summary>
        string LongMessage { get; }

        /// <summary>
        /// The key of the error message to be consumed by UI.
        /// </summary>
        string MessageKey { get; }

        /// <summary>
        /// Returns the key of the error message. Used for building new errors out of existing ones.
        /// </summary>
        [TransportDisabled]
        ErrorResourceKey ErrorResourceKey { get; }

        /// <summary>
        /// Returns the args of the error message. Used for building new errors out of existing ones, in some cases.
        /// </summary>
        [TransportDisabled]
        object[] MessageArgs { get; }

        /// <summary>
        /// Strings describing potential fixes to the error.
        /// This can be null if no information exists, or one
        /// or more strings.
        /// </summary>
        IList<string> HowToFixMessages { get; }

        /// <summary>
        /// A description about why the error should be addressed.
        /// This may be null if no information exists.
        /// </summary>
        string WhyToFixMessage { get; }

        /// <summary>
        /// A set of URLs to support articles for this error.
        /// Used by client UI to display helpful links.
        /// </summary>
        IList<IErrorHelpLink> Links { get; }

        /// <summary>
        /// Name of the entity for the error.
        /// </summary>
        string Entity { get; }

        /// <summary>
        /// Id of the entity for the error.
        /// </summary>
        string EntityId { get; }

        /// <summary>
        /// Name of the parent of the entity if there is one.
        /// This could be empty string if this is not a rule error.
        /// </summary>
        string Parent { get; }

        /// <summary>
        /// The property this error is for to be consumed by UI.
        /// This could be empty string if this is not a rule error.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        /// TextSpan for the rule error.
        /// This could be null if this is not a rule error.
        /// </summary>
        Span TextSpan { get; }

        /// <summary>
        /// SinkTypeErrors for the rule error.
        /// This could be null if this is not a rule error.
        /// </summary>
        IEnumerable<string> SinkTypeErrors { get; }

        /// <summary>
        /// The error severity.
        /// </summary>
        DocumentErrorSeverity Severity { get; }

        /// <summary>
        /// The internal exception, which can be null
        /// This is for diagnostic purposes only, NOT for display to the end-user.
        /// </summary>
        [TransportDisabled] // not available to script
        Exception InternalException { get; }
    }
}
