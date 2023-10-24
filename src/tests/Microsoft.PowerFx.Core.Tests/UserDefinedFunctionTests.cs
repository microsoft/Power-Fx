// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class UserDefinedFunctionTests : PowerFxTest
    {
        [Theory]
        [InlineData("Foo(x: Number): Number = Abs(x);", 1, 0, false)]
        [InlineData("IsType(x: Number): Number = Abs(x);", 0, 0, true)]
        [InlineData("AsType(x: Number): Number = Abs(x);", 0, 0, true)]
        [InlineData("Type(x: Number): Number = Abs(x);", 0, 0, true)]
        [InlineData("Foo(x: Number): Number = Abs(x); x = 1;", 1, 1, false)]
        [InlineData("x = 1; Foo(x: Number): Number = Abs(x);", 1, 1, false)]
        [InlineData("/*this is a test*/ x = 1; Foo(x: Number): Number = Abs(x);", 1, 1, false)]
        [InlineData("x = 1; Foo(x: Number): Number = Abs(x); y = 2;", 1, 2, false)]
        [InlineData("Add(x: Number, y:Number): Number = x + y; Foo(x: Number): Number = Abs(x); y = 2;", 2, 1, false)]
        [InlineData("Foo(x: Number): Number = /*this is a test*/ Abs(x); y = 2;", 1, 1, false)]
        [InlineData("Add(x: Number, y:Number): Number = b + b; Foo(x: Number): Number = Abs(x); y = 2;", 2, 1, true)]
        [InlineData("Add(x: Number, y:Number): Boolean = x + y;", 1, 0, false)]
        [InlineData("Add(x: Number, y:Number): SomeType = x + y;", 0, 0, true)]
        [InlineData("Add(x: SomeType, y:Number): Number = x + y;", 0, 0, true)]
        [InlineData("Add(x: Number, y:Number): Number = x + y", 0, 0, true)]
        [InlineData("x = 1; Add(x: Number, y:Number): Number = x + y", 0, 1, true)]
        [InlineData("Add(x: Number, y:Number) = x + y;", 0, 0, true)]
        [InlineData("Add(x): Number = x + 2;", 0, 0, true)]
        [InlineData("Add(a:Number, b:Number): Number { a + b + 1; \n a + b; };", 0, 0, true)]
        [InlineData("Add(a:Number, b:Number): Number { a + b; };", 0, 0, true)]
        [InlineData("Add(a:Number, b:Number): Number { /*this is a test*/ a + b; };", 0, 0, true)]
        [InlineData("Add(a:Number, b:Number): Number { /*this is a test*/ a + b; ;", 0, 0, true)]
        [InlineData("Add(a:Number, a:Number): Number { a; };", 0, 0, true)]
        [InlineData(@"F2(b: Number): Number  = F1(b*3); F1(a:Number): Number = a*2;", 2, 0, false)]
        [InlineData(@"F2(b: Text): Text  = ""Test"";", 1, 0, false)]
        [InlineData(@"F2(b: String): String  = ""Test"";", 0, 0, true)]
        public void TestUDFNamedFormulaCounts(string script, int udfCount, int namedFormulaCount, bool expectErrors)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var userDefinitions = UserDefinitions.ProcessUserDefinitions(script, parserOptions, out var userDefinitionResult);
            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library);
            var glue = new Glue2DocumentBinderGlue();
            var hasBinderErrors = false;

            foreach (var udf in userDefinitionResult.UDFs)
            {
                var binding = udf.BindBody(ReadOnlySymbolTable.Compose(nameResolver, ReadOnlySymbolTable.NewDefault(userDefinitionResult.UDFs)), glue, BindingConfig.Default);
                hasBinderErrors |= binding.ErrorContainer.HasErrors();
            }

            Assert.Equal(udfCount, userDefinitionResult.UDFs.Count());
            Assert.Equal(namedFormulaCount, userDefinitionResult.NamedFormulas.Count());
            Assert.Equal(expectErrors, (userDefinitionResult.Errors?.Any() ?? false) || hasBinderErrors);
        }

        [Theory]
        [InlineData("Mul(x:Number, y:DateTime): DateTime = x * y;", "Mul(\"4a\", Date(1900, 1, 3))", "Mul:d(Float:n(\"4a\":s), DateToDateTime:d(Date:D(Coalesce:n(Float:n(1900:w), 0:n), Coalesce:n(Float:n(1:w), 0:n), Coalesce:n(Float:n(3:w), 0:n))))")]
        public void TestCoercionWithUDFParams(string udfScript, string invocationScript, string expectedIR)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library);
            var glue = new Glue2DocumentBinderGlue();
            var userDefinitions = UserDefinitions.ProcessUserDefinitions(udfScript, parserOptions, out var userDefinitionResult);
            var texlFunctionSet = new TexlFunctionSet(userDefinitionResult.UDFs);

            var engine = new Engine();
            var result = engine.Check(invocationScript, symbolTable: ReadOnlySymbolTable.Compose(ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library), ReadOnlySymbolTable.NewDefault(texlFunctionSet)));

            var actualIR = result.PrintIR();
            Assert.Equal(expectedIR, actualIR);
        }

        [Theory]
        [InlineData("Mul(x:Number, y:DateTime): DateTime = x * y;", "(NumberToDateTime:d(MulNumbers:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo), DateTimeToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)))), Scope 0)")]
        [InlineData("nTod(x:Number): Decimal = x;", "(Decimal:w(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dToN(x:Decimal): Number = x;", "(Float:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("sTon(x:Text): Number = x;", "(Float:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("bTon(x:Boolean): Number = x;", "(BooleanToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTon(x:Date): Number = x;", "(DateToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("timeTon(x:Time): Number = x;", "(TimeToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTimeTon(x:DateTime): Number = x;", "(DateTimeToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("sTod(x:Text): Decimal = x;", "(Decimal:w(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("bTod(x:Boolean): Decimal = x;", "(BooleanToDecimal:w(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTod(x:Date): Decimal = x;", "(DateToDecimal:w(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTimeTod(x:DateTime): Decimal = x;", "(DateTimeToDecimal:w(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("timeTod(x:Time): Decimal = x;", "(TimeToDecimal:w(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("textToHyperlink(x:Text): Hyperlink = x;", "(TextToHyperlink:h(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nTos(x:Number): Text = x;", "(NumberToText:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dTos(x:Decimal): Text = x;", "(DecimalToText:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("bTos(x:Boolean): Text = x;", "(BooleanToText:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTos(x:Date): Text = x;", "(Text:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTimeTos(x:DateTime): Text = x;", "(Text:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("timeTos(x:Time): Text = x;", "(Text:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToBool(x:Number): Boolean = x;", "(NumberToBoolean:b(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dToBool(x:Decimal): Boolean = x;", "(DecimalToBoolean:b(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("sToBool(x:Text): Boolean = x;", "(TextToBoolean:b(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToDateTime(x:Number): DateTime = x;", "(NumberToDateTime:d(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dToDateTime(x:Decimal): DateTime = x;", "(DecimalToDateTime:d(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToDate(x:Number): Date = x;", "(NumberToDate:D(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dToDate(x:Decimal): Date = x;", "(DecimalToDate:D(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToTime(x:Number): Time = x;", "(NumberToTime:T(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dToTime(x:Decimal): Time = x;", "(DecimalToTime:T(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("sToDateTime(x:Text): DateTime = x;", "(DateTimeValue:d(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("sToDate(x:Text): Date = x;", "(DateValue:D(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("sToTime(x:Text): Time = x;", "(TimeValue:T(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTimeToTime(x:DateTime): Time = x;", "(DateTimeToTime:T(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateToTime(x:Date): Time = x;", "(DateToTime:T(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("timeToDate(x:Time): Date = x;", "(TimeToDate:D(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTimeToDate(x:DateTime): Date = x;", "(DateTimeToDate:D(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("timeToDateTime(x:Time): DateTime = x;", "(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo), Scope 0)")]
        [InlineData("dateToDateTime(x:Date): DateTime = x;", "(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo), Scope 0)")]
        [InlineData("textToGUID(x:Text): GUID = x;", "(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo), Scope 0)")]
        [InlineData("GUIDToText(x:GUID): Text = x;", "(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo), Scope 0)")]
        public void TestCoercionWithUDFBody(string udfScript, string expectedIR)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library);
            var glue = new Glue2DocumentBinderGlue();
            var userDefinitions = UserDefinitions.ProcessUserDefinitions(udfScript, parserOptions, out var userDefinitionResult);
            var udfs = new TexlFunctionSet(userDefinitionResult.UDFs);

            Assert.Single(userDefinitionResult.UDFs);

            var udf = userDefinitionResult.UDFs.First();
            var binding = udf.BindBody(ReadOnlySymbolTable.Compose(nameResolver, ReadOnlySymbolTable.NewDefault(udfs)), glue, BindingConfig.Default);
            var actualIR = IRTranslator.Translate(binding).ToString();

            Assert.Equal(expectedIR, actualIR);
        }

        [Theory]
        [InlineData("/* Comment1 */ Foo(x: Number): Number = /* Comment2 */ Abs(x) /* Comment3 */;/* Comment4 */", 4)]
        [InlineData("Foo(x: Number): Number /* Comment1 */ =  Abs(x);// Comment2", 2)]
        public void TestCommentsFromUserDefinitionsScript(string script, int commentCount)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);

            Assert.Equal(commentCount, parseResult.Comments.Count());
        }

        [Theory]
        [InlineData("/* Comment1 */ Foo(x: Number): Number = Abs(x);", 0, 15)]
        [InlineData("Foo(x: Number): Number /* Comment1 */ = Abs(x);", 23, 38)]
        [InlineData("Foo(x: Number): Number = Abs(x) /* Comment1 */;", 32, 46)]
        [InlineData("Foo(x: Number): Number = Abs(x); /* Comment1 */", 33, 47)]
        public void TestCommentSpansFromUserDefinitionsScript(string script, int begin, int end)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);

            Assert.Single(parseResult.Comments);

            var commentSpan = parseResult.Comments.First().Span;

            Assert.Equal(commentSpan.Min, begin);
            Assert.Equal(commentSpan.Lim, end);
        }

        [Theory]
        [InlineData("a = Abs(1.2);\nAdd(a: Number, b: Number):Number = a + b;\nb = Add(1, 2);", 2, 1, 0)]
        [InlineData("a = Abs(1.2);\nAdd(a: Number, b:):Number = a + b;\nb = Add(1, 2); ", 2, 0, 1)]
        [InlineData("a = Abs(1.2);\nAdd(a: Number, b:/*comment*/):Number = a + b;\nb = Add(1, 2); ", 2, 0, 1)]
        [InlineData("a = Abs(1.2);\nAdd(a: Number, b:/*comment*/Number):Number = a + b;\nb = Add(1, 2); ", 2, 1, 0)]
        [InlineData("a = Abs(1.2);\nF1(a: Number):Number = a;\nb = F1(1, 2); F2(", 2, 1, 1)]
        [InlineData("a = Abs(1.2);\nF1(a: Number):Number = a;\nb = F1(1, 2); F2(a):Number = 1;", 2, 1, 1)]
        [InlineData("a = Abs(1.2);\nF1(a: Number):Number = a;\nb = F1(1, 2); F2(a):Number = ;", 2, 1, 1)]
        [InlineData("a = Abs(1.2);\nF1(a: Number):Number = a;\nb = F1(1, 2); F2(a:Number): = 1;", 2, 1, 1)]
        [InlineData(@"A(a:Number, b:/*comment*/Number):Number = 12;b(a:Number): = 1;c(a:Number):Number = 100;x = 10;", 1, 2, 1)]
        [InlineData(@"A(a:Number, b:/*comment*/Number):Number = 12;b(a:):Number = 1;c(a:Number):Number = 100;", 0, 2, 1)]
        [InlineData("a = Abs(1.2);\nAdd(a: Number, b: Number):/* Number */Number", 1, 0, 1)]
        [InlineData("a = Abs(1.2);\nAdd(a: Number, b: Number):/* Number */Number = a + b;", 1, 1, 0)]

        public void TestParseUserDefinitionsCountswithIncompleteUDFs(string script, int nfCount, int validUDFCount, int inValidUDFCount)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);

            Assert.Equal(nfCount, parseResult.NamedFormulas.Count());
            Assert.Equal(validUDFCount, parseResult.UDFs.Count(udf => udf.IsParseValid));
            Assert.Equal(inValidUDFCount, parseResult.UDFs.Count(udf => !udf.IsParseValid));
        }

        [Theory]

        [InlineData("a = Abs(1.2);\n F1(a:Number):Number = a;", 0)]
        [InlineData("a = Abs(1.2);\n F1(a):Number = a;", 1)]
        [InlineData("a = Abs(1.2);\n F1(a:):Number = a;", 1)]
        [InlineData("a = Abs(1.2);\n F1(a:Number): = a;", 1)]
        [InlineData("a = -;\n F1(a:):Number = 1;F2(a:Number): = 1;", 3)]
        public void TestErrorCountsWithUDFs(string script, int errorCount)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);

            Assert.Equal(errorCount, parseResult.Errors?.Count() ?? 0);
        }

        /// <summary>
        /// Verifies that UDFs are marked as valid/invalid approriately.
        /// </summary>
        [Fact]
        public void TestUserDefinedFunctionValidity()
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var script = @" a = Abs(1.2);
                            F1(a:Number, b: Number):Number = a + b /*comment*/;
                            F2(a:Number, b):Number = a + b;
                            F3(a:Number, b:):Number = a + b;
                            b = ""test"";
                            F4(";
            var parseResult = UserDefinitions.Parse(script, parserOptions);
            Assert.Equal(2, parseResult.NamedFormulas.Count());
            Assert.Equal(1, parseResult.UDFs.Count(udf => udf.IsParseValid));
            Assert.Equal(3, parseResult.UDFs.Count(udf => !udf.IsParseValid));

            foreach (var udf in parseResult.UDFs)
            {
                if (udf.Ident.Name == "F1")
                {
                    // Verify return type colon token span
                    Assert.Equal(67, udf.ReturnTypeColonToken.Span.Min);
                    Assert.Equal(68, udf.ReturnTypeColonToken.Span.Lim);
                }
                else if (udf.Ident.Name == "F2")
                {
                    Assert.Equal(2, udf.Args.Count());
                    var firstArg = udf.Args.ElementAt(0);
                    var secondArg = udf.Args.ElementAt(1);
                    Assert.NotNull(firstArg.ColonToken);

                    // Verify first arg colon token span
                    Assert.Equal(129, firstArg.ColonToken.Span.Min);
                    Assert.Equal(130, firstArg.ColonToken.Span.Lim);

                    Assert.Null(secondArg.ColonToken);
                    Assert.Null(secondArg.TypeIdent);
                }
                else if (udf.Ident.Name == "F3")
                {
                    Assert.Equal(2, udf.Args.Count());
                    Assert.Null(udf.Args.ElementAt(1).TypeIdent);
                }
                else if (udf.Ident.Name == "F4")
                {
                    Assert.Empty(udf.Args);
                    Assert.Null(udf.ReturnTypeColonToken);
                    Assert.Null(udf.ReturnType);
                    Assert.Null(udf.Body);
                }
            }
        }

        /// <summary>
        /// Verifies that UDFs are marked as valid/invalid approriately.
        /// </summary>
        [Fact]
        public void TestUserDefinedFunctionValidity2()
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var script = @"func(a";
            var parseResult = UserDefinitions.Parse(script, parserOptions);
            var func = parseResult.UDFs.FirstOrDefault(udf => udf.Ident.Name == "func");

            Assert.False(func.IsParseValid);
            Assert.NotNull(func);
            Assert.Single(func.Args);
            var firstArg = func.Args.Single();
            Assert.NotNull(firstArg.NameIdent);
            Assert.Null(firstArg.TypeIdent);
            Assert.Null(firstArg.ColonToken);
            Assert.Null(func.ReturnTypeColonToken);
            Assert.Null(func.ReturnType);
            Assert.Null(func.Body);

            script = @"func(a:";
            parseResult = UserDefinitions.Parse(script, parserOptions);
            func = parseResult.UDFs.FirstOrDefault(udf => udf.Ident.Name == "func");

            Assert.False(func.IsParseValid);
            Assert.NotNull(func);
            Assert.Single(func.Args);
            firstArg = func.Args.Single();
            Assert.NotNull(firstArg.NameIdent);
            Assert.Null(firstArg.TypeIdent);
            Assert.NotNull(firstArg.ColonToken);

            script = @"func(a:Number";
            parseResult = UserDefinitions.Parse(script, parserOptions);
            func = parseResult.UDFs.FirstOrDefault(udf => udf.Ident.Name == "func");

            Assert.False(func.IsParseValid);
            Assert.NotNull(func);
            Assert.Single(func.Args);
            firstArg = func.Args.Single();
            Assert.NotNull(firstArg.NameIdent);
            Assert.NotNull(firstArg.TypeIdent);
            Assert.NotNull(firstArg.ColonToken);

            script = @"func(a:Number, b";
            parseResult = UserDefinitions.Parse(script, parserOptions);
            func = parseResult.UDFs.FirstOrDefault(udf => udf.Ident.Name == "func");

            Assert.False(func.IsParseValid);
            Assert.NotNull(func);
            Assert.Equal(2, func.Args.Count());
            firstArg = func.Args.ElementAt(0);
            Assert.NotNull(firstArg.NameIdent);
            Assert.NotNull(firstArg.TypeIdent);
            Assert.NotNull(firstArg.ColonToken);
            firstArg = func.Args.ElementAt(1);
            Assert.NotNull(firstArg.NameIdent);

            script = @"func(a:Number):";
            parseResult = UserDefinitions.Parse(script, parserOptions);
            func = parseResult.UDFs.FirstOrDefault(udf => udf.Ident.Name == "func");

            Assert.False(func.IsParseValid);
            Assert.NotNull(func);
            Assert.Single(func.Args);
            Assert.NotNull(func.ReturnTypeColonToken);
            Assert.Null(func.ReturnType);
            Assert.Null(func.Body);

            script = @"func(a:Number):Number";
            parseResult = UserDefinitions.Parse(script, parserOptions);
            func = parseResult.UDFs.FirstOrDefault(udf => udf.Ident.Name == "func");

            Assert.False(func.IsParseValid);
            Assert.NotNull(func);
            Assert.Single(func.Args);
            Assert.NotNull(func.ReturnTypeColonToken);
            Assert.NotNull(func.ReturnType);
            Assert.Null(func.Body);

            script = @"func(a:Number):Number = 1;";
            parseResult = UserDefinitions.Parse(script, parserOptions);
            func = parseResult.UDFs.FirstOrDefault(udf => udf.Ident.Name == "func");

            Assert.NotNull(func);
            Assert.Single(func.Args);
            Assert.NotNull(func.ReturnTypeColonToken);
            Assert.NotNull(func.ReturnType);
            Assert.NotNull(func.Body);
            Assert.True(func.IsParseValid);
        }

        // Show definitions directly on symbol tables
        [Fact]
        public void Basic()
        {
            var st1 = new SymbolTable();
            st1.AddUserDefinedFunction("Foo1(x: Number): Number = x*2;");
            st1.AddUserDefinedFunction("Foo2(x: Number): Number = Foo1(x)+1;");

            var engine = new Engine();
            var check = engine.Check("Foo2(3)", symbolTable: st1);
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.Number, check.ReturnType);

            // A different symbol table can have same function name with different type.  
            var st2 = new SymbolTable();
            st2.AddUserDefinedFunction("Foo2(x: Number): Text = x;");
            check = engine.Check("Foo2(3)", symbolTable: st2);
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.String, check.ReturnType);
        }
                
        [Fact]
        public void DefineEmpty()
        {
            // Empty symbol table doesn't get builtins. 
            var st = new SymbolTable();

            st.AddUserDefinedFunction("Foo1(x: Number): Number = x;"); // ok 
            Assert.Throws<InvalidOperationException>(() => st.AddUserDefinedFunction("Foo2(x: Number): Number = Abs(x);"));
        }

        // Show definitions on public symbol tables
        [Fact]
        public void BasicEngine()
        {
            var extra = new SymbolTable();
            extra.AddVariable("K1", FormulaType.Number);

            var engine = new Engine();
            engine.AddUserDefinedFunction("Foo1(x: Number): Number = Abs(K1);", symbolTable: extra);

            var check = engine.Check("Foo1(3)");
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.Number, check.ReturnType);
        }

        [Fact]
        public void TestUserDefinedFunctionCloning()
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var script = "Add(a: Number, b: Number):Number = a + b;";
            UserDefinitions.ProcessUserDefinitions(script, parserOptions, out var userDefinitionResult);
            var func = userDefinitionResult.UDFs.FirstOrDefault();
            Assert.NotNull(func);

            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library);
            var glue = new Glue2DocumentBinderGlue();
            var udfs = new TexlFunctionSet(userDefinitionResult.UDFs);

            Assert.Single(userDefinitionResult.UDFs);

            var udf = userDefinitionResult.UDFs.First();
            var binding = udf.BindBody(ReadOnlySymbolTable.Compose(nameResolver, ReadOnlySymbolTable.NewDefault(udfs)), glue, BindingConfig.Default);
            var clonedFunc = func.WithBinding(nameResolver, glue, out binding);
            Assert.NotNull(clonedFunc);
            Assert.NotNull(binding);

            Assert.NotEqual(func, clonedFunc);
        }
    }
}
