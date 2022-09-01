// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{    
    public class TexlTests : PowerFxTest
    {
        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Date(2000,1,1)")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Time(2000,1,1)")]
        [InlineData("Date(2000,1,1) + Date(1999,1,1)")]
        [InlineData("Date(2000,1,1) + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(2000,1,1) + Time(1999,1,1)")]
        [InlineData("Time(2000,1,1) + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Time(2000,1,1)")]
        [InlineData("DateValue(\"1 Jan 2015\") - Time(2000,1,1)")]
        [InlineData("Time(2000,1,1) - DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(2000,1,1) - Date(2000,1,1)")]
        public void TexlDateOverloads_Negative(string script)
        {
            // TestBindingErrors(script, DType.Error);
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(script);
            
            Assert.Equal(DType.Error, result._binding.ResultType);            
            Assert.False(result.IsSuccess);
        }
    }
}
