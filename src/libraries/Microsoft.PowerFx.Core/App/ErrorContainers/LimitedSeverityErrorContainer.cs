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

        #region Record and Replay
        private readonly List<FrozenInvocation> _frozenInvocations = new List<FrozenInvocation>();

        private abstract class FrozenInvocation
        {
            public abstract void Invoke(IErrorContainer errors);
        }

        private sealed class EnsureErrorFrozenInvocation : FrozenInvocation
        {
            private readonly TexlNode _node;
            private readonly ErrorResourceKey _errKey;
            private readonly object[] _args;

            public EnsureErrorFrozenInvocation(TexlNode node, ErrorResourceKey errKey, params object[] args)
            {
                _node = node;
                _errKey = errKey;
                _args = args;
            }

            public override void Invoke(IErrorContainer errors)
            {
                errors.EnsureError(_node, _errKey, _args);
            }
        }

        private sealed class EnsureErrorSeverityFrozenInvocation : FrozenInvocation
        {
            private readonly DocumentErrorSeverity _severity;
            private readonly TexlNode _node;
            private readonly ErrorResourceKey _errKey;
            private readonly object[] _args;

            public EnsureErrorSeverityFrozenInvocation(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
            {
                _severity = severity;
                _node = node;
                _errKey = errKey;
                _args = args;
            }

            public override void Invoke(IErrorContainer errors)
            {
                errors.EnsureError(_severity, _node, _errKey, _args);
            }
        }

        private sealed class ErrorFrozenInvocation : FrozenInvocation
        {
            private readonly TexlNode _node;
            private readonly ErrorResourceKey _errKey;
            private readonly object[] _args;

            public ErrorFrozenInvocation(TexlNode node, ErrorResourceKey errKey, params object[] args)
            {
                _node = node;
                _errKey = errKey;
                _args = args;
            }

            public override void Invoke(IErrorContainer errors)
            {
                errors.Error(_node, _errKey, _args);
            }
        }

        private sealed class ErrorSeverityFrozenInvocation : FrozenInvocation
        {
            private readonly DocumentErrorSeverity _severity;
            private readonly TexlNode _node;
            private readonly ErrorResourceKey _errKey;
            private readonly object[] _args;

            public ErrorSeverityFrozenInvocation(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
            {
                _severity = severity;
                _node = node;
                _errKey = errKey;
                _args = args;
            }

            public override void Invoke(IErrorContainer errors)
            {
                errors.Error(_severity, _node, _errKey, _args);
            }
        }

        public void Undiscard()
        {
            foreach (var invocation in _frozenInvocations)
            {
                invocation.Invoke(_errors);
            }

            _frozenInvocations.Clear();
        }
        #endregion

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

            _frozenInvocations.Add(new EnsureErrorFrozenInvocation(node, errKey, args));
            return null;
        }

        public TexlError EnsureError(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (severity <= _maximumSeverity)
            {
                return _errors.EnsureError(severity, node, errKey, args);
            }

            _frozenInvocations.Add(new EnsureErrorSeverityFrozenInvocation(severity, node, errKey, args));
            return null;
        }

        public TexlError Error(TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (DefaultSeverity <= _maximumSeverity)
            {
                return _errors.Error(node, errKey, args);
            }

            _frozenInvocations.Add(new ErrorFrozenInvocation(node, errKey, args));
            return null;
        }

        public TexlError Error(DocumentErrorSeverity severity, TexlNode node, ErrorResourceKey errKey, params object[] args)
        {
            if (severity <= _maximumSeverity)
            {
                return _errors.Error(severity, node, errKey, args);
            }

            _frozenInvocations.Add(new ErrorSeverityFrozenInvocation(severity, node, errKey, args));
            return null;
        }

        public void Errors(TexlNode node, DType nodeType, KeyValuePair<string, DType> schemaDifference, DType schemaDifferenceType)
        {
            if (_maximumSeverity >= DocumentErrorSeverity.Severe)
            {
                _errors.Errors(node, nodeType, schemaDifference, schemaDifferenceType);
            }
        }
    }
}
