// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Lexer.Tokens;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
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
    }
}
