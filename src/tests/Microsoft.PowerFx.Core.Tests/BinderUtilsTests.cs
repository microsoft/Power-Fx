// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

extern alias PfxCore;

using System;
using System.Collections.Generic;
using System.Linq;
using PfxCore.Microsoft.PowerFx;
using PfxCore.Microsoft.PowerFx.Core.Binding;
using PfxCore.Microsoft.PowerFx.Core.Functions;
using PfxCore.Microsoft.PowerFx.Core.Localization;
using PfxCore.Microsoft.PowerFx.Core.Types;
using PfxCore.Microsoft.PowerFx.Core.Utils;
using PfxCore.Microsoft.PowerFx.Syntax;
using PfxCore.Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class BinderUtilTests : PowerFxTest
    {
        [Theory]
        [InlineData("ThisRecord.Field2", "ThisRecord.Field2")]
        [InlineData("ThisRecord.Related.Field", "ThisRecord.Related.new_field")]
        [InlineData("Related.Field", "new_rel1.Field")]
        [InlineData("First(Table).Field2", "First.new_field2")]
        [InlineData("First(Table).Related.Field", "First.new_rel1.new_field")]
        [InlineData("Testone().Field2", "Testone.new_field2")]
        [InlineData("Testone().Related.Field", "Testone.new_rel1.new_field")]
        [InlineData("Namespace.Testtwo().Field2", "Testtwo.new_field2")]
        [InlineData("Namespace.Testtwo().Related.Field", "Testtwo.new_rel1.new_field")]
        public void TestTryConvertNodeToDPath(string formula, string expected)
        {
            // Simulate symbols like dataverse. 
            var r1 = RecordType.Empty()
              .Add(new NamedFormulaType("new_field", FormulaType.Number, "Field"));

            // Simulate symbols like dataverse. 
            var r2 = RecordType.Empty()
              .Add(new NamedFormulaType("new_field2", FormulaType.Number, "Field2"))
              .Add(new NamedFormulaType("new_rel1", r1, "Related"));

            var f1 = new TestTexlFunction("Testone", r2._type);
            var f2 = new TestTexlFunction("Testtwo", r2._type, DPath.Root.Append(new DName("Namespace")));

            var rowScopeSymbols = ReadOnlySymbolTable.NewFromRecord(r2, allowThisRecord: true);

            var globalSymbols = new SymbolTable { DebugName = "Globals" };
            var tableType = r2.ToTable();
            globalSymbols.AddVariable("crf_table", tableType, displayName: "Table");
            globalSymbols.AddFunction(f1);
            globalSymbols.AddFunction(f2);

            var allSymbols = ReadOnlySymbolTable.Compose(rowScopeSymbols, globalSymbols);

            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var parseResult = engine.Parse(formula);
            Assert.True(parseResult.IsSuccess);

            var checkResult = new CheckResult(engine)
                .SetText(parseResult)
                .SetBindingInfo(allSymbols);
            Assert.True(checkResult.IsSuccess);

            (var binding, _) = engine.ComputeBinding(checkResult);

            if (parseResult.Root is DottedNameNode dottedNameNode &&
                BinderUtils.TryConvertNodeToDPath(binding, dottedNameNode, out DPath result))
            {
                Assert.Equal(expected, result.ToString());
                return;
            }

            Assert.True(false);
        }

        private class TestTexlFunction : TexlFunction
        {
            private readonly int _requiredEnums = 0;

            public TestTexlFunction(string name, DType type = null, DPath? path = null)
                : base(path ?? DPath.Root, name, name, (string locale) => name, FunctionCategories.REST, type ?? DType.String, 0, 1, 1, type ?? DType.String)
            {
            }

            public override bool IsSelfContained => true;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<string> GetRequiredEnumNames()
            {
                return _requiredEnums == 0
                    ? Enumerable.Empty<string>()
                    : Enumerable.Range(0, _requiredEnums).Select(n => $"Enum{n}");
            }
        }
    }
}
