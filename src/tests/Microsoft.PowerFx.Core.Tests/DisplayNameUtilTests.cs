// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DisplayNameUtilTests : PowerFxTest
    {
        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "a", "b", "c" }, new string[] { "a", "b", "c" })] // Same logical/display
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "d", "e", "f" }, new string[] { "d", "e", "f" })] // Distinct logical/display
        [InlineData(new string[] { "a", "a", "a" }, new string[] { "d", "e", "f" }, null)] // Colliding logical names
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "d", "d", "d" }, new string[] { "d (a)", "d (b)", "d (c)" })] // Colliding display names
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "d", "e", "a" }, new string[] { "d", "e", "a (c)" })] // Logical -> display collision
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "c", "e", "f" }, new string[] { "c (a)", "e", "f" })] // Logical -> display collision
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "d", "", "f" }, new string[] { "d", "b", "f" })] // Empty display name
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "c", "c", "c (a)" }, new string[] { "c (a) (a)", "c (b)", "c (a) (c)" })] // Collision of collisions
        [InlineData(new string[] { "      ", "b", "c" }, new string[] { "b", "e", "b (a)" }, null)] // Invalid logical name
        public void MakeUniqueTests(string[] logicalNames, string[] inputDisplayNames, string[] expectedDisplayNames)
        {
            var input = logicalNames.Zip(inputDisplayNames, (logical, display) => new KeyValuePair<string, string>(logical, display));

            if (expectedDisplayNames == null) 
            {
                Assert.Throws<ArgumentException>(() => DisplayNameUtility.MakeUnique(input));
            }
            else
            {
                var result = DisplayNameUtility.MakeUnique(input);
                var expectedResult = logicalNames.Zip(expectedDisplayNames, (logical, display) => new KeyValuePair<DName, DName>(new DName(logical), new DName(display)));

                Assert.Equal(expectedResult.OrderBy(kvp => kvp.Key.Value), result.OrderBy(kvp => kvp.Key.Value));
            }
        }
    }
}
