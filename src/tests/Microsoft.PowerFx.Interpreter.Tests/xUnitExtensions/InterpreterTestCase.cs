// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.PowerFx.Interpreter.Tests.xUnitExtensions
{
    public class InterpreterTestCase : LongLivedMarshalByRefObject, IXunitTestCase
    {
        private IXunitTestCase testCase;

        public InterpreterTestCase(IXunitTestCase testCase, string[] skippingExceptionNames)
        {
            this.testCase = testCase;
            SkippingExceptionNames = skippingExceptionNames;
        }

        /// <summary>
        /// Gets an array of the full names of the exception types which should be interpreted as a skipped test.
        /// </summary>
        internal string[] SkippingExceptionNames { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public InterpreterTestCase() { }

        public Exception InitializationException { get; set; }

        public IMethodInfo Method => testCase.Method;

        public int Timeout { get; set; }

        public string DisplayName => testCase.DisplayName;

        public string SkipReason => testCase.SkipReason;

        public ISourceInformation SourceInformation
        {
            get => testCase.SourceInformation;
            set => testCase.SourceInformation = value;
        }

        public ITestMethod TestMethod => testCase.TestMethod;
        public object[] TestMethodArguments => testCase.TestMethodArguments;

        public Dictionary<string, List<string>> Traits
        {
            get
            {
                var expressionTestCase = testCase.TestMethodArguments[0] as ExpressionTestCase;
                testCase.Traits.Add("File", new List<string>() { Path.GetFileName(expressionTestCase.SourceFile) });
                return testCase.Traits;
            }
        }

        public string UniqueID => testCase.UniqueID;

        public void Deserialize(IXunitSerializationInfo info)
        {
            SkippingExceptionNames = info.GetValue<string[]>(nameof(SkippingExceptionNames));
            testCase = info.GetValue<IXunitTestCase>("InnerTestCase");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(SkippingExceptionNames), SkippingExceptionNames);
            info.AddValue("InnerTestCase", testCase);
        }

        public async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            var messageBusInterceptor = new SkippableTestMessageBus(messageBus, SkippingExceptionNames ?? (new string[1] { "Xunit.SkipException" }));
            var result = await testCase.RunAsync(diagnosticMessageSink, messageBusInterceptor, constructorArguments, aggregator, cancellationTokenSource).ConfigureAwait(false);
            result.Failed -= messageBusInterceptor.SkippedCount;
            result.Skipped += messageBusInterceptor.SkippedCount;
            return result;

        }
    }
}
