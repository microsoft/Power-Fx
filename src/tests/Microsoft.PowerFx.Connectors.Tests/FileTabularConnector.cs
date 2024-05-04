// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Connectors.Tabular;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PowerFx.Connectors.Tests
{
    public class FileTabularTests
    {
        private readonly ITestOutputHelper _output;

        public FileTabularTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task FileTabularTest()
        {
            string curDir = Directory.GetCurrentDirectory();
            string fileName = Path.Combine(curDir, "TestFile.txt");
            File.WriteAllLines(fileName, new string[] { "a", "b", "c" });

            FileTabularService tabularService = new FileTabularService(fileName);
            Assert.False(tabularService.IsInitialized);

            tabularService.Init();
            Assert.True(tabularService.IsInitialized);

            ConnectorTableValue fileTable = tabularService.GetTableValue();
            Assert.True(fileTable._tabularService.IsInitialized);

            // This one is not delegatable
            Assert.False(fileTable.IsDelegable);
            Assert.Equal("*[line:s]", fileTable.Type._type.ToString());

            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

#pragma warning disable CS0618 // Type or member is obsolete
            engine.EnableTabularConnectors();
#pragma warning restore CS0618 // Type or member is obsolete

            SymbolValues symbolValues = new SymbolValues().Add("File", fileTable);

            // Expression with tabular connector
            string expr = @"Last(FirstN(File, 2)).line";

            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);

            // Confirm that InjectServiceProviderFunction has properly been added
            string ir = new Regex("RuntimeValues_[0-9]+").Replace(check.PrintIR(), "RuntimeValues_XXX");
            Assert.Equal("FieldAccess(Last:![line:s](FirstN:*[line:s](InjectServiceProviderFunction:*[line:s](ResolvedObject('File:RuntimeValues_XXX')), Float:n(2:w))), line)", ir);

            // Use tabular connector. Internally we'll call ConnectorTableValueWithServiceProvider.GetRowsInternal to get the data
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, symbolValues).ConfigureAwait(false);
            StringValue str = Assert.IsType<StringValue>(result);
            Assert.Equal("b", str.Value);
        }
    }

    internal class FileTabularService : TabularService
    {
        private readonly string _fileName;

        public FileTabularService(string fileName)
        {
            _fileName = File.Exists(fileName) ? fileName : throw new FileNotFoundException($"File not found: {_fileName}");
        }

        public override bool IsDelegable => false;

        // Initialization can be synchronous
        public void Init()
        {
            SetTableType(RecordType.Empty().Add("line", FormulaType.String));
        }

        protected override async Task<IReadOnlyCollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, ODataParameters oDataParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] lines = await File.ReadAllLinesAsync(_fileName, cancellationToken).ConfigureAwait(false);
            return lines.Select(line => DValue<RecordValue>.Of(FormulaValue.NewRecordFromFields(new NamedValue("line", FormulaValue.New(line))))).ToArray();
        }
    }
}
