// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Microsoft.PowerFx.Repl.Tests
{
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

    public class StringWithSourceTests
    {
        private class Poco1
        {
            public StringWithSource Prop { get; set; }
        }

        private class Poco1Direct
        {
            public string Prop { get; set; }
        }

        // common fake filename used in locations. 
        private const string Filename = "myfile1.txt";

        private T Parse<T>(string contents)
        {
            // Deserialize.
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new StringWithSourceConverter(Filename, contents))
                .Build();

            var poco = deserializer.Deserialize<T>(contents);

            return poco;
        }

        private T ParseExpectError<T>(string contents, string expectedError)
        {
            try
            {
                // Deserialize.
                var deserializer = new DeserializerBuilder()
                    .WithTypeConverter(new StringWithSourceConverter(Filename, contents))
                    .Build();

                var poco = deserializer.Deserialize<T>(contents);

                Assert.Fail($"Expected error: {expectedError}");
                return poco;
            }
            catch (YamlException e)
            {
                // Exceptions from parser will be wrapped in YamlException.
                var msg = e?.InnerException?.Message ?? e.Message;
                Assert.Contains(expectedError, msg);
            }

            return default;
        }

        // In all cases, parse as 'string' should succeed. 
        // But Parse with SourceLocaiton has more limitations.
        //  expectedError specifies restrictions. 
        private StringWithSource Test(string contents, string expectedError = null)
        {
            // Should always work with 'string'
            var p1 = Parse<Poco1Direct>(contents);

            if (expectedError != null)
            {
                ParseExpectError<Poco1>(contents, expectedError);
                return null;
            }
            else
            {
                var p2 = Parse<Poco1>(contents);

                Assert.Equal(p1.Prop, p2.Prop.Value);
                Assert.Same(Filename, p2.Prop.Location.Filename);

                if (p2.Prop.Value != null)
                {
                    p2.Prop.Value = p2.Prop.Value.Replace("\r", string.Empty);
                }

                return p2.Prop;
            }
        }

        [Theory]
        [InlineData(
@"Prop: |
  123
", 2, 3, "123\n")]

        // Leading space in front of property. 
        [InlineData(
@" Prop: |
  123
", 2, 3, "123\n")]

        [InlineData(
@"# another line 
Prop: |
     123
     456", 3, 6, "123\n456")]

        [InlineData(
@"Prop: |-
  123
", 2, 3, "123")]

        [InlineData(
@"Prop: |-
  true
", 2, 3, "true")] // YDN will still return as string. 

        // Other encodings (not | ) are not supported. 
        [InlineData(
@"Prop: >
  123
  456
", 0, 0, "123456\n", "literal")] // Folding 

        [InlineData(
@"Prop: 123456
", 0, 0, "123456", "literal")] // plain

        [InlineData(
@"Prop: ""123456""
", 0, 0, "123456", "literal")] // quotes

        [InlineData(
@"Prop: ", 1, 6, null)] // empty 

        public void Test1(string contents, int lineStart, int colStart, string value, string errorMessage = null)
        {
            var p1 = Test(contents, errorMessage);

            if (errorMessage == null)
            {
                Assert.Equal(value, p1.Value);
                Assert.Equal(colStart, p1.Location.ColStart);
                Assert.Equal(lineStart, p1.Location.LineStart);
            }
            else
            {
                Assert.Null(p1);
            }
        }

        // Error conditions. 
        [Theory]
        [InlineData(
"Prop: { }")] // object , expecting string. 
        public void TestError(string contents)
        {
            ParseExpectError<Poco1Direct>(contents, "Failed");
            ParseExpectError<Poco1>(contents, "Expected 'Scalar'");
        }
    }
}
