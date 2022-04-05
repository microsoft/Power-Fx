// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ExpressionEvaluationTests
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
            protected override Task<FormulaValue> RunAsyncInternal(string expr, string setupHandlerName)
            {
                FeatureFlags.StringInterpolation = true;
                RecalcEngine engine;
                RecordValue parameters;

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

                var result = engine.Eval(expr, parameters);
                return Task.FromResult(result);
            }
        }
    }
}
