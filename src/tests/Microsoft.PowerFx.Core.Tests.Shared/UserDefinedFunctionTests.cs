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
        private static readonly ReadOnlySymbolTable _primitiveTypes = ReadOnlySymbolTable.PrimitiveTypesTableInstance;

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
        [InlineData(@"F2(b: Number): Number  = F1(b*3); F1(a:Number): Number = a*2;", 2, 0, false)]
        [InlineData(@"F2(b: Text): Text  = ""Test"";", 1, 0, false)]
        [InlineData(@"F2(b: String): String  = ""Test"";", 0, 0, true)]
        public void TestUDFNamedFormulaCounts(string script, int udfCount, int namedFormulaCount, bool expectErrors)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);

            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library, FormulaType.PrimitiveTypes);

            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), nameResolver, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            var glue = new Glue2DocumentBinderGlue();
            var hasBinderErrors = false;

            foreach (var udf in udfs)
            {
                var binding = udf.BindBody(ReadOnlySymbolTable.Compose(nameResolver, ReadOnlySymbolTable.NewDefault(udfs)), glue, BindingConfig.Default);
                hasBinderErrors |= binding.ErrorContainer.HasErrors();
            }

            Assert.Equal(udfCount, udfs.Count());
            Assert.Equal(namedFormulaCount, parseResult.NamedFormulas.Count());
            Assert.Equal(expectErrors, (errors?.Any() ?? false) || hasBinderErrors);
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

            var parseResult = UserDefinitions.Parse(udfScript, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            var texlFunctionSet = new TexlFunctionSet(udfs);

            var engine = new Engine();
            var result = engine.Check(invocationScript, symbolTable: ReadOnlySymbolTable.Compose(ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library), ReadOnlySymbolTable.NewDefault(texlFunctionSet)));

            var actualIR = result.PrintIR();
            Assert.Equal(expectedIR, actualIR);
        }

        [Theory]
        [InlineData("Mul(x:Number, y:DateTime): DateTime = x * y;", "(NumberToDateTime:d(MulNumbers:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo), DateTimeToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)))), Scope 0)")]
        [InlineData("sTon(x:Text): Number = x;", "(Float:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("bTon(x:Boolean): Number = x;", "(BooleanToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTon(x:Date): Number = x;", "(DateToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("timeTon(x:Time): Number = x;", "(TimeToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTimeTon(x:DateTime): Number = x;", "(DateTimeToNumber:n(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("textToHyperlink(x:Text): Hyperlink = x;", "(TextToHyperlink:h(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nTos(x:Number): Text = x;", "(NumberToText:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("bTos(x:Boolean): Text = x;", "(BooleanToText:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTos(x:Date): Text = x;", "(Text:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("dateTimeTos(x:DateTime): Text = x;", "(Text:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("timeTos(x:Time): Text = x;", "(Text:s(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToBool(x:Number): Boolean = x;", "(NumberToBoolean:b(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("sToBool(x:Text): Boolean = x;", "(TextToBoolean:b(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToDateTime(x:Number): DateTime = x;", "(NumberToDateTime:d(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToDate(x:Number): Date = x;", "(NumberToDate:D(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
        [InlineData("nToTime(x:Number): Time = x;", "(NumberToTime:T(ResolvedObject(Microsoft.PowerFx.Core.Binding.BindInfo.UDFParameterInfo)), Scope 0)")]
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

            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library, FormulaType.PrimitiveTypes);
            var glue = new Glue2DocumentBinderGlue();

            var parseResult = UserDefinitions.Parse(udfScript, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), nameResolver, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            var texlFunctionSet = new TexlFunctionSet(udfs);

            Assert.Single(udfs);

            var udf = udfs.First();
            var binding = udf.BindBody(ReadOnlySymbolTable.Compose(nameResolver, ReadOnlySymbolTable.NewDefault(texlFunctionSet)), glue, BindingConfig.Default);
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
            var st1 = SymbolTable.WithPrimitiveTypes();
            st1.AddUserDefinedFunction("Foo1(x: Number): Number = x*2;");
            st1.AddUserDefinedFunction("Foo2(x: Number): Number = Foo1(x)+1;");

            var engine = new Engine();
            var check = engine.Check("Foo2(3)", symbolTable: st1);
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.Number, check.ReturnType);

            // A different symbol table can have same function name with different type.  
            var st2 = SymbolTable.WithPrimitiveTypes();
            st2.AddUserDefinedFunction("Foo2(x: Number): Text = x;");
            check = engine.Check("Foo2(3)", symbolTable: st2);
            Assert.True(check.IsSuccess);
            Assert.Equal(FormulaType.String, check.ReturnType);
        }

        [Fact]
        public void DefineEmpty()
        {
            // Empty symbol table doesn't get builtins. 
            var st = SymbolTable.WithPrimitiveTypes();
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

            var parseResult = UserDefinitions.Parse(script, parserOptions);

            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library, FormulaType.PrimitiveTypes);

            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), nameResolver, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            var func = udfs.FirstOrDefault();
            Assert.NotNull(func);

            var glue = new Glue2DocumentBinderGlue();
            var texlFunctionSet = new TexlFunctionSet(udfs);

            Assert.Single(udfs);

            var udf = udfs.First();
            var binding = udf.BindBody(ReadOnlySymbolTable.Compose(nameResolver, ReadOnlySymbolTable.NewDefault(texlFunctionSet)), glue, BindingConfig.Default);
            var clonedFunc = func.WithBinding(nameResolver, glue, out binding);
            Assert.NotNull(clonedFunc);
            Assert.NotNull(binding);

            Assert.NotEqual(func, clonedFunc);
        }

        [Theory]
        [InlineData("x = $\"{\"}\";", 1, 0, 0)]
        [InlineData("x = First([$\"{ {a:1,b:2,c:3}.a }]).Value;", 1, 0, 0)]
        [InlineData("x = $\"{\"1$\"}.{\"}\";\r\nudf():Text = $\"{\"}\";\r\ny = 2;", 2, 1, 0)]
        [InlineData("x = $\"{$\"{$\"{$\"{.12e4}\"}}\"}\";\r\nudf():Text = $\"{\"}\";\r\ny = 2;", 2, 1, 0)]
        [InlineData("x = $\"{$\"{$\"{$\"{.12e4}\"}\"}\"}{$\"Another nested}\";\r\nudf():Text = $\"{\"}\";\r\ny = 2;", 2, 1, 0)]
        public void TestUDF(string formula, int nfCount, int udfCount, int validUdfCount)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(formula, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.Equal(nfCount, parseResult.NamedFormulas.Count());
            Assert.Equal(udfCount, parseResult.UDFs.Count());
            Assert.Equal(validUdfCount, udfs.Count());
            Assert.Contains(errors, e => e.MessageKey == "ErrBadToken");
        }

        [Theory]
        [InlineData("Foo(x: Number): None = Abs(x);")]
        [InlineData("Foo(x: None): Number = Abs(x);")]
        [InlineData("Foo(x: Decimal): Number =  Abs(x);")]
        [InlineData("Foo(x: Number): Decimal =  Abs(x);")]
        [InlineData("Foo(x: DateTimeTZInd): Decimal =  Abs(x);")]
        [InlineData("Foo(x: Number): DateTimeTZInd =  Abs(x);")]
        public void TestUDFsWithRestrictedTypes(string script)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.Contains(errors, x => x.MessageKey == "ErrUDF_InvalidReturnType" || x.MessageKey == "ErrUDF_InvalidParamType");
        }

        [Theory]
        [InlineData("Set(x: Number):Number = x + 1;", true)]
        [InlineData("Count():Number = 5;", false)]
        public void TestUDFsReservedNames(string script, bool expectedError)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            if (expectedError)
            {
                Assert.Contains(errors, x => x.MessageKey == "ErrUDF_FunctionNameRestricted");
            }
            else
            {
                Assert.True(errors.Count() == 0);
            }
        }

        [Fact]
        public void TestUDFsReservedNamesTracking()
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            // Adding a restricted UDF name is a breaking change, this test will need to be updated and a conversion will be needed for existing scenarios
            var restrictedUDFNames = new HashSet<string>
            {
                "Type", "IsType", "AsType", "Set", "Collect", "ClearCollect",
                "UpdateContext", "Navigate",
            };

            foreach (var func in BuiltinFunctionsCore._library.FunctionNames.Union(BuiltinFunctionsCore.OtherKnownFunctions))
            {
                var script = $"{func}():Boolean = true;";
                var parseResult = UserDefinitions.Parse(script, parserOptions);
                var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
                errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());
                if (!restrictedUDFNames.Contains(func))
                {
                    Assert.True(errors.Count() == 0);
                }
                else
                {
                    Assert.Contains(errors, x => x.MessageKey == "ErrUDF_FunctionNameRestricted");
                }
            }
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(29, false)]
        [InlineData(30, false)]
        [InlineData(31, true)]
        [InlineData(1000, true)]
        [InlineData(10000, true)]
        public void TestUDFsBlockTooManyParameters(int count, bool errorExpected)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = true
            };

            var parameters = new List<string>();
            for (int i = 0; i < count; i++)
            {
                parameters.Add($"parameter{i}: Number");
            }

            string script = $"test({string.Join(", ", parameters)}):Number = 1;";

            var parseResult = UserDefinitions.Parse(script, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            if (errorExpected)
            {
                Assert.Contains(errors, x => x.MessageKey == "ErrUDF_TooManyParameters");
            }
            else
            {
                Assert.True(errors.Count == 0);
            }
        }

        [Theory]
        [InlineData("func():Void { Set(x, 123) };")]
        [InlineData("func():Void { Set(x, 123); Set(y, 123) };")]
        [InlineData("func():Void = { Set(x, 123) };")]
        [InlineData("func():Void = { Set(x, 123); Set(y, 123) };")]
        public void TestImperativeUDFParseWithoutSemicolon(string script)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = true,
            };

            var parseResult = UserDefinitions.Parse(script, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            Assert.True(errors.Count() == 0);
        }

        [Theory]
        [InlineData("Count():Number = 1;")]
        public void TestUDFHasWarningWhenShadowing(string script)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = true,
            };
            var nameResolver = ReadOnlySymbolTable.NewDefault(BuiltinFunctionsCore._library, FormulaType.PrimitiveTypes);

            var parseResult = UserDefinitions.Parse(script, parserOptions);
            var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), nameResolver, out var errors);
            errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

            // Only one error should exist.
            Assert.True(errors.Count() == 1 &&
                errors.Any(error => error.MessageKey == "WrnUDF_ShadowingBuiltInFunction" &&
                error.Severity == DocumentErrorSeverity.Warning));
        }

        [Fact]
        public void TestUDFRestrictedTypes()
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = true,
            };

            foreach (var type in UserDefinitions.RestrictedTypes)
            {
                var script = $"func():{type.GetKindString()} = Blank();";
                var parseResult = UserDefinitions.Parse(script, parserOptions);
                var udfs = UserDefinedFunction.CreateFunctions(parseResult.UDFs.Where(udf => udf.IsParseValid), _primitiveTypes, out var errors);
                errors.AddRange(parseResult.Errors ?? Enumerable.Empty<TexlError>());

                Assert.Contains(errors, x => x.MessageKey == "ErrUDF_UnknownType" || x.MessageKey == "ErrUDF_InvalidReturnType");
            }
        }
    }
}
