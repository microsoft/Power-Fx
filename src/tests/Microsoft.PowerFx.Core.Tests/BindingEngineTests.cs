﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Tests
{
    public class BindingEngineTests : PowerFxTest
    {
        [Fact]
        public void CheckSuccess()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var result = engine.Check(
                "3*2+x",
                RecordType.Empty().Add(
                    new NamedFormulaType("x", FormulaType.Number)));

            Assert.True(result.IsSuccess);
            Assert.True(result.ReturnType is NumberType);
            Assert.Single(result.TopLevelIdentifiers);
            Assert.Equal("x", result.TopLevelIdentifiers.First());
        }

        // Parse and Bind separately. 
        [Fact]
        public void CheckParseSuccess()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var parse = engine.Parse("3*x");

            Assert.False(parse.HasError);
            Assert.Empty(parse.Errors);

            Assert.NotNull(parse.Root);

            var str = parse.Root.ToString();
            Assert.Equal("3 * x", str);

            var r = RecordType.Empty().Add(
                   new NamedFormulaType("x", FormulaType.Number));

            var check = engine.Check(parse, r);
            Assert.True(check.IsSuccess);

            // Can reuse Parse 
            var check2 = engine.Check(parse, r);
            Assert.True(check2.IsSuccess);
        }

        // Parse and Bind separately. 
        [Fact]
        public void CheckChainingParseSuccess()
        {
            var opts = new ParserOptions
            {
                AllowsSideEffects = true
            };

            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var parse = engine.Parse("a;b;c", opts);

            Assert.False(parse.HasError);
            Assert.Empty(parse.Errors);

            Assert.NotNull(parse.Root);

            var str = parse.Root.ToString();
            Assert.Equal("a ; b ; c", str);
        }

        [Fact]
        public void CheckParseOnlyError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Parse("3*1+");

            Assert.True(result.HasError);
            Assert.Single(result.Errors);

            AssertContainsError(result, "Error 4-4: Expected an operand");
        }

        [Fact]
        public void CheckParseError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("3*1+");

            Assert.False(result.IsSuccess);
            Assert.True(result.Errors.Count() >= 1);
            AssertContainsError(result, "Error 4-4: Expected an operand");
        }

        [Fact]
        public void CheckParseErrorCommaSeparatedLocale()
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Parse("3.145", new ParserOptions(CultureInfo.GetCultureInfo("it-IT")));

            Assert.False(result.IsSuccess);
            Assert.StartsWith("Error 2-5: Caratteri non previsti", result.Errors.First().ToString());
        }

        [Fact]
        public void CheckParseSuccessCommaSeparatedLocale()
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Parse("Function(args; separated; by; semicolons) + 123,456", options: new ParserOptions(CultureInfo.GetCultureInfo("de-DE")));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckParseSuccessCommaSeparatedLocaleUsingStatic()
        {
            var result = Engine.Parse("Function(args; separated; by; semicolons) + 123,456", options: new ParserOptions(CultureInfo.GetCultureInfo("de-DE")));

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void CheckNullRef()
        {
            var engine = new Engine(new PowerFxConfig());
            Assert.Throws<ArgumentNullException>(() => engine.Check((string)null));
        }

        [Fact]
        public void CheckBindError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("3+foo+2"); // foo is undefined

            Assert.False(result.IsSuccess);
            AssertContainsError(result, "Error 2-5: Name isn't valid. 'foo' isn't recognized");
        }

        [Theory]

        // Binding errors
        [InlineData("3+foo+2", "Error 2-5: Il nome non è valido. \"foo\" non riconosciuto.", "it-IT")]
        [InlineData("Foo()", "Error 0-5: 'Foo' est une fonction inconnue ou non prise en charge.", "fr-FR")]
        [InlineData("AAA", "Error 0-3: O nome não é válido. 'AAA' não é reconhecido.", "pt-BR")]
        [InlineData("Bar()", "Error 0-5: \"Bar\" — неизвестная или неподдерживаемая функция.", "ru-RU")]
        [InlineData("Table({a:BB})", "Error 9-11: Name isn't valid. 'BB' isn't recognized.", "en-US")]

        // Parse errors
        [InlineData("2e.5", "Error 1-2: È previsto un operatore. A questo punto della formula è previsto un operatore, ad esempio +, * o &.", "it-IT")]
        [InlineData(".2.3", "Error 0-1: Caractères inattendus. Des caractères sont utilisés dans la formule de manière inattendue.", "fr-FR")]
        [InlineData("2EEE5", "Error 1-5: Operador esperado. Esperamos um operador como +, * ou & neste ponto na fórmula.", "pt-BR")]
        [InlineData("7E1111111", "Error 0-9: Numerická hodnota je príliš veľká.", "sk-SK")]
        [InlineData("4E88888", "Error 0-7: Numeric value is too large.", "en-US")]
        public void CheckBindError2(string expression, string expected, string locale)
        {
            var engine = new Engine(new PowerFxConfig());
            var result = engine.Check(expression, new ParserOptions(CultureInfo.GetCultureInfo(locale)));

            Assert.False(result.IsSuccess);
            AssertContainsError(result, expected);
        }

        [Fact]
        public void CheckBadFunctionError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("abc(123)");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 0-8: 'abc' is an unknown or unsupported function.");
        }

        [Fact]
        public void CheckBadNamespaceFunctionError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("abc.def(123)");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 0-12: 'def' is an unknown or unsupported function in namespace 'abc'.");
        }

        [Fact]
        public void CheckLambdaBindError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("Filter([1,2,3] As X, X.Value > foo)");

            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 31-34: Name isn't valid. 'foo' isn't recognized");
        }

        [Fact]
        public void CheckDottedBindError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("First([1,2,3]).foo");
            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 14-18: Name isn't valid. 'foo' isn't recognized.");
        }

        [Fact]
        public void CheckDottedBindErrorForSingleColumnAccess()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var result = engine.Check("[1,2,3].foo");
            Assert.False(result.IsSuccess);
            Assert.Single(result.Errors);
            AssertContainsError(result, "Error 7-11: Deprecated use of '.'. Please use the 'ShowColumns' function instead.");
        }

        [Fact]
        public void TableInRegressionError()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);
            var formula = "Table() in \"\"";
            var result = engine.Check(formula);

            Assert.False(result.IsSuccess);
            AssertContainsError(result, "Error 0-7: Invalid argument type. Cannot use Table values in this context.");
        }

        [Fact]
        public void CheckParseResultSideEffects()
        {
            var config = new PowerFxConfig();
            config.AddFunction(new BehaviorFunction());

            var engine = new Engine(config);
            var formula = "Behavior(); Behavior()";
            var options = new ParserOptions { AllowsSideEffects = true };

            var result1 = engine.Check(formula, options: options);
            Assert.True(result1.IsSuccess);

            var parseResult2 = engine.Parse(formula, options);
            var result2 = engine.Check(parseResult2);
            Assert.True(result2.IsSuccess);
        }

        [Fact]
        public void CheckRecursiveCustomType()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var lazyTypeInstance = new LazyRecursiveRecordType();

            var result = engine.Check(
                "Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop" +
                ".Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop" +
                ".Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop" +
                ".Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop.Loop", lazyTypeInstance);

            Assert.True(result.IsSuccess);
            Assert.IsType<LazyRecursiveRecordType>(result.ReturnType);
            Assert.Equal(lazyTypeInstance, result.ReturnType);

            // We never needed to iterate the fields of the lazy type
            Assert.False(lazyTypeInstance.EnumerableIterated);
        }

        [Fact]
        public void CheckFunctionsWithColumnIdentifierAndLambda_OneFunction()
        {
            var config = new PowerFxConfig();
            var gotException = false;

            try
            {
                config.AddFunction(new LambdaAndColumnIdentifierFunction());
            }
            catch (ArgumentException ex)
            {
                Assert.Equal("This function is ambiguous, it contains lambda expressions and column identifiers for the same argument.", ex.Message);
                gotException = true;
            }
            finally
            {
                Assert.True(gotException);
            }
        }

        [Fact]
        public void CheckFunctionsWithColumnIdentifierAndLambda_Overloads_TwoFunctions()
        {
            var funcs = new TexlFunction[] { new LambdaFunction(0x1), new ColumnIdentifierFunction(0x1) };

            for (var i = 0; i < 2; i++)
            {
                var config = new PowerFxConfig();
                var gotException = false;

                config.AddFunction(funcs[i % 2]);

                try
                {
                    config.AddFunction(funcs[(i + 1) % 2]);
                }
                catch (ArgumentException ex)
                {
                    Assert.Equal("This function is ambiguous, it contains lambda expressions and column identifiers for the same argument.", ex.Message);
                    gotException = true;
                }
                finally
                {
                    Assert.True(gotException);
                }
            }
        }

        [Fact]
        public void CheckFunctionsWithColumnIdentifierAndLambda_Overloads_NoConflict()
        {
            var funcs = new TexlFunction[] { new LambdaFunction(0x1), new ColumnIdentifierFunction(0x2) };

            for (var i = 0; i < 2; i++)
            {
                var config = new PowerFxConfig();                

                config.AddFunction(funcs[i % 2]);
                config.AddFunction(funcs[(i + 1) % 2]);                
            }
        }

        [Fact]
        public void CheckFunctionsWithColumnIdentifierAndLambda_Overloads_NoConflict2()
        {
            var funcs = new TexlFunction[] { new LambdaFunction(0x1), new ColumnIdentifierFunction(0x2), new LambdaFunction(0x4), new ColumnIdentifierFunction(0x8) };

            for (var i = 0; i < 4; i++)
            {
                var config = new PowerFxConfig();

                config.AddFunction(funcs[i % 4]);
                config.AddFunction(funcs[(i + 1) % 4]);
                config.AddFunction(funcs[(i + 2) % 4]);
                config.AddFunction(funcs[(i + 3) % 4]);
            }
        }

        [Fact]
        public void CheckFunctionsWithColumnIdentifierAndLambda_Overloads_Conflict()
        {
            var funcs = new TexlFunction[] { new LambdaFunction(0x1), new ColumnIdentifierFunction(0x2), new ColumnIdentifierFunction(0x1) };

            for (var i = 0; i < 3; i++)
            {
                var config = new PowerFxConfig();
                var gotException = false;                

                try
                {
                    config.AddFunction(funcs[i % 3]);
                    config.AddFunction(funcs[(i + 1) % 3]);
                    config.AddFunction(funcs[(i + 2) % 3]);
                }
                catch (ArgumentException ex)
                {
                    Assert.Equal("This function is ambiguous, it contains lambda expressions and column identifiers for the same argument.", ex.Message);
                    gotException = true;
                }
                finally
                {
                    Assert.True(gotException);
                }
            }
        }

        internal class LazyRecursiveRecordType : RecordType
        {
            public override IEnumerable<string> FieldNames => GetFieldNames();

            public bool EnumerableIterated = false;

            public LazyRecursiveRecordType()
                : base()
            {
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                switch (name)
                {
                    case "SomeString":
                        type = FormulaType.String;
                        return true;
                    case "Loop":
                        type = this;
                        return true;
                    default:
                        type = FormulaType.Blank;
                        return false;
                }
            }

            private IEnumerable<string> GetFieldNames()
            {
                EnumerableIterated = true;

                yield return "SomeString";
                yield return "Loop";
            }

            public override bool Equals(object other)
            {
                return other is LazyRecursiveRecordType; // All the same 
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }

        [Fact]
        public void CheckTypeUnionLazy()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var lazyTypeInstance = new LazyRecursiveRecordType();

            var result = engine.Check("First(Table(Loop, {A: SomeString}))", lazyTypeInstance);

            Assert.True(result.IsSuccess);
            Assert.IsType<KnownRecordType>(result.ReturnType);

            Assert.Equal("![A:s, Loop:r!, SomeString:s]", result.ReturnType._type.ToString());

            // Union operations require iterating fields
            Assert.True(lazyTypeInstance.EnumerableIterated);
        }

        [Fact]
        public void CheckShuffleLazyTable()
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            var lazyTypeInstance = new LazyRecursiveRecordType().ToTable();

            var result = engine.Check("Shuffle(Table)", RecordType.Empty().Add("Table", lazyTypeInstance));
            Assert.True(result.IsSuccess);

            var tableType = Assert.IsType<TableType>(result.ReturnType);
            Assert.IsType<LazyRecursiveRecordType>(tableType.ToRecord());
        }

        /// <summary>
        /// A function with behavior/side-effects used in testing.
        /// </summary>
        internal class BehaviorFunction : TexlFunction
        {
            public BehaviorFunction()
                : base(
                      DPath.Root,
                      "Behavior",
                      "Behavior",
                      TexlStrings.AboutSet, // just to add something
                      FunctionCategories.Behavior,
                      DType.Boolean,
                      0, // no lambdas
                      0, // no args
                      0)
            {
            }

            public override bool IsSelfContained => false;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }
        }

        // Example of function that requires AI disclaimer.
        internal class AISummarizeFunction : TexlFunction
        {
            public AISummarizeFunction()
                : base(
                      DPath.Root,
                      "AISummarize",
                      "AISummarize",
                      TexlStrings.AboutSet, // just to add something
                      FunctionCategories.Information,
                      DType.Boolean,
                      0, // no lambdas
                      0, // no args
                      0)
            {
            }

            public override bool IsSelfContained => false;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }
        }

        internal class LambdaAndColumnIdentifierFunction : TexlFunction
        {
            public LambdaAndColumnIdentifierFunction()
                : base(DPath.Root, "LambdaAndColumnIdentifierFunction", "LambdaAndColumnIdentifierFunction", TexlStrings.AboutSet, FunctionCategories.Text, DType.Boolean, 0x1, 1, 1)
            {
            }

            public override bool HasLambdas => true;

            public override bool HasColumnIdentifiers => true;

            public override bool IsSelfContained => false;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }

            public override bool IsLambdaParam(TexlNode node, int index)
            {
                return true;
            }

            public override ParamIdentifierStatus GetIdentifierParamStatus(TexlNode node, Features features, int index)
            {
                return ParamIdentifierStatus.AlwaysIdentifier;
            }
        }

        internal class LambdaFunction : TexlFunction
        {
            private readonly int _mask = 0;

            public LambdaFunction(int mask)
                : base(DPath.Root, "TestFunction1", "TestFunction1", TexlStrings.AboutSet, FunctionCategories.Text, DType.Boolean, mask, 1, 1)
            {
                _mask = mask;
            }

            public override bool HasLambdas => true;

            public override bool HasColumnIdentifiers => false;

            public override bool IsSelfContained => false;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }

            public override bool IsLambdaParam(TexlNode node, int index)
            {
                return (_mask & (1 << index)) != 0;
            }
        }

        internal class ColumnIdentifierFunction : TexlFunction
        {
            private readonly int _mask = 0;

            public ColumnIdentifierFunction(int mask)
                : base(DPath.Root, "TestFunction1", "TestFunction1", TexlStrings.AboutSet, FunctionCategories.Text, DType.Boolean, 0x0, 1, 1)
            {
                _mask = mask;
            }

            public override bool HasLambdas => false;

            public override bool HasColumnIdentifiers => true;

            public override bool IsSelfContained => false;

            public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
            {
                yield break;
            }

            public override ParamIdentifierStatus GetIdentifierParamStatus(TexlNode node, Features features, int index)
            {
                return (_mask & (1 << index)) != 0 ? ParamIdentifierStatus.AlwaysIdentifier : ParamIdentifierStatus.NeverIdentifier;
            }
        }

        private void AssertContainsError(IOperationStatus result, string errorMessage)
        {
            Assert.Contains(result.Errors, x => x.ToString().StartsWith(errorMessage));
        }
    }
}
