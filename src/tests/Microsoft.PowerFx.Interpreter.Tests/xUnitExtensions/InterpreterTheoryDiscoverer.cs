// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions
{
    public class InterpreterTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly TheoryDiscoverer _theoryDiscoverer;

        public InterpreterTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _theoryDiscoverer = new TheoryDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var firstArgument = (object[])factAttribute.GetConstructorArguments().FirstOrDefault();
            var skippingExceptions = firstArgument?.Cast<Type>().ToArray() ?? Type.EmptyTypes;
            Array.Resize(ref skippingExceptions, skippingExceptions.Length + 1);
            skippingExceptions[skippingExceptions.Length - 1] = typeof(SkipException);

            var skippingExceptionNames = skippingExceptions.Select(ex => ex.FullName).ToArray();

            return _theoryDiscoverer.Discover(discoveryOptions, testMethod, factAttribute)
                                    .Select(testCase => new InterpreterTestCase(testCase, skippingExceptionNames));
        }
    }
}
