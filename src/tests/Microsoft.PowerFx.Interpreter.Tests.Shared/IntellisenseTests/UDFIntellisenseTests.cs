// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Intellisense;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests.Shared.IntellisenseTests
{
    public class UDFIntellisenseTests
    {
        [Theory]
        [InlineData("AddNumbers(x: Number, y: Number): Number = x + y; |", "")]
        [InlineData("AddNumbers(x: Number, y: Number): Number = x + |", "")]
        [InlineData("AddNumbers(x: Number, y: Number): Number = x + Su|", "ErrorKind.InsufficientMemory,ErrorKind.NotSupported,StartOfWeek.Sunday,Sum,Substitute,TraceOptions.IgnoreUnsupportedTypes")]
        [InlineData("AddNumbers(x: Number, y: Number): |", "Boolean,Color,Date,DateTime,Decimal,Dynamic,GUID,Hyperlink,Number,Text,Time,Void")]
        [InlineData("AddNumbers(x: Number, y: |", "Boolean,Color,Date,DateTime,Decimal,Dynamic,GUID,Hyperlink,Number,Text,Time")]

        // Suggest UDF names when calling one UDF from another
        [InlineData("AddNumbers(x: Number, y: Number): Number = x + y; AddNumbers2(x: Number, y: Text): Number = AddNum|", "AddNumbers")]
        public void UDFSuggestionTest(string expression, string expected)
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config)
            {
                    PrimitiveTypes = SymbolTable.NewDefaultTypes(ImmutableDictionary.CreateRange(new Dictionary<DName, FormulaType>()
                    {
                        { new DName("Boolean"), FormulaType.Boolean },
                        { new DName("Color"), FormulaType.Color },
                        { new DName("Date"), FormulaType.Date },
                        { new DName("Time"), FormulaType.Time },
                        { new DName("DateTime"), FormulaType.DateTime },
                        { new DName("GUID"), FormulaType.Guid },
                        { new DName("Number"), FormulaType.Number },
                        { new DName("Decimal"), FormulaType.Decimal },
                        { new DName("Text"), FormulaType.String },
                        { new DName("Hyperlink"), FormulaType.Hyperlink },
                        { new DName("Dynamic"), FormulaType.UntypedObject },
                        { new DName("Void"), FormulaType.Void },
                    }))
            };
            var scope = engine.CreateUDFEditorScope();

            // engine.AddUserDefinedFunction(expression);

            var iResult = scope.Suggest(expression, expression.IndexOf('|'));

            var suggestions = iResult.Suggestions;

            var sugg = string.Join(",", suggestions.Select(s => s.DisplayText.Text));
            Assert.Equal(expected, sugg);
        }
    }
}
