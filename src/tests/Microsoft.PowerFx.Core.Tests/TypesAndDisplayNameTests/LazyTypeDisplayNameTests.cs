// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.TypesAndDisplayNameTests
{
    public class LazyTypeDisplayNameTests
    {        
        public class TestLazyRecordTypeWithDisplayNames : RecordType
        {
            public override IEnumerable<string> FieldNames { get; }

            public TestLazyRecordTypeWithDisplayNames()
                : base()
            {
                FieldNames = new List<string>() { "Foo", "Bar", "Baz" };
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                type = name switch
                {
                    "Foo" => FormulaType.Number,
                    "Bar" => FormulaType.DateTime,
                    "Baz" => FormulaType.Boolean,
                    _ => FormulaType.Blank
                };

                return type != FormulaType.Blank;
            }

            public override bool TryGetDisplayName(string logicalName, out string displayName)
            {                
                displayName = logicalName switch
                {
                    "Foo" => "FooDisplay",
                    "Bar" => "BarDisplay",
                    "Baz" => "BazDisplay",
                    _ => null
                };

                return displayName != null;
            }

            public override bool TryGetLogicalName(string displayName, out string logicalName)
            {
                logicalName = displayName switch
                {
                    "FooDisplay" => "Foo",
                    "BarDisplay" => "Bar",
                    "BazDisplay" => "Baz",
                    _ => null
                };

                return logicalName != null;
            }

            public override bool Equals(object other)
            {
                return other is TestLazyRecordTypeWithDisplayNames otherRecord;
            }

            public override int GetHashCode()
            {
                return 4;
            }
        }

        [Theory]
        [InlineData("If(Baz, Foo, 1234)", "If(BazDisplay, FooDisplay, 1234)", true)]
        [InlineData("If(BazDisplay, FooDisplay, 1234)", "If(BazDisplay, FooDisplay, 1234)", true)]
        [InlineData("If(BazDisplay, Foo, 1234)", "If(BazDisplay, FooDisplay, 1234)", true)]
        [InlineData("If(BazDisplay, FooDisplay, 1234)", "If(Baz, Foo, 1234)", false)]
        [InlineData("If(Baz, Foo, 1234)", "If(Baz, Foo, 1234)", false)]
        [InlineData("If(Baz, FooDisplay, 1234)", "If(Baz, Foo, 1234)", false)]
        public void ValidateDisplayNames(string inputExpression, string outputExpression, bool toDisplay)
        {
            var r1 = new TestLazyRecordTypeWithDisplayNames();
            var engine = new Engine(new PowerFxConfig(CultureInfo.InvariantCulture));
            
            var result = engine.Check(inputExpression, r1);
            Assert.True(result.IsSuccess);

            if (toDisplay)
            {
                var outDisplayExpression = engine.GetDisplayExpression(inputExpression, r1);
                Assert.Equal(outputExpression, outDisplayExpression);
            }
            else
            {
                var outInvariantExpression = engine.GetInvariantExpression(inputExpression, r1);
                Assert.Equal(outputExpression, outInvariantExpression);
            }
        }
    }
}
