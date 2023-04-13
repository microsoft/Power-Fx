// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.App.ErrorContainers
{
    /// <summary>
    /// Ensures that only errors under a given severity will be posted. This is
    /// useful if you're calling a function to check validity and don't want error
    /// side effects, but also want to provide warnings, for instance.
    /// </summary>
    internal class LimitedSeverityErrorContainer : IErrorContainer
    {
        private readonly IErrorContainer _errors;
        private readonly DocumentErrorSeverity _maximumSeverity;

        public DocumentErrorSeverity DefaultSeverity => _errors.DefaultSeverity;

        public LimitedSeverityErrorContainer(IErrorContainer errors, DocumentErrorSeverity maximumSeverity)
        {
            _errors = errors;
            _maximumSeverity = maximumSeverity;
        }

        public TexlError EnsureError(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (DefaultSeverity <= _maximumSeverity)
            {
                return _errors.EnsureError(node, errKey, args);
            }

            return null;
        }

        public TexlError EnsureError(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (severity <= _maximumSeverity)
            {
                return _errors.EnsureError(severity, node, errKey, args);
            }

            return null;
        }

        public TexlError Error(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (DefaultSeverity <= _maximumSeverity)
            {
                return _errors.Error(node, errKey, args);
            }

            return null;
        }

        public TexlError Error(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (severity <= _maximumSeverity)
            {
                return _errors.Error(severity, node, errKey, args);
            }

            return null;
        }

        public void Errors(TexlNode node, DType nodeType, KeyValuePair<string, DType> schemaDifference, DType schemaDifferenceType)
        {
            if (_maximumSeverity >= DocumentErrorSeverity.Severe)
            {
                _errors.Errors(node, nodeType, schemaDifference, schemaDifferenceType);
            }
        }

        public TexlError EnsureError(DocumentErrorSeverity severity, Token token, ErrorResourceKey errKey, params object[] args)
        {
            if (severity <= _maximumSeverity)
            {
                return _errors.EnsureError(severity, token, errKey, args);
            }

            return null;
        }
    }
}
