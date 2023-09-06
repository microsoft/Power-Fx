// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Texl.Intellisense;

namespace Microsoft.PowerFx.Tests.IntellisenseTests
{
    internal class SkippingTokensTestCases
    {
        private static readonly IEnumerable<TokenizationTestCase> SkippingTokenTestCases = new List<TokenizationTestCase>
        {
            // Only + should be in the final result
            TokenizationTestCase.Create(
                "1 + 1",
                CreateTokenTypesToSkip(TokenType.NumLit),
                ExpectedToken.CreateIgnoredPlaceholderToken("1 ".Length),
                ExpectedToken.CreateBinaryOpToken()),

            // All tokens should be skipped
            TokenizationTestCase.Create(
                "1 + 1 = 2",
                CreateTokenTypesToSkip(TokenType.NumLit, TokenType.BinaryOp)),

            // Only Delimeter tokens should be in the final result
            TokenizationTestCase.Create(
               "Max(1,2,3)",
               CreateTokenTypesToSkip(TokenType.NumLit, TokenType.BinaryOp, TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("Max(1".Length),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateDelimeterToken()),

            // Max function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "Max\n\n\n\n\n\n\n(1,2,3)",
               CreateTokenTypesToSkip(TokenType.NumLit, TokenType.Function),
               ExpectedToken.CreateFunctionToken(3),
               ExpectedToken.CreateIgnoredPlaceholderToken("\n\n\n\n\n\n\n(1".Length),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateDelimeterToken()),

            // Max function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "Max\n\n\n\n\n\n\n(1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateFunctionToken(3),
               ExpectedToken.CreateIgnoredPlaceholderToken("\n\n\n\n\n\n\n(".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // Max function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "Max\n\n\n\n\n\n\n(1,2,3)",
               ExpectedToken.CreateFunctionToken(3),
               ExpectedToken.CreateIgnoredPlaceholderToken("\n\n\n\n\n\n\n(".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // 'Foo Bar Bar Foo' function token should be ignored as it is immediately followed by (
            TokenizationTestCase.Create(
               "'Foo Bar Bar Foo'(1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("'Foo Bar Bar Foo'(".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // 'Foo Bar Bar Foo' function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "'Foo Bar Bar Foo'                       (1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function, TokenType.Delimiter),
               ExpectedToken.CreateFunctionToken("'Foo Bar Bar Foo'".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken("                       (".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateNumLitToken(1)),

            // 'Foo Bar Bar Foo' function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "'Foo Bar Bar Foo'                       \n\n\n(1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateFunctionToken("'Foo Bar Bar Foo'".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken("                       \n\n\n(".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // CrashStudio function token should be ignored as it is immediately followed by (
            TokenizationTestCase.Create(
               "StudioCrashService.CrashStudio(1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("StudioCrashService".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(".CrashStudio(".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // CrashStudio function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "StudioCrashService.CrashStudio      (1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("StudioCrashService".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateFunctionToken("CrashStudio".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken("      (".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // CrashStudio function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "StudioCrashService.'Crash Studio'  \n\n\n\n\n   (1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("StudioCrashService".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateFunctionToken("'Crash Studio'".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken("  \n\n\n\n\n   (".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // 'Crash Studio' function token should be ignored as it is immediately followed by (
            TokenizationTestCase.Create(
               "StudioCrashService.'Crash Studio'(1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("StudioCrashService".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(".'Crash Studio'(".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // 'Crash Studio' function token should not be ignored as it is not immediately followed by (
            TokenizationTestCase.Create(
               "StudioCrashService.'Crash Studio' (1,2,3)",
               CreateTokenTypesToSkip(TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("StudioCrashService".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateFunctionToken("'Crash Studio'".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(" (".Length),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateDelimeterToken(),
               ExpectedToken.CreateNumLitToken(1)),

            // Almost all tokens should be ignored except the Sum function which is not immediately followed by (
            TokenizationTestCase.Create(
               "true;\"String Var\";//Comment One\n12;12e3;2;false;Self;Parent;/*Comment Line One\nCommentLineTwo\n\n*/1+2;%3;Max(1,2,3);Sum   (1,2,3)",
               new ParserOptions { AllowsSideEffects = true, NumberIsFloat = true },
               CreateTokenTypesToSkip(TokenType.Function, TokenType.StrLit, TokenType.NumLit, TokenType.Self, TokenType.Parent, TokenType.Comment, TokenType.BoolLit, TokenType.UnaryOp, TokenType.BinaryOp, TokenType.VariadicOp, TokenType.Delimiter),
               ExpectedToken.CreateIgnoredPlaceholderToken("true;\"String Var\";//Comment One\n12;12e3;2;false;Self;Parent;/*Comment Line One\nCommentLineTwo\n\n*/1+2;%3;Max(1,2,3);".Length),
               ExpectedToken.CreateFunctionToken(3)),

            // Almost all tokens should be ignored except the Sum function which is not immediately followed by (
            TokenizationTestCase.Create(
               "true;\"String Var\";//Comment One\n12;12e3;2;false;Self;Parent;/*Comment Line One\nCommentLineTwo\n\n*/1+2;%3;Max(1,2,3);Sum   (1,2,3)",
               new ParserOptions { AllowsSideEffects = true },
               CreateTokenTypesToSkip(TokenType.Function, TokenType.StrLit, TokenType.DecLit, TokenType.Self, TokenType.Parent, TokenType.Comment, TokenType.BoolLit, TokenType.UnaryOp, TokenType.BinaryOp, TokenType.VariadicOp, TokenType.Delimiter),
               ExpectedToken.CreateIgnoredPlaceholderToken("true;\"String Var\";//Comment One\n12;12e3;2;false;Self;Parent;/*Comment Line One\nCommentLineTwo\n\n*/1+2;%3;Max(1,2,3);".Length),
               ExpectedToken.CreateFunctionToken(3)),

            // Color Enum should be ignored
            TokenizationTestCase.Create(
               "Color.White",
               CreateTokenTypesToSkip(TokenType.Enum),
               ExpectedToken.CreateIgnoredPlaceholderToken(6),
               ExpectedToken.CreateDottedNamePartToken(5)),

            // White should be ignored
            TokenizationTestCase.Create(
               "Color.White",
               CreateTokenTypesToSkip(TokenType.DottedNamePart),
               ExpectedToken.CreateColorEnumToken()),

            // All control tokens should be ignored
            TokenizationTestCase.Create(
               "Select(gallery1.Selected.nestedLabel1)",
               CreateTokenTypesToSkip(TokenType.Control),
               ExpectedToken.CreateFunctionToken("Select".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken("(gallery1.".Length),
               ExpectedToken.CreateDottedNamePartToken("Selected".Length)),

            // No control tokens should be ignored
            TokenizationTestCase.Create(
               "Select(gallery1.Selected.nestedLabel1)",
               CreateTokenTypesToSkip(TokenType.DottedNamePart, TokenType.Function),
               ExpectedToken.CreateIgnoredPlaceholderToken("Select(".Length),
               ExpectedToken.CreateControlToken("gallery1".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(".Selected.".Length),
               ExpectedToken.CreateControlToken("nestedLabel1".Length)),

            // Only First function and gallery1 tokens should be in the final result
            TokenizationTestCase.Create(
               "First (foo).test;gallery1;ThisItem.Income;name",
               new ParserOptions { AllowsSideEffects = true },
               CreateTokenTypesToSkip(TokenType.DottedNamePart, TokenType.Function, TokenType.VariadicOp, TokenType.ThisItem, TokenType.Alias, TokenType.Data),
               ExpectedToken.CreateFunctionToken(5),
               ExpectedToken.CreateIgnoredPlaceholderToken(" (foo).test;".Length),
               ExpectedToken.CreateControlToken("gallery1".Length)),

            // Only control and First function tokens should be ignored
            TokenizationTestCase.Create(
               "First(foo).test;gallery1;ThisItem.Income;name",
               new ParserOptions { AllowsSideEffects = true },
               CreateTokenTypesToSkip(TokenType.DottedNamePart, TokenType.Function, TokenType.VariadicOp, TokenType.Control),
               ExpectedToken.CreateIgnoredPlaceholderToken(6),
               ExpectedToken.CreateDataToken(3),
               ExpectedToken.CreateIgnoredPlaceholderToken(").test;gallery1;".Length),
               ExpectedToken.CreateThisItemToken(),
               ExpectedToken.CreateIgnoredPlaceholderToken(".Income;".Length),
               ExpectedToken.CreateAliasToken(4)),

            // Only String interpolation start and num lit tokens should be ignored
            TokenizationTestCase.Create(
               "$\"1 + 2 = {3}\"",
               CreateTokenTypesToSkip(TokenType.StrInterpStart, TokenType.NumLit),
               ExpectedToken.CreateIgnoredPlaceholderToken(2),
               ExpectedToken.CreateStrLitToken("1 + 2 = "),
               ExpectedToken.CreateIslandStartToken(),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateIslandEndToken(),
               ExpectedToken.CreateStringInterpEndToken()),
            
            // Only string interp end tokens and str lit tokens should be ignored
            TokenizationTestCase.Create(
               "$\"1 + 2 = {3}\"",
               CreateTokenTypesToSkip(TokenType.StrInterpEnd, TokenType.StrLit),
               ExpectedToken.CreateStringInterpStartToken(),
               ExpectedToken.CreateIgnoredPlaceholderToken("1 + 2 = ".Length),
               ExpectedToken.CreateIslandStartToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateIslandEndToken()),

            // Only island start tokens should be ignored
            TokenizationTestCase.Create(
               "$\"1 + 2 = {3}\"",
               CreateTokenTypesToSkip(TokenType.IslandStart),
               ExpectedToken.CreateStringInterpStartToken(),
               ExpectedToken.CreateStrLitToken("1 + 2 = ".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateIslandEndToken(),
               ExpectedToken.CreateStringInterpEndToken()),

            // Only Island start and string interp tokens should be ignored
            TokenizationTestCase.Create(
               "$\"1 + 2 = {3}\"",
               CreateTokenTypesToSkip(TokenType.IslandEnd, TokenType.StrInterpStart),
               ExpectedToken.CreateIgnoredPlaceholderToken(2),
               ExpectedToken.CreateStrLitToken("1 + 2 = ".Length),
               ExpectedToken.CreateIslandStartToken(),
               ExpectedToken.CreateNumLitToken(1),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateStringInterpEndToken()),

            // All string interp realted tokens should be ignored
            TokenizationTestCase.Create(
               "$\"1 + 2 = {3}\"",
               CreateTokenTypesToSkip(TokenType.IslandEnd, TokenType.StrInterpStart, TokenType.StrInterpEnd, TokenType.IslandStart),
               ExpectedToken.CreateIgnoredPlaceholderToken(2),
               ExpectedToken.CreateStrLitToken("1 + 2 = ".Length),
               ExpectedToken.CreateIgnoredPlaceholderToken(1),
               ExpectedToken.CreateNumLitToken(1))
        };

        private static IReadOnlyCollection<TokenType> CreateTokenTypesToSkip(params TokenType[] tokenTypes)
        {
            return tokenTypes.ToHashSet();
        }

        public static IEnumerable<object[]> SkippingTokenTestCasesAsObjects => TokenizationTestCase.TestCasesAsObjectsArray(SkippingTokenTestCases);
    }
}
