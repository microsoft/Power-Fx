// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Entities;
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

            // Lazy type
            Assert.Equal("r*", fileTable.Type._type.ToString());

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

            RecordType trt = fileTable.RecordType;
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

        // No need for files
        public override HttpClient HttpClient => null;

        internal override IReadOnlyDictionary<string, Relationship> Relationships => null;

        // Initialization can be synchronous
        public void Init()
        {            
            RecordType = new FileTabularRecordType(RecordType.Empty().Add("line", FormulaType.String));
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

    internal class FileTabularRecordType : RecordType
    {
        internal readonly RecordType _recordType;

        public FileTabularRecordType(RecordType recordType)
            : base(GetDisplayNameProvider(recordType), GetTableParameters())
        {
            _recordType = recordType;
        }

        private static TableParameters GetTableParameters()
        {
            return new TableParameters()
            {
                TableName = "FileTabular"
            };
        }

        private static DisplayNameProvider GetDisplayNameProvider(RecordType recordType) => DisplayNameProvider.New(recordType.FieldNames.Select(f => new KeyValuePair<Core.Utils.DName, Core.Utils.DName>(new Core.Utils.DName(f), new Core.Utils.DName(f))));        

        public override bool TryGetFieldType(string name, out FormulaType type)
        {
            return _recordType.TryGetFieldType(name, out type);
        }

        public override bool Equals(object other)
        {
            if (other == null || other is not FileTabularRecordType other2)
            {
                return false;
            }

            return _recordType == other2._recordType;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }       
    }
}
