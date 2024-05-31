// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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

namespace Microsoft.PowerFx.Interpreter.Tests.XUnitExtensions
{
    public class InterpreterTestCase : LongLivedMarshalByRefObject, IXunitTestCase
    {
        private IXunitTestCase _testCase;

        public InterpreterTestCase(IXunitTestCase testCase, string[] skippingExceptionNames)
        {
            this._testCase = testCase;
            SkippingExceptionNames = skippingExceptionNames;
        }

        /// <summary>
        /// Gets an array of the full names of the exception types which should be interpreted as a skipped test.
        /// </summary>
        internal string[] SkippingExceptionNames { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public InterpreterTestCase()
        {
        }

        public Exception InitializationException { get; set; }

        public IMethodInfo Method => _testCase.Method;

        public int Timeout { get; set; }

        public string DisplayName => _testCase.DisplayName;

        public string SkipReason => _testCase.SkipReason;

        public ISourceInformation SourceInformation
        {
            get => _testCase.SourceInformation;
            set => _testCase.SourceInformation = value;
        }

        public ITestMethod TestMethod => _testCase.TestMethod;

        public object[] TestMethodArguments => _testCase.TestMethodArguments;

        public Dictionary<string, List<string>> Traits
        {
            get
            {
                var expressionTestCase = _testCase.TestMethodArguments[0] as ExpressionTestCase;
                _testCase.Traits.Add("File", new List<string>() { Path.GetFileName(expressionTestCase.SourceFile) });
                return _testCase.Traits;
            }
        }

        public string UniqueID => _testCase.UniqueID;

        public void Deserialize(IXunitSerializationInfo info)
        {
            SkippingExceptionNames = info.GetValue<string[]>(nameof(SkippingExceptionNames));
            _testCase = info.GetValue<IXunitTestCase>("InnerTestCase");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(SkippingExceptionNames), SkippingExceptionNames);
            info.AddValue("InnerTestCase", _testCase);
        }

        public async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            var messageBusInterceptor = new SkippableTestMessageBus(messageBus, SkippingExceptionNames ?? (new string[1] { "Xunit.SkipException" }));
            var result = await _testCase.RunAsync(diagnosticMessageSink, messageBusInterceptor, constructorArguments, aggregator, cancellationTokenSource).ConfigureAwait(false);
            result.Failed -= messageBusInterceptor.SkippedCount;
            result.Skipped += messageBusInterceptor.SkippedCount;
            return result;
        }
    }
}
