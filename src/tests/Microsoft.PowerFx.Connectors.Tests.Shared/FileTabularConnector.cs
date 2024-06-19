// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

            CdpTableValue fileTable = tabularService.GetTableValue();
            Assert.True(fileTable._tabularService.IsInitialized);

            // This one is not delegatable
            Assert.False(fileTable.IsDelegable);
            Assert.Equal("*[line:s]", fileTable.Type._type.ToString());

            PowerFxConfig config = new PowerFxConfig(Features.PowerFxV1);
            RecalcEngine engine = new RecalcEngine(config);

            SymbolValues symbolValues = new SymbolValues().Add("File", fileTable);

            // Expression with tabular connector
            string expr = @"Last(FirstN(File, 2)).line";

            CheckResult check = engine.Check(expr, options: new ParserOptions() { AllowsSideEffects = true }, symbolTable: symbolValues.SymbolTable);
            Assert.True(check.IsSuccess);
            
            // Use tabular connector. Internally we'll call CdpTableValue.GetRowsInternal to get the data
            FormulaValue result = await check.GetEvaluator().EvalAsync(CancellationToken.None, symbolValues);
            StringValue str = Assert.IsType<StringValue>(result);
            Assert.Equal("b", str.Value);

            RecordType trt = fileTable.TabularRecordType;
            Assert.NotNull(trt);
        }
    }

    internal class FileTabularService : CdpService
    {
        private readonly string _fileName;

        public FileTabularService(string fileName)
        {
            _fileName = File.Exists(fileName) ? fileName : throw new FileNotFoundException($"File not found: {_fileName}");
        }

        public override bool IsDelegable => false;

        public override ConnectorType ConnectorType => null;

        // No need for files
        public override HttpClient HttpClient => null;

        // Initialization can be synchronous
        public void Init()
        {            
            SetRecordType(RecordType.Empty().Add("line", FormulaType.String));
        }

        protected override async Task<IReadOnlyCollection<DValue<RecordValue>>> GetItemsInternalAsync(IServiceProvider serviceProvider, ODataParameters oDataParameters, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

#if !NET462
            string[] lines = await File.ReadAllLinesAsync(_fileName, cancellationToken);
#else
            string[] lines = File.ReadAllLines(_fileName);
#endif

            return lines.Select(line => DValue<RecordValue>.Of(FormulaValue.NewRecordFromFields(new NamedValue("line", FormulaValue.New(line))))).ToArray();
        }
    }
}
