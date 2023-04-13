// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.App.ErrorContainers
{
    internal interface IErrorContainer
    {
        /// <summary>
        /// The severity to use in the default EnsureError and Error functions. Is not
        /// used in the Errors function.
        /// </summary>
        DocumentErrorSeverity DefaultSeverity { get; }

        /// <summary>
        /// Only adds and returns the error if its severity is equal to or higher
        /// than the existing errors for the node in the container.
        ///
        /// Severity is defaulted to critical.
        /// </summary>
        TexlError EnsureError(TexlNode node, ErrorResourceKey errKey, params object[] args);

        /// <summary>
        /// Adds an error to the container and returns the composed error value
        /// that was inserted.
        ///
        /// Severity is defaulted to critical.
        /// </summary>
        TexlError Error(TexlNode node, ErrorResourceKey errKey, params object[] args);

        /// <summary>
        /// Only adds and returns the error if its severity is equal to or higher
        /// than the existing errors for the node in the container.
        /// </summary>
        TexlError EnsureError(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args);

        TexlError EnsureError(DocumentErrorSeverity severity, Token token, ErrorResourceKey errKey, params object[] args);

        /// <summary>
        /// Adds an error to the container and returns the composed error value
        /// that was inserted.
        /// </summary>
        TexlError Error(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args);

        /// <summary>
        /// Used to apply errors due to differing type schemas. Use schemaDifferenceType = DType.Invalid to indicate
        /// that the schema difference is due to a missing member.
        /// </summary>
        void Errors(TexlNode node, DType nodeType, KeyValuePair<string, DType> schemaDifference, DType schemaDifferenceType);
    }
}
