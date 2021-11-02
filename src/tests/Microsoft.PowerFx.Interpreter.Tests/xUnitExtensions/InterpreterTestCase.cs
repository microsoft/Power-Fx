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
            this.SkippingExceptionNames = skippingExceptionNames;
        }

        /// <summary>
        /// Gets an array of the full names of the exception types which should be interpreted as a skipped test.
        /// </summary>
        internal string[] SkippingExceptionNames { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public InterpreterTestCase() { }

        public Exception InitializationException { get; set; }

        public IMethodInfo Method
        {
            get { return testCase.Method; }
        }

        public int Timeout { get; set; }

        public string DisplayName
        {
            get
            {
                return testCase.DisplayName;
            }
        }

        public string SkipReason
        {
            get
            {
                return testCase.SkipReason;
            }
        }

        public ISourceInformation SourceInformation
        {
            get { return testCase.SourceInformation; }
            set { testCase.SourceInformation = value; }
        }

        public ITestMethod TestMethod
        {
            get { return testCase.TestMethod; }
        }
        public object[] TestMethodArguments
        {
            get { return testCase.TestMethodArguments; }
        }

        public Dictionary<string, List<string>> Traits
        {
            get
            {
                ExpressionTestCase expressionTestCase = testCase.TestMethodArguments[0] as ExpressionTestCase;
                testCase.Traits.Add("File", new List<string>() {Path.GetFileName(expressionTestCase.SourceFile) });
                return testCase.Traits;
            }
        }

        public string UniqueID
        {
            get { return testCase.UniqueID; }
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            this.SkippingExceptionNames = info.GetValue<string[]>(nameof(this.SkippingExceptionNames));
            testCase = info.GetValue<IXunitTestCase>("InnerTestCase");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(this.SkippingExceptionNames), this.SkippingExceptionNames);
            info.AddValue("InnerTestCase", testCase);
        }

        public async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            var messageBusInterceptor = new SkippableTestMessageBus(messageBus, SkippingExceptionNames != null ? SkippingExceptionNames : new string[1] { "Xunit.SkipException" });
            var result = await this.testCase.RunAsync(diagnosticMessageSink, messageBusInterceptor, constructorArguments, aggregator, cancellationTokenSource).ConfigureAwait(false);
            result.Failed -= messageBusInterceptor.SkippedCount;
            result.Skipped += messageBusInterceptor.SkippedCount;
            return result;

        }
    }
}
