// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class LazyTypeCustomeDisplayNameProviderTests
    {
        private class LazyRecordType : RecordType
        {
            public override IEnumerable<string> FieldNames { get; }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                type = name switch
                {
                    "Num" => FormulaType.Number,
                    "B" => FormulaType.Boolean,
                    "Nested" => FormulaType.Unknown,
                    "Inner" => FormulaType.Number,
                    _ => FormulaType.Blank
                };

                return type != FormulaType.Blank;
            }

            public LazyRecordType()
                : base(new CustomDisplayNameProvider())
            {
                FieldNames = new List<string>() { "Num", "B", "Nested", "Inner" };
            }

            public override bool Equals(object other)
            {
                return other is LazyRecordType;
            }

            public override int GetHashCode()
            {
                return 3;
            }

            private class CustomDisplayNameProvider : DisplayNameProvider
            {
                internal override ImmutableDictionary<DName, DName> LogicalToDisplayPairs => throw new NotImplementedException();

                internal override bool TryGetDisplayName(DName logicalName, out DName displayDName)
                {
                    var displayName = logicalName.Value switch
                    {
                        "Num" => "DisplayNum",
                        "B" => "DisplayB",
                        "Inner" => "InnerDisplay",
                        "Nested" => "NestedDisplay",
                        _ => null
                    };
                    displayDName = displayName == null ? default : new DName(displayName);
                    return displayName != null;
                }

                internal override bool TryGetLogicalName(DName displayName, out DName logicalDName)
                {
                    var logicalName = displayName.Value switch
                    {
                        "DisplayNum" => "Num",
                        "DisplayB" => "B",
                        "InnerDisplay" => "Inner",
                        "NestedDisplay" => "Nested",
                        _ => null
                    };
                    logicalDName = logicalName == null ? default : new DName(logicalName);
                    return logicalName != null;
                }

                internal override bool TryRemapLogicalAndDisplayNames(DName displayName, out DName logicalName, out DName newDisplayName)
                {
                    newDisplayName = displayName;
                    return TryGetLogicalName(displayName, out logicalName);
                }
            }
        }

        [Theory]
        [InlineData("If(B, Num, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("If(DisplayB, Num, 1234)", "If(DisplayB, DisplayNum, 1234)", true)]
        [InlineData("Sum(Nested, Inner)", "Sum(NestedDisplay, InnerDisplay)", true)]
        [InlineData("Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", "Sum(NestedDisplay /* The source */ , InnerDisplay /* Sum over the InnerDisplay column */)", true)]
        [InlineData("If(DisplayB, DisplayNum, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(B, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("If(DisplayB, Num, 1234)", "If(B, Num, 1234)", false)]
        [InlineData("Sum(NestedDisplay, InnerDisplay)", "Sum(Nested, Inner)", false)]
        [InlineData("Sum(NestedDisplay /* The source */ , InnerDisplay /* Sum over the InnerDisplay column */)", "Sum(Nested /* The source */ , Inner /* Sum over the InnerDisplay column */)", false)]
        public void ValidateCustomDNP(string inputExpression, string outputExpression, bool toDisplay)
        {
            var engine = new Engine(new PowerFxConfig(CultureInfo.InvariantCulture));

            var r1 = new LazyRecordType();
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
