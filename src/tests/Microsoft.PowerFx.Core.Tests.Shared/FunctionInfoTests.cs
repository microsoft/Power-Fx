﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class FunctionInfoTests
    {
        private FunctionInfo GetMid()
        {
            // Get any function info. 
            var engine = new Engine();
            var infos = engine.FunctionInfos.ToArray();
            var infoMid = infos.Where(info => info.Name == "Mid").First();

            return infoMid;
        }

        [Fact]
        public void FunctionInfo()
        {
            var infoMid = GetMid();

            Assert.Equal("Returns the characters from the middle of a text value, given a starting position and length.", infoMid.GetDescription(null));
            Assert.Equal(2, infoMid.MinArity);
            Assert.Equal(3, infoMid.MaxArity);
            Assert.Equal("Mid", infoMid.Name);
            Assert.Equal("https://go.microsoft.com/fwlink/?LinkId=722347#m", infoMid.HelpLink);

            var sigs = infoMid.Signatures.ToArray();
            Assert.Equal(2, sigs.Length);
            Assert.Equal("Mid(text, start_num)", sigs[0].DebugToString());
            Assert.Equal("Mid(text, start_num, num_chars)", sigs[1].DebugToString());
        }

        [Fact]
        public void ParameterInfo()
        {
            var infoMid = GetMid();

            var sig = infoMid.Signatures.First();
            Assert.Equal("Mid(text, start_num)", sig.DebugToString());

            var parameters = sig.GetParameters();
            Assert.Equal(2, parameters.Length);

            Assert.Equal("text", parameters[0].Name);

            // This may need to be updated if resx changes. 
            Assert.Equal("A text value from which characters will be extracted.", parameters[0].Description);

            Assert.Equal("start_num", parameters[1].Name);
        }

        [Fact]
        public void ParameterInfoLocalized()
        {
            var infoMid = GetMid();

            var sig = infoMid.Signatures.First();
            Assert.Equal("Mid(text, start_num)", sig.DebugToString());

            var parameters = sig.GetParameters(CultureInfo.CreateSpecificCulture("pt-BR"));

            Assert.Equal(2, parameters.Length);
            Assert.Equal("texto", parameters[0].Name);
            Assert.Equal("Valor de texto do qual os caracteres serão extraídos.", parameters[0].Description);
            Assert.Equal("núm_inicial", parameters[1].Name);
        }

        [Fact]
        public void LocaleTests()
        {
            var infoMid = GetMid();

            CultureInfo fr = new CultureInfo("fr-FR");
            fr.RunOnIsolatedThread((culture) =>
            {
                // Ensure it does not use current culture
                var descr = infoMid.GetDescription(null);
                Assert.StartsWith("Returns the ", descr); // null is invariant, is english. 
            });

            var descr2 = infoMid.GetDescription(null);
            Assert.StartsWith("Returns the ", descr2); // null is invariant, is english. 

            // Now explicitly ask for another locale. 
            var descr3 = infoMid.GetDescription(fr);
            Assert.StartsWith("Retourne les", descr3); // in french
        }

        [Theory]
        [InlineData("Boolean", "Text", "Texto")]
        [InlineData("Sqrt", "Math and statistical", "Matemática e estatística")]
        [InlineData("ConcatenateT", "Table", "Tabela", "Text", "Texto")]
        public void FunctionCategoriesLocaleTest(string functionName, params string[] localizedResources)
        {
            var locale_enus = CultureInfo.CreateSpecificCulture("en-US");
            var locale_ptbr = CultureInfo.CreateSpecificCulture("pt-BR");

            // Picked some fixed examples to test.Concatenate is a good example of a function that is in multiple categories.
            var funcDict = new Dictionary<string, TexlFunction>
            {
                { "Boolean", new BooleanFunction() },
                { "Sqrt", new SqrtFunction() },
                { "ConcatenateT", new ConcatenateTableFunction() }
            };

            funcDict.TryGetValue(functionName, out var func);

            var info = new FunctionInfo(func);
            var categoryNames_enus = info.GetCategoryNames(locale_enus);
            var categoryNames_ptbr = info.GetCategoryNames(locale_ptbr);
            var enumerator = localizedResources.GetEnumerator();

            foreach (FunctionCategories category in Enum.GetValues(typeof(FunctionCategories)))
            {
                if ((func.FunctionCategoriesMask & category) != 0)
                {
                    enumerator.MoveNext();
                    Assert.Contains(enumerator.Current, categoryNames_enus);

                    enumerator.MoveNext();
                    Assert.Contains(enumerator.Current, categoryNames_ptbr);
                }
            }
        }
    }
}
