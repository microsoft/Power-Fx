﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ExpressionEvaluationTests : PowerFxTest
    {
        internal static Dictionary<string, Func<PowerFxConfig, (RecalcEngine engine, RecordValue parameters)>> SetupHandlers = new ()
        {
            { "OptionSetTestSetup", OptionSetTestSetup }
        };

        private static (RecalcEngine engine, RecordValue parameters) OptionSetTestSetup(PowerFxConfig config)
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
            private async Task<FormulaValue> RunVerifyAsync(string expr, PowerFxConfig config, InternalSetup setup)
            {
                var verify = new AsyncVerify();

                // Add Async(),WaitFor() functions 
                var asyncHelper = new AsyncFunctionsHelper(verify);
                config.AddFunction(asyncHelper.GetFunction());

                var waitForHelper = new WaitForFunctionsHelper(verify);
                config.AddFunction(waitForHelper.GetFunction());

                config.EnableSetFunction();
                var engine = new RecalcEngine(config);
                engine.UpdateVariable("varNumber", 9999);

                // Run in special mode that ensures we're not calling .Result
                var result = await verify.EvalAsync(engine, expr, setup);
                return result;
            }

            protected override async Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName)
            {
                Preview.FeatureFlags.StringInterpolation = true;
                RecalcEngine engine;
                RecordValue parameters;
                var iSetup = InternalSetup.Parse(setupHandlerName);
                var config = new PowerFxConfig(features: iSetup.Features);

                if (string.Equals(iSetup.HandlerName, "AsyncTestSetup", StringComparison.OrdinalIgnoreCase))
                {
                    return new RunResult(await RunVerifyAsync(expr, config, iSetup));
                }

                if (iSetup.HandlerName != null)
                {
                    if (!SetupHandlers.TryGetValue(setupHandlerName, out var handler))
                    {
                        throw new SetupHandlerNotFoundException();
                    }

                    (engine, parameters) = handler(config);
                }
                else
                {
                    engine = new RecalcEngine(config);
                    parameters = null;
                }

                if (parameters == null)
                {
                    parameters = RecordValue.Empty();
                }

                var check = engine.Check(expr, parameters.Type, options: iSetup.Flags.ToParserOptions());
                if (!check.IsSuccess)
                {
                    return new RunResult(check);
                }

                var rtConfig = SymbolValues.NewFromRecord(parameters) as SymbolValues;

                if (iSetup.TimeZoneInfo != null)
                {
                    rtConfig.AddService(iSetup.TimeZoneInfo);
                }

                var newValue = await check.GetEvaluator().EvalAsync(CancellationToken.None, rtConfig);

                return new RunResult(newValue);
            }
        }
    }
}
