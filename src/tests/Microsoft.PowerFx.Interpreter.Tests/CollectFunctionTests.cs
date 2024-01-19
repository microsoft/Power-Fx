// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Functions.FunctionArgValidators;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class CollectFunctionTests : PowerFxTest
    {
        private readonly ParserOptions _opts = new ParserOptions { AllowsSideEffects = true };

        [Theory]
        [InlineData("Collect(t, r1)", 1)]
        [InlineData("Collect(t, r1);Collect(t, r1);Collect(t, r1)", 3)]
        [InlineData("Collect(t, r1);Collect(t, Blank())", 1)]
        [InlineData("Collect(t, r1);Collect(t, {})", 2)]
        public async Task AppendCountTest(string script, int expected)
        {
            var engine = new RecalcEngine();
            var symbol = engine.Config.SymbolTable;

            var listT = new List<RecordValue>();

            symbol.EnableMutationFunctions();

            RecordValue r1 = FormulaValue.NewRecordFromFields(
                new NamedValue("Field1", FormulaValue.New(1)),
                new NamedValue("Field2", FormulaValue.New("Hello World!!!")));

            var t = FormulaValue.NewTable(r1.Type, listT);

            engine.UpdateVariable("t", t);
            symbol.AddConstant("r1", r1);

            var result = await engine.EvalAsync(script, CancellationToken.None, options: _opts, symbolTable: symbol).ConfigureAwait(false);
            var resultCount = await engine.EvalAsync("t", CancellationToken.None, options: _opts, symbolTable: symbol).ConfigureAwait(false);

            Assert.Equal(expected, ((TableValue)resultCount).Count());
        }

        [Theory]
        [InlineData("Collect(lazyTable, lazyRecord)", true)]
        [InlineData("Collect(lazyTable, lazyCoercibleRecord)", true)]
        [InlineData("Collect(lazyTable, lazyNotCoercibleRecord)", false)]
        [InlineData("Collect(lazyTable, {Value:1})", false)]
        [InlineData("Collect(lazyTable, lazyTable)", true)]
        [InlineData("Collect(lazyRecord, lazyRecord)", false)]
        [InlineData("Collect(lazyRecord, lazyTable)", false)]

        [InlineData("Patch(lazyTable, First(lazyTable), lazyRecord)", true)]
        [InlineData("Patch(lazyTable, First(lazyTable), lazyCoercibleRecord)", true)]
        [InlineData("Patch(lazyTable, First(lazyTable), lazyNotCoercibleRecord)", false)]
        [InlineData("Patch(lazyTable, First(lazyTable), {Value:1})", false)]
        [InlineData("Patch(lazyTable, First(lazyTable), lazyTable)", false)]
        [InlineData("Patch(lazyRecord, First(lazyTable), lazyRecord)", false)]
        [InlineData("Patch(lazyRecord, First(lazyTable), lazyTable)", false)]
        public void CheckMutationFunctionWithLazyTypesTest(string expr, bool isCheckSuccess)
        {
            var engine = new RecalcEngine();
            engine.Config.SymbolTable.EnableMutationFunctions();
            engine.Config.EnableSetFunction();
            var options = new ParserOptions() { AllowsSideEffects = true };
            
            var lazyRecordType = new CustomTypeRecordType("lazyRecord");

            // coercible type because both types have the same fields.
            var lazyCoercibleRecordType = new CustomTypeRecordType("lazyCoercibleRecordType");

            // not coercible type because both this will have more fields compare to source table.
            var lazyNotCoercibleRecordType = new CustomTypeRecordType("lazyNotCoercibleRecord").Add(new NamedFormulaType("f1", FormulaType.String));

            engine.Config.SymbolTable.AddVariable("lazyRecord", lazyRecordType, true);
            engine.Config.SymbolTable.AddVariable("lazyTable", lazyRecordType.ToTable(), true);
            engine.Config.SymbolTable.AddVariable("lazyCoercibleRecord", lazyCoercibleRecordType, true);
            engine.Config.SymbolTable.AddVariable("lazyNotCoercibleRecord", lazyNotCoercibleRecordType, true);

            var result = engine.Check(expr, options);

            Assert.Equal(isCheckSuccess, result.IsSuccess);
        }

        public class CustomTypeRecordType : RecordType
        {
            public readonly string TypeName;
            private readonly IDictionary<string, FormulaType> _fieldTypes = new Dictionary<string, FormulaType>();

            public CustomTypeRecordType(string typeName)
            {
                TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            }

            public override IEnumerable<string> FieldNames => _fieldTypes.Select(field => field.Key);

            #region Method overrides

            public override RecordType Add(NamedFormulaType field)
            {
                _fieldTypes[field.Name] = field.Type;
                return this;
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                return _fieldTypes.TryGetValue(name, out type);
            }

            public override bool Equals(object other)
            {
                return (other is CustomTypeRecordType otherType) && TypeName == otherType.TypeName;
            }

            public override int GetHashCode()
            {
                return TypeName.GetHashCode();
            }

            #endregion
        }
    }
}
