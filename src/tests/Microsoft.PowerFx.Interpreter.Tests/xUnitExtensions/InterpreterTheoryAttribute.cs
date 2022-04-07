﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions.InterpreterTheoryDiscoverer", "Microsoft.PowerFx.Interpreter.Tests")]
    public class InterpreterTheoryAttribute : TheoryAttribute
    {
        public InterpreterTheoryAttribute(params Type[] skippingExceptions)
        {
        }
    }
}
