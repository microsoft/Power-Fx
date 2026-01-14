// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests.Scenarios
{
    // Demonstrate creative use of symbol tables to make a reflection function with a dynamic return type
    public class ScenarioSymbolTables : PowerFxTest
    {
        [Theory]
        [InlineData("FindRecord(\"Customer\")", typeof(CustomerTable))]
        [InlineData("FindRecord(\"Vendor\")", typeof(VendorTable))]
        public void DynamicRecordTypeChecking(string expr, Type expectedType)
        {
            var typeCache = new TypeMarshallerCache();

            var config = new PowerFxConfig();
            config.AddFunction(new FindRecord(typeCache));

            var engine = new RecalcEngine(config);

            var parseResult = engine.Parse(expr);
            var findRecords = new FindRecordVisitor();
            parseResult.Root.Accept(findRecords);

            var symbolTable = new SymbolTable();
            foreach (var tableName in findRecords.RecordTypes)
            {
                var record = BaseRecord.CreateRecord(tableName);
                var recordValue = typeCache.Marshal(record, record.GetType()) as RecordValue;

                symbolTable.AddFunction(new FindRecord(typeCache, recordValue.Type));
            }

            var checkResult = engine.Check(parseResult, options: null, symbolTable);

            Assert.True(checkResult.IsSuccess);

            var resultValue = checkResult.GetEvaluator().Eval() as ObjectRecordValue;

            Assert.NotNull(resultValue);
            Assert.IsType(expectedType, resultValue.Source);
        }

        [Theory]
        [InlineData("FindRecord(\"Customer\").CustomerId")]
        [InlineData("FindRecord(\"Vendor\").VendorId")]
        [InlineData("FindRecord(\"Vendor\").VendorId; FindRecord(\"Customer\").CustomerId;", Skip = "Only first entry in symbol table is used")]
        public void DynamicRecordTypeDottedChecking(string expr)
        {
            var typeCache = new TypeMarshallerCache();
            var parserOptions = new ParserOptions() { AllowsSideEffects = true };

            var config = new PowerFxConfig();
            config.AddFunction(new FindRecord(typeCache));

            var engine = new RecalcEngine(config);

            var parseResult = engine.Parse(expr, parserOptions);
            var findRecords = new FindRecordVisitor();
            parseResult.Root.Accept(findRecords);

            var symbolTable = new SymbolTable();
            foreach (var tableName in findRecords.RecordTypes)
            {
                var record = BaseRecord.CreateRecord(tableName);
                var recordValue = typeCache.Marshal(record, record.GetType()) as RecordValue;

                symbolTable.AddFunction(new FindRecord(typeCache, recordValue.Type));
            }

            var checkResult = engine.Check(parseResult, parserOptions, symbolTable);

            Assert.True(checkResult.IsSuccess);
        }

        private class BaseRecord
        {
            public string TableName { get; set; }

            public long RecordId { get; set; }

            public static BaseRecord CreateRecord(string tableName)
            {
                BaseRecord record = null;

                switch (tableName)
                {
                    case "Customer":
                        record = new CustomerTable();
                        break;
                    case "Vendor":
                        record = new VendorTable();
                        break;
                }

                return record;
            }
    }

        private class CustomerTable : BaseRecord
        {
            public string CustomerId { get; set; }
        }

        private class VendorTable : BaseRecord
        {
            public string VendorId { get; set; }
        }

        private class FindRecord : ReflectionFunction
        {
            private readonly TypeMarshallerCache _typeCache;

            public FindRecord(TypeMarshallerCache cache, RecordType recordType)
                : base("FindRecord", recordType, FormulaType.String)
            {
                _typeCache = cache;
            }

            public FindRecord(TypeMarshallerCache cache)
                : this(cache, RecordType.Empty())
            {
            }

            public RecordValue Execute(StringValue tableName)
            {
                var record = BaseRecord.CreateRecord(tableName.Value);

                return record != null ? _typeCache.Marshal(record, record.GetType()) as RecordValue : RecordValue.Empty();
            }
        }

        private class FindRecordVisitor : TexlVisitor
        {
            private readonly List<string> _recordTypes = new List<string>();

            public IEnumerable<string> RecordTypes => _recordTypes;

            public override void PostVisit(StrInterpNode node)
            {
            }

            public override void PostVisit(DottedNameNode node)
            {
            }

            public override void PostVisit(UnaryOpNode node)
            {
            }

            public override void PostVisit(BinaryOpNode node)
            {
            }

            public override void PostVisit(VariadicOpNode node)
            {
            }

            public override void PostVisit(CallNode node)
            {
                if (node.Head.Name.Equals("FindRecord"))
                {
                    if (node.Args.ChildNodes.First() is StrLitNode tableNameLiteral)
                    {
                        _recordTypes.Add(tableNameLiteral.Value);
                    }
                }
                else
                {
                    throw new ArgumentException("String literal expected as first argument for 'FindRecord' function");
                }
            }

            public override void PostVisit(ListNode node)
            {
            }

            public override void PostVisit(RecordNode node)
            {
            }

            public override void PostVisit(TableNode node)
            {
            }

            public override void PostVisit(AsNode node)
            {
            }

            public override void Visit(ErrorNode node)
            {
            }

            public override void Visit(BlankNode node)
            {
            }

            public override void Visit(BoolLitNode node)
            {
            }

            public override void Visit(StrLitNode node)
            {
            }

            public override void Visit(NumLitNode node)
            {
            }

            public override void Visit(FirstNameNode node)
            {
            }

            public override void Visit(ParentNode node)
            {
            }

            public override void Visit(SelfNode node)
            {
            }
        }
    }
}
