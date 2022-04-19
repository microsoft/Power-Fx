// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Tests;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ExpressionEvaluationTests : PowerFxTest
    {
        internal static Dictionary<string, Func<(RecalcEngine engine, RecordValue parameters)>> SetupHandlers = new Dictionary<string, Func<(RecalcEngine engine, RecordValue parameters)>>() 
        {
            { "OptionSetTestSetup", OptionSetTestSetup }
        };

        private static (RecalcEngine engine, RecordValue parameters) OptionSetTestSetup()
        {            
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() 
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));
            
            var otherOptionSet = new OptionSet("OtherOptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>() 
            {
                    { "99", "OptionA" },
                    { "112", "OptionB" },
                    { "35694", "OptionC" },
                    { "123412983", "OptionD" },
            }));

            var config = new PowerFxConfig(null);
            config.AddOptionSet(optionSet);
            config.AddOptionSet(otherOptionSet);

            optionSet.TryGetValue(new DName("option_1"), out var o1Val);            
            otherOptionSet.TryGetValue(new DName("123412983"), out var o2Val);

            var parameters = FormulaValue.NewRecordFromFields(
                    new NamedValue("TopOptionSetField", o1Val),
                    new NamedValue("Nested", FormulaValue.NewRecordFromFields(
                        new NamedValue("InnerOtherOptionSet", o2Val)))); 

            return (new RecalcEngine(config), parameters);
        }        

        internal class InterpreterRunner : BaseRunner
        {
            // For async tests, run in special mode. 
            // This does _not_ change evaluation semantics, but does verify .Result isn't called by checking
            // task completion status.. 
            private async Task<FormulaValue> RunVerifyAsync(string expr)
            {
                var config = new PowerFxConfig(null);

                var verify = new AsyncVerify();

                // Add Async(),WaitFor() functions 
                var asyncHelper = new AsyncFunctionsHelper(verify);
                config.AddFunction(asyncHelper.GetFunction());

                var waitForHelper = new WaitForFunctionsHelper(verify);
                config.AddFunction(waitForHelper.GetFunction());

                var engine = new RecalcEngine(config);

                // Run in special mode that ensures we're not calling .Result
                var result = await verify.EvalAsync(engine, expr);
                return result;
            }

            protected override async Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName)
            {
                FeatureFlags.StringInterpolation = true;
                RecalcEngine engine;
                RecordValue parameters;

                if (setupHandlerName == "AsyncTestSetup")
                {
                    return new RunResult(await RunVerifyAsync(expr));
                }

                if (setupHandlerName != null) 
                {
                    if (!SetupHandlers.TryGetValue(setupHandlerName, out var handler))
                    {
                        throw new SetupHandlerNotFoundException();
                    }

                    (engine, parameters) = handler();
                }
                else
                {
                    engine = new RecalcEngine();
                    parameters = null;
                }

                if (parameters == null)
                {
                    parameters = RecordValue.Empty();
                }

                var check = engine.Check(expr, parameters.Type);
                if (!check.IsSuccess)
                {
                    return new RunResult(check);
                }

                var newValue = await check.Expression.EvalAsync(parameters, CancellationToken.None);

                return new RunResult(newValue);
            }
        }
    }
}
