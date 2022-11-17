// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class ExpressionErrorTests
    {
        [Fact]
        public void TestError()
        {
            var error = new ExpressionError()
            {
                Message = "ouch",
                Span = new Span(2, 5)
            };

            // Verify defaults for non-nullable objects
            Assert.False(error.IsWarning);
            Assert.Equal(ErrorKind.None, error.Kind);

            Assert.Equal("Error 2-5: ouch", error.ToString());
        }

        [Fact]
        public void TestWarning()
        {
            var error = new ExpressionError()
            {
                Message = "ouch",
                Span = new Span(2, 5),
                Severity = ErrorSeverity.Warning
            };

            // Verify defaults for non-nullable objects
            Assert.True(error.IsWarning);
            Assert.Equal(ErrorKind.None, error.Kind);

            Assert.Equal("Warning 2-5: ouch", error.ToString());
        }

        [Fact]
        public void Empty()
        {            
            // We don't want null IEnumerables. 
            // Null gets normalized to empty.
            var internalErrors = (IEnumerable<IDocumentError>)null;
            var errors = ExpressionError.New(internalErrors);

            Assert.Empty(errors);
        }

        [Fact]
        public void CompatTest()
        {
            var span = new Span(2, 5);
            var e = new MyError(null, null, DocumentErrorKind.Persistence, DocumentErrorSeverity.Critical, TexlStrings.ErrBadArity, span, null, "arg1");
            Assert.Equal(DocumentErrorSeverity.Critical, e.Severity);

            var e2 = new MyError(null, null, DocumentErrorKind.Persistence, DocumentErrorSeverity.Warning, TexlStrings.ErrBadArity, "arg1");
            Assert.Equal(DocumentErrorSeverity.Warning, e2.Severity);
        }

#pragma warning disable CS0618 // Type or member is obsolete

        // Back compat test signatures used by PA-client 
        // Obsolete, but PAClient still uses them. 
        private class MyError : BaseError
        {
            public MyError(IDocumentError innerError, Exception internalException, DocumentErrorKind kind, DocumentErrorSeverity severity, ErrorResourceKey errKey, Span textSpan, IEnumerable<string> sinkTypeErrors, params object[] args)
                : base(innerError, internalException, kind, severity, errKey, textSpan, sinkTypeErrors, args)
            {
            }

            public MyError(IDocumentError innerError, Exception internalException, DocumentErrorKind kind, DocumentErrorSeverity severity, ErrorResourceKey errKey, params object[] args)
                : base(innerError, internalException, kind, severity, errKey, args)
            {
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
