// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class PrimitiveMarshallerProviderTests
    {
        private static readonly TypeMarshallerCache _cache = new TypeMarshallerCache();

        // Test how .net types marshal to FormulaType
        [Theory]
        [InlineData(typeof(double), typeof(NumberType))]
        [InlineData(typeof(int), typeof(NumberType))]
        [InlineData(typeof(decimal), typeof(NumberType))]
        [InlineData(typeof(long), typeof(NumberType))]
        [InlineData(typeof(float), typeof(NumberType))]
        [InlineData(typeof(Guid), typeof(GuidType))]
        [InlineData(typeof(bool), typeof(BooleanType))]
        [InlineData(typeof(DateTime), typeof(DateTimeType))]
        [InlineData(typeof(DateTimeOffset), typeof(DateTimeType))]
        [InlineData(typeof(TimeSpan), typeof(TimeType))]
        [InlineData(typeof(string), typeof(StringType))]
        [InlineData(typeof(object), null)] // no match
        public void TestAll(Type dotnetType, Type fxType)
        {            
            var provder = new PrimitiveMarshallerProvider();
            var result = provder.TryGetMarshaller(dotnetType, _cache, 1, out var tm);

            if (fxType == null)
            {
                Assert.False(result);
                Assert.Null(tm);
            }
            else
            {
                Assert.True(result);

                Assert.NotNull(tm);

                var actualFxType = tm.Type;
                Assert.True(actualFxType.GetType() == fxType);

                var value = GetValue(dotnetType);
                var fxValue = tm.Marshal(value);
                Assert.Equal(fxType, fxValue.Type.GetType());
            }
        }

        // Get an instance of the object we can test against the marhsaller. 
        private static object GetValue(Type t)
        {
            if (t == typeof(string))
            {
                return string.Empty;
            }

            if (t == typeof(Guid))
            {
                return Guid.Empty;
            }

            return Activator.CreateInstance(t);
        }        
    }
}
