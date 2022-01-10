// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Syntax.Nodes;
using Microsoft.PowerFx.Core.Types;

namespace Microsoft.PowerFx.Core.App.ErrorContainers
{
    /// <summary>
    /// Ensures that only errors under a given severity will be posted. This is
    /// useful if you're calling a function to check validity and don't want error
    /// side effects, but also want to provide warnings, for instance.
    /// </summary>
    internal class LimitedSeverityErrorContainer : IErrorContainer
    {
        private readonly IErrorContainer errors;
        private readonly DocumentErrorSeverity maximumSeverity;

        public DocumentErrorSeverity DefaultSeverity => errors.DefaultSeverity;

        public LimitedSeverityErrorContainer(IErrorContainer errors, DocumentErrorSeverity maximumSeverity)
        {
            this.errors = errors;
            this.maximumSeverity = maximumSeverity;
        }

        public TexlError EnsureError(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (DefaultSeverity <= maximumSeverity)
            {
                return errors.EnsureError(node, errKey, args);
            }
            return null;
        }

        public TexlError EnsureError(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (severity <= maximumSeverity)
            {
                return errors.EnsureError(severity, node, errKey, args);
            }
            return null;
        }

        public TexlError Error(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (DefaultSeverity <= maximumSeverity)
            {
                return errors.Error(node, errKey, args);
            }
            return null;
        }

        public TexlError Error(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (severity <= maximumSeverity)
            {
                return errors.Error(severity, node, errKey, args);
            }
            return null;
        }

        public void Errors(TexlNode node, DType nodeType, KeyValuePair<string, DType> schemaDifference, DType schemaDifferenceType)
        {
            if (DocumentErrorSeverity.Severe <= maximumSeverity)
            {
                errors.Errors(node, nodeType, schemaDifference, schemaDifferenceType);
            }
        }
    }
}
