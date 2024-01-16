// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class FunctionInfoTests
    {
        [Fact]
        public void FunctionInfo()
        {
            // Get any function info. 
            var engine = new Engine();
            var infos = engine.FunctionInfos.ToArray();
            var infoMid = infos.Where(info => info.Name == "Mid").First();

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
        public void LocaleTests()
        {
            var engine = new Engine();
            var infos = engine.FunctionInfos.ToArray();
            var infoMid = infos.Where(info => info.Name == "Mid").First();
            
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
    }
}
