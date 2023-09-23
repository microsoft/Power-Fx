// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter.Tests.Helpers;
using Microsoft.PowerFx.Tests;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    public class ExpressionEvaluationTests : PowerFxTest
    {
        internal static Dictionary<string, Func<PowerFxConfig, bool, (RecalcEngine engine, RecordValue parameters)>> SetupHandlers = new ()
        {
            { "OptionSetTestSetup", OptionSetTestSetup },
            { "MutationFunctionsTestSetup", MutationFunctionsTestSetup },
            { "OptionSetSortTestSetup", OptionSetSortTestSetup },
            { "AllEnumsSetup", AllEnumsSetup },
            { "RegEx", RegExSetup },
            { "DecimalSupport", NoOpSetup }, // Decimal is enabled in the C# interpreter
        };

        private static (RecalcEngine engine, RecordValue parameters) NoOpSetup(PowerFxConfig config, bool numberIsFloat)
        {
            return (new RecalcEngine(config), null);
        }

        private static (RecalcEngine engine, RecordValue parameters) RegExSetup(PowerFxConfig config, bool numberIsFloat)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            config.EnableRegExFunctions(new TimeSpan(0, 0, 5));
#pragma warning restore CS0618 // Type or member is obsolete
            return (new RecalcEngine(config), null);
        }

        private static (RecalcEngine engine, RecordValue parameters) AllEnumsSetup(PowerFxConfig config, bool numberIsFloat)
        {
            return (new RecalcEngine(PowerFxConfig.BuildWithEnumStore(new EnumStoreBuilder().WithDefaultEnums(), new TexlFunctionSet(), config.Features)), null);
        }

        private static (RecalcEngine engine, RecordValue parameters) OptionSetTestSetup(PowerFxConfig config, bool numberIsFloat)
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

        private static (RecalcEngine engine, RecordValue parameters) OptionSetSortTestSetup(PowerFxConfig config, bool numberIsFloat)
        {
            var optionSet = new OptionSet("OptionSet", DisplayNameUtility.MakeUnique(new Dictionary<string, string>()
            {
                    { "option_1", "Option1" },
                    { "option_2", "Option2" }
            }));

            config.AddOptionSet(optionSet);

            optionSet.TryGetValue(new DName("option_1"), out var o1Val);
            optionSet.TryGetValue(new DName("option_2"), out var o2Val);

            var r1 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o1Val), new NamedValue("StrField1", FormulaValue.New("test1")));
            var r2 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o2Val), new NamedValue("StrField1", FormulaValue.New("test2")));
            var r3 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o1Val), new NamedValue("StrField1", FormulaValue.New("test3")));
            var r4 = FormulaValue.NewRecordFromFields(new NamedValue("OptionSetField1", o2Val), new NamedValue("StrField1", FormulaValue.New("test4")));

            // Testing with missing/blank option set field is throwing an exception. Once that is resolved uncomment and fix the test case in Sort.txt
            var r5 = FormulaValue.NewRecordFromFields(new NamedValue("StrField1", FormulaValue.New("test5")));

            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("OptionSetField1", FormulaType.OptionSetValue, "DisplayNameField1"))
                .Add(new NamedFormulaType("StrField1", FormulaType.String, "DisplayNameField2"));

            var t1 = FormulaValue.NewTable(rType, r1, r2);
            var t2 = FormulaValue.NewTable(rType, r1, r2, r3, r4);
            var t3 = FormulaValue.NewTable(rType, r1, r2, r3, r5, r4);

            var symbol = config.SymbolTable;
            symbol.AddConstant("t1", t1);
            symbol.AddConstant("t2", t2);
            symbol.AddConstant("t3", t3);

            return (new RecalcEngine(config), null);
        }

        private static (RecalcEngine engine, RecordValue parameters) MutationFunctionsTestSetup(PowerFxConfig config, bool numberIsFloat)
        {
            /*
             * Record r1 => {![Field1:n, Field2:s, Field3:d, Field4:b]}
             * Record r2 => {![Field1:n, Field2:s, Field3:d, Field4:b]}
             * Record rwr1 => {![Field1:n, Field2:![Field2_1:n, Field2_2:s, Field2_3:![Field2_3_1:n, Field2_3_2:s]], Field3:b]}
             * Record rwr2 => {![Field1:n, Field2:![Field2_1:n, Field2_2:s, Field2_3:![Field2_3_1:n, Field2_3_2:s]], Field3:b]}
             * Record rwr3 => {![Field1:n, Field2:![Field2_1:n, Field2_2:s, Field2_3:![Field2_3_1:n, Field2_3_2:s]], Field3:b]}
             * Record r_empty => {}
             * Table t1(r1) => Type (Field1, Field2, Field3, Field4)
             * Table t2(rwr1, rwr2, rwr3)
             * Table t_empty: *[Value:n] = []
             * Table t_empty2: *[Value:n] = []
             * Table t_an_bs: *[a:n,b:s] = []
             * Table t_name: *[name:s] = []
             */

            var numberType = numberIsFloat ? FormulaType.Number : FormulaType.Decimal;

            Func<double, FormulaValue> newNumber = number =>
                numberIsFloat ? FormulaValue.New(number) : FormulaValue.New((decimal)number);

            var rType = RecordType.Empty()
                .Add(new NamedFormulaType("Field1", numberType, "DisplayNameField1"))
                .Add(new NamedFormulaType("Field2", FormulaType.String, "DisplayNameField2"))
                .Add(new NamedFormulaType("Field3", FormulaType.DateTime, "DisplayNameField3"))
                .Add(new NamedFormulaType("Field4", FormulaType.Boolean, "DisplayNameField4"));

            var r1Fields = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(1)),
                new NamedValue("Field2", FormulaValue.New("earth")),
                new NamedValue("Field3", FormulaValue.New(DateTime.Parse("1/1/2022").Date)),
                new NamedValue("Field4", FormulaValue.New(true))
            };

            var r2Fields = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(2)),
                new NamedValue("Field2", FormulaValue.New("moon")),
                new NamedValue("Field3", FormulaValue.New(DateTime.Parse("2/1/2022").Date)),
                new NamedValue("Field4", FormulaValue.New(false))
            };

            var r1 = FormulaValue.NewRecordFromFields(rType, r1Fields);
            var r2 = FormulaValue.NewRecordFromFields(rType, r2Fields);
            var rEmpty = RecordValue.Empty();

            var t1 = FormulaValue.NewTable(rType, new List<RecordValue>() { r1 });

#pragma warning disable SA1117 // Parameters should be on same line or separate lines
            var recordWithRecordType = RecordType.Empty()
                .Add(new NamedFormulaType("Field1", numberType, "DisplayNameField1"))
                .Add(new NamedFormulaType("Field2", RecordType.Empty()
                    .Add(new NamedFormulaType("Field2_1", numberType, "DisplayNameField2_1"))
                    .Add(new NamedFormulaType("Field2_2", FormulaType.String, "DisplayNameField2_2"))
                    .Add(new NamedFormulaType("Field2_3", RecordType.Empty()
                        .Add(new NamedFormulaType("Field2_3_1", numberType, "DisplayNameField2_3_1"))
                        .Add(new NamedFormulaType("Field2_3_2", FormulaType.String, "DisplayNameField2_3_2")),
                    "DisplayNameField2_3")),
                "DisplayNameField2"))
                .Add(new NamedFormulaType("Field3", FormulaType.Boolean, "DisplayNameField3"));
#pragma warning restore SA1117 // Parameters should be on same line or separate lines

            var recordWithRecordFields1 = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(1)),
                new NamedValue("Field2", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                {
                    new NamedValue("Field2_1", newNumber(121)),
                    new NamedValue("Field2_2", FormulaValue.New("2_2")),
                    new NamedValue("Field2_3", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                    {
                        new NamedValue("Field2_3_1", newNumber(1231)),
                        new NamedValue("Field2_3_2", FormulaValue.New("common")),
                    }))
                })),
                new NamedValue("Field3", FormulaValue.New(false))
            };

            var recordWithRecordFields2 = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(2)),
                new NamedValue("Field2", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                {
                    new NamedValue("Field2_1", newNumber(221)),
                    new NamedValue("Field2_2", FormulaValue.New("2_2")),
                    new NamedValue("Field2_3", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                    {
                        new NamedValue("Field2_3_1", newNumber(2231)),
                        new NamedValue("Field2_3_2", FormulaValue.New("common")),
                    }))
                })),
                new NamedValue("Field3", FormulaValue.New(false))
            };

            var recordWithRecordFields3 = new List<NamedValue>()
            {
                new NamedValue("Field1", newNumber(3)),
                new NamedValue("Field2", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                {
                    new NamedValue("Field2_1", newNumber(321)),
                    new NamedValue("Field2_2", FormulaValue.New("2_2")),
                    new NamedValue("Field2_3", FormulaValue.NewRecordFromFields(new List<NamedValue>()
                    {
                        new NamedValue("Field2_3_1", newNumber(3231)),
                        new NamedValue("Field2_3_2", FormulaValue.New("common")),
                    }))
                })),
                new NamedValue("Field3", FormulaValue.New(true))
            };

            var recordWithRecord1 = FormulaValue.NewRecordFromFields(recordWithRecordType, recordWithRecordFields1);
            var recordWithRecord2 = FormulaValue.NewRecordFromFields(recordWithRecordType, recordWithRecordFields2);
            var recordWithRecord3 = FormulaValue.NewRecordFromFields(recordWithRecordType, recordWithRecordFields3);

            var t2 = FormulaValue.NewTable(recordWithRecordType, new List<RecordValue>()
            {
                recordWithRecord1,
                recordWithRecord2,
                recordWithRecord3
            });

            var engine = new RecalcEngine(config);
            var symbol = engine._symbolTable;

            symbol.EnableMutationFunctions();

            engine.UpdateVariable("t1", t1);
            engine.UpdateVariable("r1", r1);

            engine.UpdateVariable("r2", r2);
            engine.UpdateVariable("t2", t2);
            engine.UpdateVariable("rwr1", recordWithRecord1);
            engine.UpdateVariable("rwr2", recordWithRecord2);
            engine.UpdateVariable("rwr3", recordWithRecord3);
            engine.UpdateVariable("r_empty", rEmpty);

            var valueTableType = TableType.Empty().Add("Value", numberType);
            var tEmpty = FormulaValue.NewTable(valueTableType.ToRecord());
            var tEmpty2 = FormulaValue.NewTable(valueTableType.ToRecord());
            engine.UpdateVariable("t_empty", tEmpty);
            engine.UpdateVariable("t_empty2", tEmpty2);

            var abTableType = TableType.Empty().Add("a", numberType).Add("b", FormulaType.String);
            engine.UpdateVariable("t_an_bs", FormulaValue.NewTable(abTableType.ToRecord()));

            var nameTableType = TableType.Empty().Add("name", FormulaType.String);
            engine.UpdateVariable("t_name", FormulaValue.NewTable(nameTableType.ToRecord()));

            return (engine, null);
        }

        // Interpret each test case independently
        // Supports #setup directives. 
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
                var result = await verify.EvalAsync(engine, expr, setup).ConfigureAwait(false);
                return result;
            }

            protected override async Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName)
            {
                RecalcEngine engine;
                RecordValue parameters;
                var iSetup = InternalSetup.Parse(setupHandlerName, Features, NumberIsFloat);

                var config = new PowerFxConfig(features: iSetup.Features);
                config.EnableParseJSONFunction();

                if (string.Equals(iSetup.HandlerName, "AsyncTestSetup", StringComparison.OrdinalIgnoreCase))
                {
                    return new RunResult(await RunVerifyAsync(expr, config, iSetup).ConfigureAwait(false));
                }

                if (iSetup.HandlerName != null)
                {
                    if (!SetupHandlers.TryGetValue(iSetup.HandlerName, out var handler))
                    {
                        throw new SetupHandlerNotFoundException();
                    }

                    (engine, parameters) = handler(config, NumberIsFloat);
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

                var symbolTable = ReadOnlySymbolTable.NewFromRecord(parameters.Type);

                // These tests are only run in en-US locale for now
                var options = iSetup.Flags.ToParserOptions(new CultureInfo("en-US"));
                var check = engine.Check(expr, options: options, symbolTable: symbolTable);
                if (!check.IsSuccess)
                {
                    return new RunResult(check);
                }

                var symbolValues = SymbolValues.NewFromRecord(symbolTable, parameters);
                var runtimeConfig = new RuntimeConfig(symbolValues);

                if (iSetup.TimeZoneInfo != null)
                {
                    runtimeConfig.AddService(iSetup.TimeZoneInfo);
                }

                // Ensure tests can run with governor on. 
                // Some tests that use large memory can disable via:
                //    #SETUP: DisableMemChecks
                if (!iSetup.DisableMemoryChecks)
                {
                    var kbytes = 1000;
                    var mem = new SingleThreadedGovernor(10 * 1000 * kbytes);
                    runtimeConfig.AddService<Governor>(mem);
                }

                var newValue = await check.GetEvaluator().EvalAsync(CancellationToken.None, runtimeConfig).ConfigureAwait(false);

                // UntypedObjectType type is currently not supported for serialization.
                if (newValue.Type is UntypedObjectType)
                {
                    return new RunResult(newValue);
                }

                FormulaValue newValueDeserialized;

                var sb = new StringBuilder();
                var settings = new FormulaValueSerializerSettings()
                {
                    UseCompactRepresentation = true,
                };
                newValue.ToExpression(sb, settings);

                try
                {
                    // Serialization test. Serialized expression must produce an identical result.
                    newValueDeserialized = await engine.EvalAsync(sb.ToString(), CancellationToken.None, options, runtimeConfig: runtimeConfig).ConfigureAwait(false);
                }
                catch (InvalidOperationException e)
                {
                    // If we failed because of range limitations with decimal, retry with NumberIsFloat enabled
                    // This is for tests that return 1e100 as a result, verifying proper floating point operation
                    if (!NumberIsFloat && e.Message.Contains("value is too large"))
                    {
                        // Serialization test. Serialized expression must produce an identical result.
                        options.NumberIsFloat = true;
                        newValueDeserialized = await engine.EvalAsync(sb.ToString(), CancellationToken.None, options, runtimeConfig: runtimeConfig).ConfigureAwait(false);
                    }
                    else
                    {
                        throw;
                    }
                }

                return new RunResult(newValueDeserialized, newValue);
            }
        }

        // Run through a .txt in sequence, allowing Set() functions that can create state. 
        // Useful for testing mutation functions. 
        // The .txt format still provides an expected return value after each expression.
        internal class ReplRunner : BaseRunner
        {
            private readonly RecalcEngine _engine;

            // Repl engine does all the policy around declaring variables via Set().
            public readonly PowerFxReplBase _repl;

            public ReplRunner(RecalcEngine engine)
            {
                _engine = engine;

                _repl = new PowerFxReplBase
                {
                    Engine = _engine,
                    AllowSetDefinitions = true
                };
            }

            protected override async Task<RunResult> RunAsyncInternal(string expr, string setupHandlerName = null)
            {
                var replResult = await _repl.HandleCommandAsync(expr, default).ConfigureAwait(false);

                // .txt output format - if there are any Set(), compare those.
                if (replResult.DeclaredVars.Count > 0)
                {
                    var newVar = replResult.DeclaredVars.First().Value;
                    var runResult = new RunResult(newVar);
                    return runResult;
                }

                var check = replResult.CheckResult;
                if (!check.IsSuccess)
                {
                    return new RunResult(check);
                }

                var result = replResult.EvalResult;
                return new RunResult(result);
            }
        }
    }

    public static class UserInfoTestSetup
    {
        public static BasicUserInfo UserInfo = new BasicUserInfo
        {
            FullName = "Susan Burk",
            Email = "susan@contoso.com",
            DataverseUserId = new Guid("aa1d4f65-044f-4928-a95f-30d4c8ebf118"),
            TeamsMemberId = "29:1DUjC5z4ttsBQa0fX2O7B0IDu30R",
        };

        public static SymbolTable GetUserInfoSymbolTable()
        {
            var props = new Dictionary<string, object>
            {
                { "FullName", UserInfo.FullName },
                { "Email", UserInfo.Email },
                { "DataverseUserId", UserInfo.DataverseUserId },
                { "TeamsMemberId", UserInfo.TeamsMemberId }
            };

            var allKeys = props.Keys.ToArray();
            SymbolTable userSymbolTable = new SymbolTable();

            userSymbolTable.AddUserInfoObject(allKeys);

            return userSymbolTable;
        }
    }
}
