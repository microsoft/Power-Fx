// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ExpressionEvaluationTests
    {
        internal static Dictionary<string, Func<RecalcEngine>> SetupHandlers = new Dictionary<string, Func<RecalcEngine>>() 
        {
            { "OptionSetTestSetup", OptionSetTestSetup }
        };

        private static RecalcEngine OptionSetTestSetup()
        {            
            var optionSet = new OptionSet("OptionSet", new Dictionary<string, string>() 
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            });
            
            var otherOptionSet = new OptionSet("OtherOptionSet", new Dictionary<string, string>() 
            {
                    { "99", "OptionA" },
                    { "112", "OptionB" },
                    { "35694", "OptionC" },
                    { "123412983", "OptionD" },
            });

            var config = new PowerFxConfig(null, null);
            config.AddOptionSet(optionSet);
            config.AddOptionSet(otherOptionSet);

            return new RecalcEngine(config);
        }

        internal class InterpreterRunner : BaseRunner
        {
            private readonly RecalcEngine _engine = new RecalcEngine();

            public override Task<FormulaValue> RunAsync(string expr)
            {
                FeatureFlags.StringInterpolation = true;
                var result = _engine.Eval(expr);
                return Task.FromResult(result);
            }

            public override Task<FormulaValue> RunWithSetup(string expr, string setupHandlerName)
            {
                FeatureFlags.StringInterpolation = true;
                if (!SetupHandlers.TryGetValue(setupHandlerName, out var handler))
                {
                    throw new NotSupportedException($"Setup Handler {setupHandlerName} not defined for {nameof(InterpreterRunner)}");
                }

                var engine = handler();
                var result = engine.Eval(expr);
                return Task.FromResult(result);
            }
        }
    }
}
