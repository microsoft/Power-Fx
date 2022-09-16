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
        [InlineData("Date(2000,1,1) + Date(1999,1,1)")]
        [InlineData("Date(2000,1,1) + DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(20,1,1) - DateTimeValue(\"1 Jan 2015\")")]
        [InlineData("Time(20,1,1) - Date(2000,1,1)")]
        public void TexlDateOverloads_Negative(string script)
        {
            // TestBindingErrors(script, DType.Error);
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(script);
            
            Assert.Equal(DType.Error, result._binding.ResultType);            
            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + 5", "d")]
        [InlineData("Date(2000,1,1) + Time(2000,1,1)", "d")]
        [InlineData("Date(2000,1,1) + 5", "d")]
        [InlineData("Time(2000,1,1) + Date(2000,1,1)", "d")]
        [InlineData("Time(2000,1,1) + 5", "T")]
        [InlineData("5 + DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 + Date(2000,1,1)", "d")]
        [InlineData("5 + Time(2000,1,1)", "T")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Date(2000,1,1)", "n")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - 5", "d")]
        [InlineData("Date(2000,1,1) - DateTimeValue(\"1 Jan 2015\")", "n")]
        [InlineData("Date(2000,1,1) - Date(1999,1,1)", "n")]
        [InlineData("Time(2,1,1) - Time(2,1,1)", "n")]
        [InlineData("5 - DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("5 - Date(2000,1,1)", "d")]
        [InlineData("5 - Time(2000,1,1)", "T")]
        [InlineData("-Date(2001,1,1)", "D")]
        [InlineData("-Time(2,1,1)", "T")]
        [InlineData("-DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("Time(20,1,1) + Time(19,1,1)", "T")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") + Time(20,1,1)", "d")]
        [InlineData("Time(20,1,1) + DateTimeValue(\"1 Jan 2015\")", "d")]
        [InlineData("DateTimeValue(\"1 Jan 2015\") - Time(20,1,1)", "d")]
        [InlineData("DateValue(\"1 Jan 2015\") - Time(20,1,1)", "d")]
        public void TexlDateOverloads(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result._binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:d]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:d]")]
        [InlineData("DateAdd(Table({a:Date(2000,1,1)}),[3])", "*[a:d]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Result:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Result:d]")]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Result:n]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Result:n]")]
        public void TexlDateTableFunctions(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result._binding.ResultType);
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("DateAdd([Date(2000,1,1)],1)", "*[Value:d]")]
        [InlineData("DateAdd([Date(2000,1,1)],[3])", "*[Value:d]")]
        [InlineData("DateAdd(Table({a:Date(2000,1,1)}),[3])", "*[Value:d]")]
        [InlineData("DateAdd(Date(2000,1,1),[1])", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],1)", "*[Value:d]")]
        [InlineData("DateAdd([DateTimeValue(\"1 Jan 2015\")],[3])", "*[Value:d]")]
        [InlineData("DateAdd(DateTimeValue(\"1 Jan 2015\"),[1])", "*[Value:d]")]
        [InlineData("DateDiff([Date(2000,1,1)],[Date(2001,1,1)],\"years\")", "*[Value:n]")]
        [InlineData("DateDiff(Date(2000,1,1),[Date(2001,1,1)],\"years\")", "*[Value:n]")]
        [InlineData("DateDiff([Date(2000,1,1)],Date(2001,1,1),\"years\")", "*[Value:n]")]
        public void TexlDateTableFunctions_ConsistentOneColumnTableResult(string expression, string expectedType)
        {
            var engine = new Engine(new PowerFxConfig(Features.ConsistentOneColumnTableResult));
            var result = engine.Check(expression);

            Assert.True(DType.TryParse(expectedType, out var expectedDType));
            Assert.Equal(expectedDType, result._binding.ResultType);
            Assert.True(result.IsSuccess);
        }
    }
}
