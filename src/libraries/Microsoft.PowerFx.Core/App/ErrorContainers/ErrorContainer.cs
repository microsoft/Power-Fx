// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.App.ErrorContainers
{
    internal class ErrorContainer : IErrorContainer
    {
        private List<TexlError> _errors;

        public DocumentErrorSeverity DefaultSeverity => DocumentErrorSeverity.Critical;

        public void MergeErrors(IEnumerable<TexlError> errors)
        {
            if (_errors == null)
            {
                _errors = new List<TexlError>();
            }

            errors = errors.Where(e => !HasErrors(e.Node, e.Severity)).ToList();

            _errors.AddRange(errors);
        }

        public bool HasErrors()
        {
            return CollectionUtils.Size(_errors) > 0;
        }

        public bool HasErrors(TexlNode node, DocumentErrorSeverity severity = DocumentErrorSeverity.Suggestion)
        {
            Contracts.AssertValue(node);

            if (CollectionUtils.Size(_errors) == 0)
            {
                return false;
            }

            foreach (var err in _errors)
            {
                if (err.Node == node && err.Severity >= severity)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasErrors(Token token, DocumentErrorSeverity severity = DocumentErrorSeverity.Suggestion)
        {
            Contracts.AssertValue(token);

            if (CollectionUtils.Size(_errors) == 0)
            {
                return false;
            }

            foreach (var err in _errors)
            {
                if (err.Tok == token && err.Severity >= severity)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasErrorsInTree(TexlNode rootNode, DocumentErrorSeverity severity = DocumentErrorSeverity.Suggestion)
        {
            Contracts.AssertValue(rootNode);

            if (CollectionUtils.Size(_errors) == 0)
            {
                return false;
            }

            foreach (var err in _errors)
            {
                if (err.Node.InTree(rootNode) && err.Severity >= severity)
                {
                    return true;
                }
            }

            return false;
        }

        public bool GetErrors(ref List<TexlError> rgerr)
        {
            if (CollectionUtils.Size(_errors) == 0)
            {
                return false;
            }

            CollectionUtils.Add(ref rgerr, _errors);
            return true;
        }

        public IEnumerable<TexlError> GetErrors()
        {
            if (_errors != null)
            {
                foreach (var err in _errors)
                {
                    yield return err;
                }
            }
        }

        public TexlError EnsureError(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            return EnsureError(DefaultSeverity, node, errKey, args);
        }

        public TexlError EnsureError(DocumentErrorSeverity severity, Token token, ErrorResourceKey errKey, params object[] args)
        {
            Contracts.AssertValue(token);
            Contracts.AssertValue(args);

            if (!HasErrors(token, severity))
            {
                return Error(severity, token, errKey, args);
            }

            return null;
        }

        public TexlError Error(DocumentErrorSeverity severity, Token token, ErrorResourceKey errKey, object[] args)
        {
            Contracts.AssertValue(token);
            Contracts.AssertValue(args);

            var err = new TexlError(token, severity, errKey, args);
            CollectionUtils.Add(ref _errors, err);
            return err;
        }

        public TexlError EnsureError(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(args);

            if (!HasErrors(node, severity))
            {
                return Error(severity, node, errKey, args);
            }

            return null;
        }

        public TexlError Error(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            return Error(DefaultSeverity, node, errKey, args);
        }

        public TexlError Error(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValue(args);

            var err = new TexlError(node, severity, errKey, args);
            CollectionUtils.Add(ref _errors, err);
            return err;
        }

        public void Errors(TexlNode node, DType nodeType, KeyValuePair<string, DType> schemaDifference, DType schemaDifferenceType)
        {
            Contracts.AssertValue(node);
            Contracts.AssertValid(nodeType);

            Error(DocumentErrorSeverity.Severe, node, TexlStrings.ErrBadSchema_ExpectedType, nodeType.GetKindString());

            // If there's no schema difference, this was just an invalid type.
            if (string.IsNullOrEmpty(schemaDifference.Key))
            {
                return;
            }

            if (schemaDifferenceType.IsValid)
            {
                Error(
                    DocumentErrorSeverity.Severe,
                    node,
                    TexlStrings.ErrColumnTypeMismatch_ColName_ExpectedType_ActualType,
                    schemaDifference.Key,
                    schemaDifference.Value.GetKindString(),
                    schemaDifferenceType.GetKindString());
            }
            else
            {
                Error(
                    DocumentErrorSeverity.Severe,
                    node,
                    TexlStrings.ErrColumnMissing_ColName_ExpectedType,
                    schemaDifference.Key,
                    schemaDifference.Value.GetKindString());
            }
        }
    }
}
