// Copyright (c) Microsoft Corporation.
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
            { "OptionSetTestSetup", OptionSetTestSetup },
            { "MutationFunctionsTestSetup", MutationFunctionsTestSetup }
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

        private static (RecalcEngine engine, RecordValue parameters) MutationFunctionsTestSetup(PowerFxConfig config)
        {
            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("Field1", FormulaType.Number, "DisplayNameField1"))
                .Add(new NamedFormulaType("Field2", FormulaType.String, "DisplayNameField2"))
                .Add(new NamedFormulaType("Field3", FormulaType.DateTime, "DisplayNameField3"))
                .Add(new NamedFormulaType("Field4", FormulaType.Boolean, "DisplayNameField4"));

            var r1Fields = new List<NamedValue>()
            {
                new NamedValue("Field1", FormulaValue.New(1)),
                new NamedValue("Field2", FormulaValue.New("earth")),
                new NamedValue("Field3", FormulaValue.New(DateTime.Parse("1/1/2022").Date)),
                new NamedValue("Field4", FormulaValue.New(true))
            };

            var r2Fields = new List<NamedValue>()
            {
                new NamedValue("Field1", FormulaValue.New(2)),
                new NamedValue("Field2", FormulaValue.New("moon")),
                new NamedValue("Field3", FormulaValue.New(DateTime.Parse("2/1/2022").Date)),
                new NamedValue("Field4", FormulaValue.New(false))
            };

            var r1 = FormulaValue.NewRecordFromFields(rType, r1Fields);
            var r2 = FormulaValue.NewRecordFromFields(rType, r2Fields);
            var rEmpty = RecordValue.Empty();

            var t1 = FormulaValue.NewTable(rType, new List<RecordValue>() { r1 });

            var symbol = new SymbolTable();

            symbol.EnableMutationFunctions();

            symbol.AddConstant("t1", t1);
            symbol.AddConstant("r1", r1);
            symbol.AddConstant("r2", r2);
            symbol.AddConstant("r_empty", rEmpty);

            config.SymbolTable = symbol;

            return (new RecalcEngine(config), null);
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
                    if (!SetupHandlers.TryGetValue(iSetup.HandlerName, out var handler))
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
