// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class NamedFormulasTests : PowerFxTest
    {
        [Theory]
        [InlineData("Foo = Type(Number);")]
        public void DefSimpleTypeTest(string script)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parsedNamedFormulasAndUDFs = UserDefinitions.Parse(script, parserOptions);
            Assert.False(parsedNamedFormulasAndUDFs.HasErrors);
            Assert.Equal("Number", parsedNamedFormulasAndUDFs.DefinedTypes.First().Type.TypeRoot.AsFirstName().Ident.Name.ToString());
            Assert.Equal("Foo", parsedNamedFormulasAndUDFs.DefinedTypes.First().Ident.Name.ToString());
        }

        [Theory]
        [InlineData("Foo = Type({ Age: Number });")]
        public void DefRecordTypeTest(string script)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parsedNamedFormulasAndUDFs = UserDefinitions.Parse(script, parserOptions);
            Assert.False(parsedNamedFormulasAndUDFs.HasErrors);
            var record = parsedNamedFormulasAndUDFs.DefinedTypes.First().Type.TypeRoot.AsRecord();
            Assert.Equal("Age", record.Ids.First().Name.ToString());
            Assert.Equal("Number", record.ChildNodes.First().AsFirstName().ToString());
        }

        [Theory]
        [InlineData("Foo(x: Number): Number = Abs(x);")]
        public void DefFuncTest(string script)
        {
            var parsedUDFS = new ParsedUDFs(script);
            var result = parsedUDFS.GetParsed();
            Assert.False(result.HasError);
            var udf = result.UDFs.First();
            Assert.Equal("Foo", udf.Ident.ToString());
            Assert.Equal("Abs(x)", udf.Body.ToString());
            Assert.Equal("Number", udf.ReturnType.ToString());
            var arg = udf.Args.First();
            Assert.Equal("x", arg.NameIdent.ToString());
            Assert.Equal("Number", arg.TypeIdent.ToString());
        }

        [Theory]
        [InlineData("Rec4(x: Number): Number { { force: 1, goo: x } };" +
                    "Rec5(x: Number): Number { \"asfd\"; { force: 1, goo: x } };" +
                    "Rec6(x: Number): Number = x + 1;" +
                    "Rec7(x: Number): Number { x + 1 };")]
        public void DefFunctionFromDiscussion(string script)
        {
            var parsedUDFs = new ParsedUDFs(script);
            var result = parsedUDFs.GetParsed();
            Assert.False(result.HasError);
        }

        [Theory]
        [InlineData("Rec4\n(x\n: \nNumber\n)\n: \nNumber \n \n \n{\n \n{ force: 1, goo: x }\n \n}\n;\n" +
                    "Rec5      (     x     :      Number    )    :    Number       {     \"asfd\";     { force: 1, goo: x }     }     ;    " +
                    "Rec6/*comment*/(/*comment*/x/*comment*/:/*comment*/ Number/*comment*/)/*comment*/:/*comment*/ Number/*comment*/ =/*comment*/ x/*comment*/ + 1/*comment*/;/*comment*/" +
                    "Rec7//comment\n(//comment\nx//comment\n://comment\n Number//comment\n)://comment\n Number//comment\n //comment\n { x + 1 }//comment\n;")]
        public void DefFunctionWeirdFormatting(string script)
        {
            var parsedUDFs = new ParsedUDFs(script);
            var result = parsedUDFs.GetParsed();
            Assert.False(result.HasError);
        }

        [Theory]
        [InlineData("Foo(): Number { 1+1; 2+2; };")]
        public void TestChaining(string script)
        {
            var parsedUDFs = new ParsedUDFs(script);
            var result = parsedUDFs.GetParsed();

            Assert.False(result.HasError);
            var udf = result.UDFs.First();
            Assert.Equal("Foo", udf.Ident.ToString());
            Assert.Equal("1 + 1 ; 2 + 2", udf.Body.ToString());
        } 

        [Theory]
        [InlineData("Foo(): Number { Sum(1, 1); Sum(2, 2); };")]
        public void TestChaining2(string script)
        {
            var parsedUDFs = new ParsedUDFs(script);
            var result = parsedUDFs.GetParsed();

            Assert.False(result.HasError);
            var udf = result.UDFs.First();
            Assert.Equal("Foo", udf.Ident.ToString());
            Assert.Equal("Sum(1, 1) ; Sum(2, 2)", udf.Body.ToString());
        }

        [Theory]
        [InlineData("Foo(): Number {// comment \nSum(1, 1); Sum(2, 2); };Bar(): Number {Foo();};x=1;y=2;", 0, 0, true)]
        [InlineData("Foo(x: /*comment\ncomment*/Number):/*comment*/Number = /*comment*/Abs(x);", 0, 1, false)]
        [InlineData("x", 0, 0, true)]
        [InlineData("x=", 0, 0, true)]
        [InlineData("x=1", 1, 0, true)]
        [InlineData("x=1;", 1, 0, false)]
        [InlineData("x=1;Foo(", 1, 0, true)]
        [InlineData("x=1;Foo(x", 1, 0, true)]
        [InlineData("x=1;Foo(x:", 1, 0, true)]
        [InlineData("x=1;Foo(x:Number", 1, 0, true)]
        [InlineData("x=1;Foo(x:Number)", 1, 0, true)]
        [InlineData("x=1;Foo(x:Number):", 1, 0, true)]
        [InlineData("x=1;Foo(x:Number):Number", 1, 0, true)]
        [InlineData("x=1;Foo(x:Number):Number = ", 1, 1, true)]
        [InlineData("x=1;Foo(x:Number):Number = 10 * x", 1, 1, true)]
        [InlineData("x=1;Foo(x:Number):Number = 10 * x;", 1, 1, false)]
        [InlineData("x=1;Foo(:Number):Number = 10 * x;", 1, 0, true)]
        public void NamedFormulaAndUdfTest(string script, int namedFormulaCount, int udfCount, bool expectErrors)
        {
            var parserOptions = new ParserOptions()
            {
                AllowsSideEffects = false
            };

            var parsedNamedFormulasAndUDFs = UserDefinitions.Parse(script, parserOptions);

            Assert.Equal(namedFormulaCount, parsedNamedFormulasAndUDFs.NamedFormulas.Count());
            Assert.Equal(udfCount, parsedNamedFormulasAndUDFs.UDFs.Count());
            Assert.Equal(expectErrors, parsedNamedFormulasAndUDFs.HasErrors);
        }

        [Theory]
        [InlineData("x=1;y=2;")]
        public void NamedFormulaTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            Assert.Equal(script, namedFormula.Script);
        }

        [Theory]
        [InlineData("x=1;y=2;", 2)]
        public void EnsureParsedTest(string script, int count)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            Assert.NotNull(formulas);
            Assert.Equal(formulas.Count(), count);
        }

        [Theory]
        [InlineData("x=")]
        public void EnsureParsedWithErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            Assert.Empty(formulas);            
        }

        [Theory]
        [InlineData("x=")]
        public void GetParseErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();

            var errors = namedFormula.GetParseErrors();
            Assert.NotEmpty(errors);
        }

        [Theory]
        [InlineData("x=1;")]
        public void GetParseErrorsNoErrorsTest(string script)
        {
            var namedFormula = new NamedFormulas(script);
            namedFormula.EnsureParsed();

            var errors = namedFormula.GetParseErrors();
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("x=1;y=2;", "1", "1", "2", "2")]
        [InlineData("x=1.00000000000000000000000001;y=2.00000000000000000000000001;", "1", "1.00000000000000000000000001", "2", "2.00000000000000000000000001")]
        [InlineData("x=1e-100;y=1e-100;", "1E-100", "1e-100", "1E-100", "1e-100")]
        public void GetNamedFormulasTest(string script, string expectedX, string scriptX, string expectedY, string scriptY)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed(TexlParser.Flags.NumberIsFloat);
            formulas.OrderBy(formula => formula.formula.Script);

            Assert.NotNull(formulas);

            Assert.Equal(expectedX, formulas.ElementAt(0).formula.ParseTree.ToString());
            Assert.Equal(expectedY, formulas.ElementAt(1).formula.ParseTree.ToString());

            Assert.Equal(NodeKind.NumLit, formulas.ElementAt(0).formula.ParseTree.Kind);
            Assert.Equal(NodeKind.NumLit, formulas.ElementAt(1).formula.ParseTree.Kind);

            Assert.Equal(scriptX, formulas.ElementAt(0).formula.Script);
            Assert.Equal(scriptY, formulas.ElementAt(1).formula.Script);
        }

        [Theory]
        [InlineData("x=1;y=2;", "1", "1", "2", "2")]
        [InlineData("x=1.00000000000000000000000001;y=2.00000000000000000000000001;", "1.00000000000000000000000001", "1.00000000000000000000000001", "2.00000000000000000000000001", "2.00000000000000000000000001")]
        [InlineData("x=1e-100;y=1e-100;", "0", "1e-100", "0", "1e-100")]
        public void GetNamedFormulasTest_Decimal(string script, string expectedX, string scriptX, string expectedY, string scriptY)
        {
            var namedFormula = new NamedFormulas(script);
            var formulas = namedFormula.EnsureParsed();
            formulas.OrderBy(formula => formula.formula.Script);

            Assert.NotNull(formulas);

            Assert.Equal(expectedX, formulas.ElementAt(0).formula.ParseTree.ToString());
            Assert.Equal(expectedY, formulas.ElementAt(1).formula.ParseTree.ToString());

            Assert.Equal(NodeKind.DecLit, formulas.ElementAt(0).formula.ParseTree.Kind);
            Assert.Equal(NodeKind.DecLit, formulas.ElementAt(1).formula.ParseTree.Kind);

            Assert.Equal(scriptX, formulas.ElementAt(0).formula.Script);
            Assert.Equal(scriptY, formulas.ElementAt(1).formula.Script);
        }
    }
}
