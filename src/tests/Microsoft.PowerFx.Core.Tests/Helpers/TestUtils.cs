// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.Helpers
{
    internal class TestUtils
    {        
        // Parse a type in string form to a DType
        public static DType DT(string type)
        {
            Assert.True(DType.TryParse(type, out DType dtype));
            Assert.True(dtype.IsValid);
            return dtype;
        }

        public static void AssertJsonEqual(string expected, string actual)
        {
            using var expectedDom = JsonDocument.Parse(expected);
            using var actualDom = JsonDocument.Parse(actual);
            AssertJsonEqual(expectedDom.RootElement, actualDom.RootElement);
        }

        public static void AssertJsonEqual(JsonElement expected, JsonElement actual)
        {
            AssertJsonEqualCore(expected, actual, new ());
        }

        private static void AssertJsonEqualCore(JsonElement expected, JsonElement actual, Stack<object> path)
        {
            JsonValueKind valueKind = expected.ValueKind;
            Assert.True(valueKind == actual.ValueKind);

            switch (valueKind)
            {
                case JsonValueKind.Object:
                    var expectedProperties = new List<string>();
                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        expectedProperties.Add(property.Name);
                    }

                    var actualProperties = new List<string>();
                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        actualProperties.Add(property.Name);
                    }

                    foreach (var property in expectedProperties.Except(actualProperties))
                    {
                        Assert.True(false, $"Property \"{property}\" missing from actual object.");
                    }

                    foreach (var property in actualProperties.Except(expectedProperties))
                    {
                        Assert.True(false, $"Actual object defines additional property \"{property}\".");
                    }

                    foreach (var name in expectedProperties)
                    {
                        path.Push(name);
                        AssertJsonEqualCore(expected.GetProperty(name), actual.GetProperty(name), path);
                        path.Pop();
                    }

                    break;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = expected.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = actual.EnumerateArray();

                    var i = 0;
                    while (expectedEnumerator.MoveNext())
                    {
                        Assert.True(actualEnumerator.MoveNext(), "Actual array contains fewer elements.");
                        path.Push(i++);
                        AssertJsonEqualCore(expectedEnumerator.Current, actualEnumerator.Current, path);
                        path.Pop();
                    }

                    Assert.False(actualEnumerator.MoveNext(), "Actual array contains additional elements.");
                    break;
                case JsonValueKind.String:
                    Assert.Equal(expected.GetString(), actual.GetString());
                    break;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    Assert.Equal(expected.GetRawText(), actual.GetRawText());
                    break;
                default:
                    Assert.True(false, $"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
                    break;
            }
        }
    }
}
