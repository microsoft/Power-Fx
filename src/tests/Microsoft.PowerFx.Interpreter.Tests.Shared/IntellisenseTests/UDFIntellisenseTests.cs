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
                        { FormulaType.Boolean.Name, FormulaType.Boolean },
                        { FormulaType.Color.Name, FormulaType.Color },
                        { FormulaType.Date.Name, FormulaType.Date },
                        { FormulaType.Time.Name, FormulaType.Time },
                        { FormulaType.DateTime.Name, FormulaType.DateTime },
                        { FormulaType.DateTimeNoTimeZone.Name, FormulaType.DateTimeNoTimeZone },
                        { FormulaType.Guid.Name, FormulaType.Guid },
                        { FormulaType.Number.Name, FormulaType.Number },
                        { FormulaType.Decimal.Name, FormulaType.Decimal },
                        { FormulaType.String.Name, FormulaType.String },
                        { FormulaType.Hyperlink.Name, FormulaType.Hyperlink },
                        { FormulaType.Blank.Name, FormulaType.Blank },
                        { FormulaType.UntypedObject.Name, FormulaType.UntypedObject },
                        { FormulaType.Void.Name, FormulaType.Void },
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
