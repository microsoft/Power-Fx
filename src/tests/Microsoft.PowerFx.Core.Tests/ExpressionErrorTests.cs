// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public void NullSpanToString()
        {
            var error = new ExpressionError()
            {
                Message = "ouch",
                Severity = ErrorSeverity.Critical
            };

            Assert.Equal("Error: ouch", error.ToString());
        }

        [Fact]
        public void Defaults()
        {
            var error = new ExpressionError();

            Assert.Equal(ErrorSeverity.Severe, error.Severity);
            Assert.Equal(ErrorKind.None, error.Kind);
            Assert.Null(error.Message);
            Assert.Null(error.MessageKey);
            Assert.Null(error.MessageLocale);
            Assert.Null(error.Span);
        }

        private static IDocumentError GetBaseError(ErrorResourceKey errKey, params object[] args)
        {
            var span = new Span(2, 5);
            var e = new MyError(null, null, DocumentErrorKind.Persistence, DocumentErrorSeverity.Critical, errKey, span, null, args);

            return e;
        }

        private static ExpressionError GetError(ErrorResourceKey errKey, params object[] args)
        {
            var e = GetBaseError(errKey, args);
            var error = ExpressionError.New(e);

            Assert.Equal(error.MessageKey, errKey.Key);

            return error;
        }

        [Fact]
        public void Localize()
        {
            var error = GetError(TexlStrings.ErrInvalidName, "name");

            Assert.Equal("Name isn't valid. 'name' isn't recognized.", error.Message);

            var cultureFr = new CultureInfo("fr-FR");
            var errorFr = error.GetInLocale(cultureFr);
            Assert.Same(cultureFr, errorFr.MessageLocale);
            
            Assert.NotSame(error, errorFr);
            Assert.Equal("Name isn't valid. 'name' isn't recognized.", error.Message);
            Assert.Equal("Le nom n’est pas valide. « name » n’est pas reconnu.", errorFr.Message);

            var error3 = error.GetInLocale(null);            
            Assert.Equal("Name isn't valid. 'name' isn't recognized.", error3.Message);
            Assert.Null(error3.MessageLocale);

            var cultureBg = new CultureInfo("bg-BG");
            var errorBg = errorFr.GetInLocale(cultureBg);
            Assert.Equal("Името не е валидно. „name“ не е разпознато.", errorBg.Message);
        }

        // Can create an error with a default culture. 
        [Fact]
        public void LocalizeDefault()
        {
            var cultureFr = new CultureInfo("fr-FR");

            var baseError = GetBaseError(TexlStrings.ErrInvalidName, "name");
            var errorFr = ExpressionError.New(baseError, cultureFr);

            Assert.Equal("Le nom n’est pas valide. « name » n’est pas reconnu.", errorFr.Message);
        }

        // If we don't have a MessageKey, then current culture is ignored 
        [Fact]
        public void LocalizeIgnoreCulture()
        {
            var error = new ExpressionError()
            {
                Message = "ouch",
                Severity = ErrorSeverity.Critical
            };
            Assert.Null(error.MessageKey);

            var cultureFr = new CultureInfo("fr-FR");
            var error2 = error.GetInLocale(cultureFr);

            Assert.Same(error2, error);
            Assert.Equal("ouch", error2.Message);
        }

        [Fact]
        public void New()
        {
            var baseError = GetBaseError(TexlStrings.ErrInvalidName, "name");
            var baseErrors = new IDocumentError[] { baseError };

            var errors = ExpressionError.New(baseErrors);

            Assert.Single(errors);
            var error = errors.First();
            Assert.Equal("Name isn't valid. 'name' isn't recognized.", error.Message);

            // Overload with Culture
            var cultureFr = new CultureInfo("fr-FR");
            var errors2 = ExpressionError.New(baseErrors, cultureFr);
            Assert.Single(errors);
            var errorFr = errors2.First();
            Assert.Equal("Le nom n’est pas valide. « name » n’est pas reconnu.", errorFr.Message);
            Assert.Same(cultureFr, errorFr.MessageLocale);
        }

        [Fact]
        public void Empty()
        {            
            // We don't want null IEnumerables. 
            // Null gets normalized to empty.
            var internalErrors = (IEnumerable<IDocumentError>)null;
            var errors = ExpressionError.New(internalErrors);

            Assert.Empty(errors);

            var errors2 = ExpressionError.New(internalErrors, CultureInfo.InvariantCulture);
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
