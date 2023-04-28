// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Interpreter.UDF;
using Microsoft.PowerFx.Types;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class RegExTests
    {
        [Fact]
        public void TestRegExNegativeTimeout()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                PowerFxConfig powerFxConfig = new PowerFxConfig();
                powerFxConfig.EnableRegExFunctions(TimeSpan.FromMilliseconds(-1));
            });            
        }

        [Fact]
        public void TestRegExNegativeCacheSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                PowerFxConfig powerFxConfig = new PowerFxConfig();
                powerFxConfig.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), -2);
            });
        }

        [Fact]
        public void TestRegExEnableTwice()
        {
            PowerFxConfig powerFxConfig = new PowerFxConfig();
            powerFxConfig.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 20);
            Assert.Throws<InvalidOperationException>(() => powerFxConfig.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 50));
        }

        [Fact]
        public void TestRegExEnableTwice2()
        {
            PowerFxConfig powerFxConfig = new PowerFxConfig();
            powerFxConfig.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 20);

            PowerFxConfig powerFxConfig2 = new PowerFxConfig();
            powerFxConfig2.EnableRegExFunctions(TimeSpan.FromMilliseconds(50), 20);
        }
    }
}
