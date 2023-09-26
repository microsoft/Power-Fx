// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Interpreter.Tests.Helpers
{
    internal static class SemanticTokenTestHelper
    {
        internal static CheckResult GetCheckResultWithControlSymbols(string expression)
        {
            var powerFxConfig = PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder().WithDefaultEnums(), new TexlFunctionSet());
            var engine = new Engine(powerFxConfig);
            var mockSymbolTable = new MockSymbolTable();
            mockSymbolTable.AddControlAsAggregateType("Label2", new TypedName(DType.String, DName.MakeValid("Text", out _)));
            mockSymbolTable.AddControlAsControlType("NestedLabel1");

            var checkResult = engine.Check(expression, new ParserOptions { AllowsSideEffects = true, NumberIsFloat = true }, mockSymbolTable);
            return checkResult;
        }
    }
}
