// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.Tests.Helpers;
using Microsoft.PowerFx.Logging;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public sealed class LoggingTest : IDisposable
    {
        private readonly TestLogger _logger = new TestLogger();

        public LoggingTest()
        {
            Log.SetLogger(_logger);
        }

        public void Dispose()
        {
            Log.TestOnly_Reset();
        }

        [Fact]
        public void TraceTest()
        {
            var trackName = string.Empty;
            var trackCustomDimensions = string.Empty;

            _logger.TrackTestEvent += (caller, args) =>
            {
                trackName = args.eventName;
                trackCustomDimensions = args.customDimensions;
            };

            Log.Instance.Track("WithArgs", new { SomeField = "foo" });
            Assert.Equal("WithArgs", trackName);
            Assert.Equal(@"{""SomeField"":""foo""}", trackCustomDimensions);
            
            trackName = trackCustomDimensions = string.Empty;

            Log.Instance.Track("WithoutArgs");
            Assert.Equal("WithoutArgs", trackName);            
            Assert.Empty(trackCustomDimensions);
        }

        [Fact]
        public void Warning()
        {
            var evtMessage = string.Empty;
            var isWarning = false;

            _logger.WarningTestEvent += (caller, message) =>
            {
                evtMessage = message;
                isWarning = true;
            };

            Log.Instance.Warning("WarningMessage");
            Assert.Equal("WarningMessage", evtMessage);
            Assert.True(isWarning);
        }

        [Fact]
        public void Error()
        {
            var evtMessage = string.Empty;
            var isError = false;

            _logger.ErrorTestEvent += (caller, message) =>
            {
                evtMessage = message;
                isError = true;
            };

            Log.Instance.Error("ErrorMessage");
            Assert.Equal("ErrorMessage", evtMessage);
            Assert.True(isError);
        }

        [Fact]
        public void ScenarioStartEnd()
        {
            var loggedName = string.Empty;
            var loggedCD = string.Empty;
            Guid loggedGuid = default;
            bool isStart, isEnd, isFail;
            isStart = isEnd = isFail = false;

            _logger.StartScenarioTestEvent += (caller, args) =>
            {
                loggedCD = args.customDimensions;
                loggedName = args.scenarioName;
                loggedGuid = args.scenarioInstance;
                isStart = true;
            };

            _logger.EndScenarioTestEvent += (caller, args) =>
            {
                loggedCD = args.customDimensions;
                loggedName = args.scenarioName;
                loggedGuid = args.scenarioInstance;
                isEnd = true;
            };

            _logger.FailScenarioTestEvent += (caller, args) =>
            {
                loggedCD = args.customDimensions;
                loggedName = args.scenarioName;
                loggedGuid = args.scenarioInstance;
                isFail = true;
            };

            var scenario = Log.Instance.StartScenario("SomeScenario");

            Assert.Equal("SomeScenario", loggedName);
            Assert.NotEqual(default, scenario.ScenarioInstance);
            Assert.Empty(loggedCD);
            Assert.Equal(scenario.ScenarioInstance, loggedGuid);
            Assert.True(isStart);
            Assert.False(isFail);
            Assert.False(isEnd);
            
            loggedName = loggedCD = string.Empty;
            loggedGuid = default;
            isStart = isEnd = isFail = false;            

            Log.Instance.EndScenario(scenario, new { Arg1 = 123, Arg2 = "abc" });

            Assert.Equal("SomeScenario", loggedName);
            Assert.Equal(scenario.ScenarioInstance, loggedGuid);
            Assert.Equal(@"{""Arg1"":123,""Arg2"":""abc""}", loggedCD);
            Assert.False(isStart);
            Assert.False(isFail);
            Assert.True(isEnd);
        }

        [Fact]
        public void ScenarioStartFail()
        {
            var loggedName = string.Empty;
            var loggedCD = string.Empty;
            Guid loggedGuid = default;
            bool isStart, isEnd, isFail;
            isStart = isEnd = isFail = false;

            _logger.StartScenarioTestEvent += (caller, args) =>
            {
                loggedCD = args.customDimensions;
                loggedName = args.scenarioName;
                loggedGuid = args.scenarioInstance;
                isStart = true;
            };

            _logger.EndScenarioTestEvent += (caller, args) =>
            {
                loggedCD = args.customDimensions;
                loggedName = args.scenarioName;
                loggedGuid = args.scenarioInstance;
                isEnd = true;
            };

            _logger.FailScenarioTestEvent += (caller, args) =>
            {
                loggedCD = args.customDimensions;
                loggedName = args.scenarioName;
                loggedGuid = args.scenarioInstance;
                isFail = true;
            };

            var scenario = Log.Instance.StartScenario("WillFail", new { Arg1 = 123, Arg2 = "abc" });

            Assert.Equal("WillFail", loggedName);
            Assert.Equal(scenario.ScenarioInstance, loggedGuid);
            Assert.Equal(@"{""Arg1"":123,""Arg2"":""abc""}", loggedCD);
            Assert.True(isStart);
            Assert.False(isFail);
            Assert.False(isEnd);
            
            loggedName = loggedCD = string.Empty;
            loggedGuid = default;
            isStart = isEnd = isFail = false;            

            Log.Instance.FailScenario(scenario);

            Assert.Equal("WillFail", loggedName);
            Assert.Empty(loggedCD);
            Assert.Equal(scenario.ScenarioInstance, loggedGuid);
            Assert.False(isStart);
            Assert.True(isFail);
            Assert.False(isEnd);
        }
    }
}
