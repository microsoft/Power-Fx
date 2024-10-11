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
    public class FileLocationTests
    {
        [Theory]
        [InlineData(0, 200, 300)]
        [InlineData(1, 200, 301)]
        [InlineData(4, 201, 300)]
        [InlineData(9, 202, 301)]
        public void Apply(int min, int expectedLine, int expectedCol)
        {
            string file =

 // 012 345 6 789
   "ABC\nDE\r\nFG";

            var loc = new FileLocation
            {
                Filename = "test",
                LineStart = 200,
                ColStart = 300
            };

            var span = new Span(min, min + 1);
            var loc2 = loc.Apply(file, span);

            Assert.Same(loc.Filename, loc2.Filename);
            Assert.Equal(expectedLine, loc2.LineStart);
            Assert.Equal(expectedCol, loc2.ColStart);
        }
    }
}
