﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Logging;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        /// <summary>
        /// This isn't part of <see cref="BuiltinFunctionsCore"/> since PA has different implementation of
        /// Texl Instance of <see cref="DistinctFunction"/>.
        /// </summary>
        public static readonly TexlFunction DistinctInterpreterFunction = new DistinctFunction();

        internal static readonly DateTime _epoch = new DateTime(1899, 12, 30, 0, 0, 0, 0);

        // Helper to get a service or fallback to a default if the service is missing.
        private static T GetService<T>(this IServiceProvider services, T defaultService)
        {
            var service = (T)services.GetService(typeof(T));
            return service ?? defaultService;
        }

        // Sync FunctionPtr - all args are evaluated before invoking this function.  
        public delegate FormulaValue FunctionPtr(SymbolContext symbolContext, IRContext irContext, FormulaValue[] args);

        // Async - can invoke lambdas.
        public delegate ValueTask<FormulaValue> AsyncFunctionPtr(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args);

        public static IEnumerable<TexlFunction> FunctionList => FunctionImplementations.Keys;

        public static readonly IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> FunctionImplementations;

        static Library()
        {
            var allFunctions = new Dictionary<TexlFunction, AsyncFunctionPtr>();
            foreach (var func in SimpleFunctionImplementations)
            {
                allFunctions.Add(func.Key, func.Value);
            }

            foreach (var func in SimpleFunctionTabularOverloadImplementations)
            {
                Contracts.Assert(allFunctions.Any(f => f.Key.Name == func.Key.Name), "It needs to be an overload");
                allFunctions.Add(func.Key, func.Value);
            }

            foreach (var func in SimpleFunctionMultiArgsTabularOverloadImplementations)
            {
                Contracts.Assert(allFunctions.Any(f => f.Key.Name == func.Key.Name), "It needs to be an overload");
                allFunctions.Add(func.Key, func.Value);
            }

            FunctionImplementations = allFunctions;
        }

        // Some TexlFunctions are overloaded
        private static IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> SimpleFunctionImplementations { get; } = new Dictionary<TexlFunction, AsyncFunctionPtr>
        {
            {
                BuiltinFunctionsCore.Abs,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Abs.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: NumberOrDecimal,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Abs)
            },
            {
                BuiltinFunctionsCore.Acos,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Acos.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(Math.Acos))
            },
            {
                BuiltinFunctionsCore.Acot,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Acot.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Acot)
            },
            {
                BuiltinFunctionsCore.AddColumns,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.AddColumns.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: AddColumnsTypeChecker,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: AddColumns)
            },
            {
                BuiltinFunctionsCore.And,
                And
            },
            {
                BuiltinFunctionsCore.Asin,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Asin.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(Math.Asin))
            },
            {
                BuiltinFunctionsCore.AsType,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.Asin.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<FormulaValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: AsType)
            },
            {
                BuiltinFunctionsCore.Atan,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Atan.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(Math.Atan))
            },
            {
                BuiltinFunctionsCore.Atan2,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Atan2.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Atan2)
            },
            {
                BuiltinFunctionsCore.Average,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Average.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Average)
            },
            {
                BuiltinFunctionsCore.AverageT,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.AverageT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: AverageTable)
            },
            {
                BuiltinFunctionsCore.Blank,
                Blank
            },
            {
                BuiltinFunctionsCore.Boolean,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Boolean.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TextToBoolean)
            },
            {
                BuiltinFunctionsCore.BooleanN,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.BooleanN.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToBoolean)
            },
            {
                BuiltinFunctionsCore.BooleanW,
                StandardErrorHandling<DecimalValue>(
                    BuiltinFunctionsCore.BooleanW.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<DecimalValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DecimalToBoolean)
            },
            {
                BuiltinFunctionsCore.BooleanL,
                StandardErrorHandling<OptionSetValue>(
                    BuiltinFunctionsCore.BooleanL.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<OptionSetValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: BooleanOptionSetToBoolean)
            },

            // This implementation is not actually used for this as this is handled at IR level. 
            // This is a placeholder, so that RecalcEngine._interpreterSupportedFunctions can add it for txt tests.
            {
                BuiltinFunctionsCore.BooleanB,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.BooleanN.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TextToBoolean)
            },
            {
                BuiltinFunctionsCore.Boolean_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.Boolean_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Boolean_UO)
            },
            {
                BuiltinFunctionsCore.Concat,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.Concat.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>,
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Concat)
            },
            {
                BuiltinFunctionsCore.Coalesce,
                NoErrorHandling(Coalesce)
            },
            {
                BuiltinFunctionsCore.Char,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Char.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Char)
            },
            {
                BuiltinFunctionsCore.ColorValue,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.ColorValue.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ColorValue)
            },
            {
                BuiltinFunctionsCore.ColorValue_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.ColorValue.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ColorValue_UO)
            },
            {
                BuiltinFunctionsCore.ColorFade,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.ColorFade.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new ColorValue(IRContext.NotInSource(FormulaType.Color), Color.FromArgb(0, 0, 0, 0)),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<ColorValue>,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: ColorFade)
            },
            {
                BuiltinFunctionsCore.Column_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.CountRows_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Column_UO)
            },
            {
                BuiltinFunctionsCore.ColumnNames_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.CountRows_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ColumnNames_UO)
            },
            {
                BuiltinFunctionsCore.Concatenate,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Concatenate.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: StringOrOptionSet,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Concatenate)
            },
            {
                BuiltinFunctionsCore.Cos,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Cos.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(Math.Cos))
            },
            {
                BuiltinFunctionsCore.Cot,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Cot.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Cot)
            },
            {
                BuiltinFunctionsCore.Count,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Count.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Count)
            },
            {
                BuiltinFunctionsCore.CountA,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.CountA.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: CountA)
            },
            {
                BuiltinFunctionsCore.CountIf,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.CountIf.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequenceVariadic(
                        new Func<IRContext, int, FormulaValue, FormulaValue>[] { ExactValueTypeOrBlank<TableValue> },
                        new Func<IRContext, int, FormulaValue, FormulaValue>[] { ExactValueTypeOrBlank<LambdaFormulaValue> }),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: CountIf)
            },
            {
                BuiltinFunctionsCore.CountRows,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.CountRows.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: CountRows)
            },
            {
                BuiltinFunctionsCore.CountRows_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.CountRows_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: CountRows_UO)
            },
            {
                BuiltinFunctionsCore.Date,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Date.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Date)
            },
            {
                BuiltinFunctionsCore.DateAdd,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.DateAdd.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                    replaceBlankValues: ReplaceBlankWith(
                        new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                        new StringValue(IRContext.NotInSource(FormulaType.String), "days")),
                    checkRuntimeTypes: ExactSequence(
                        DateOrTimeOrDateTime,
                        ExactValueTypeOrBlank<NumberValue>,
                        StringOrOptionSetBackedByString),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateAdd)
            },
            {
                BuiltinFunctionsCore.DateDiff,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.DateDiff.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                    replaceBlankValues: ReplaceBlankWith(
                        new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                        new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                        new StringValue(IRContext.NotInSource(FormulaType.String), "days")),
                    checkRuntimeTypes: ExactSequence(
                        DateOrTimeOrDateTime,
                        DateOrTimeOrDateTime,
                        StringOrBlankOrOptionSetBackedByString),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateDiff)
            },
            {
                BuiltinFunctionsCore.DateTime,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.DateTime.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 7, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: DateTimeFunction)
            },
            {
                BuiltinFunctionsCore.DateValue,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.DateValue.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateParse)
            },
            {
                BuiltinFunctionsCore.DateValue_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.DateValue_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateValue_UO)
            },
            {
                BuiltinFunctionsCore.DateTimeValue,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.DateTimeValue.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateTimeParse)
            },
            {
                BuiltinFunctionsCore.DateTimeValue_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.DateTimeValue_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateTimeValue_UO)
            },
            {
                BuiltinFunctionsCore.Day,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Day.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Day)
            },
            {
                BuiltinFunctionsCore.Degrees,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Degrees.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(x => x * 180.0 / Math.PI))
            },
            {
                BuiltinFunctionsCore.Dec2Hex,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Dec2Hex.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Dec2Hex)
            },
            {
                BuiltinFunctionsCore.Decimal,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Decimal.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Decimal)
            },
            {
                BuiltinFunctionsCore.Decimal_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Decimal.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Decimal_UO)
            },
            {
                DistinctInterpreterFunction,
                StandardErrorHandlingAsync<FormulaValue>(
                    DistinctInterpreterFunction.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DistinctTable)
            },
            {
                BuiltinFunctionsCore.DropColumns,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.DropColumns.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ShowDropRenameColumnsTypeChecker,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DropColumns)
            },
            {
                BuiltinFunctionsCore.EDate,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.EDate.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    checkRuntimeTypes: ExactSequence(
                        DateOrDateTime,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: EDate)
            },
            {
                BuiltinFunctionsCore.EOMonth,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.EOMonth.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    checkRuntimeTypes: ExactSequence(
                        DateOrDateTime,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: EOMonth)
            },
            {
                BuiltinFunctionsCore.EncodeHTML,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.EncodeUrl.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: EncodeHTML)
            },
            {
                BuiltinFunctionsCore.EncodeUrl,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.EncodeUrl.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: EncodeUrl)
            },
            {
                BuiltinFunctionsCore.EndsWith,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.EndsWith.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: EndsWith)
            },
            {
                BuiltinFunctionsCore.Error,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Error.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnFalseIfAnyArgIsBlank,
                    targetFunction: Error)
            },
            {
                BuiltinFunctionsCore.Exp,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Exp.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Exp)
            },
            {
                BuiltinFunctionsCore.Filter,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.Filter.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: FilterTable)
            },
            {
                BuiltinFunctionsCore.Find,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Find.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueType<StringValue>,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: ExactSequence(
                        DeferRuntimeValueChecking,
                        DeferRuntimeValueChecking,
                        StrictArgumentPositiveNumberChecker),
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Find)
            },
            {
                BuiltinFunctionsCore.First,
                StandardErrorHandling<TableValue>(
                    BuiltinFunctionsCore.First.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: First)
            },
            {
                BuiltinFunctionsCore.FirstN,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.FirstN.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: ReplaceBlankWithFloatZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: FirstN)
            },
            {
                BuiltinFunctionsCore.First_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.First_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: UntypedObjectArrayChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: First_UO)
            },
            {
                BuiltinFunctionsCore.FirstN_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.FirstN.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithFloatZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: ExactSequence(
                        UntypedObjectArrayChecker,
                        DeferRuntimeValueChecking),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: FirstN_UO)
            },
            {
                BuiltinFunctionsCore.Float,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Float.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Float)
            },
            {
                BuiltinFunctionsCore.Float_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Float.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Float_UO)
            },
            {
                BuiltinFunctionsCore.ForAll,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.ForAll.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ForAll)
            },
            {
                BuiltinFunctionsCore.ForAll_UO,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.ForAll_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: ExactSequence(
                        UntypedObjectArrayChecker,
                        DeferRuntimeValueChecking),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ForAll_UO)
            },
            {
                BuiltinFunctionsCore.GUIDNoArg,
                NoErrorHandling(GuidNoArg)
            },
            {
                BuiltinFunctionsCore.GUIDPure,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.GUIDPure.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: GuidPure)
            },
            {
                BuiltinFunctionsCore.GUID_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.GUID_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Guid_UO)
            },
            {
                BuiltinFunctionsCore.Hex2Dec,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Hex2Dec.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Hex2Dec)
            },
            {
                BuiltinFunctionsCore.Hour,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Hour.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: TimeOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Hour)
            },
            {
                BuiltinFunctionsCore.If,
                If
            },
            {
                BuiltinFunctionsCore.IfError,
                IfError
            },
            {
                BuiltinFunctionsCore.Int,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Int.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: NumberOrDecimal,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Int)
            },
            {
                BuiltinFunctionsCore.Index,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Index.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithFloatZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: ExactSequence(
                        DeferRuntimeValueChecking,
                        StrictNumericPositiveNumberChecker),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: IndexTable)
            },
            {
                BuiltinFunctionsCore.Index_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Index_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithFloatZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: ExactSequence(
                        UntypedObjectArrayChecker,
                        StrictNumericPositiveNumberChecker),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Index_UO)
            },
            {
                BuiltinFunctionsCore.IsBlank,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.IsBlank.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: IsBlank)
            },
            {
                // Implementation 100% shared with IsBlank() for the interpreter
                BuiltinFunctionsCore.IsBlankOptionSetValue,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.IsBlankOptionSetValue.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: IsBlank)
            },
            {
                BuiltinFunctionsCore.IsBlankOrError,
                NoErrorHandling(IsBlankOrError)
            },
            {
                BuiltinFunctionsCore.IsBlankOrErrorOptionSetValue,
                NoErrorHandling(IsBlankOrError)
            },
            {
                BuiltinFunctionsCore.IsEmpty,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.IsEmpty.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: IsEmpty)
            },
            {
                BuiltinFunctionsCore.IsError,
                NoErrorHandling(IsError)
            },
            {
                BuiltinFunctionsCore.IsNumeric,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.IsNumeric.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: IsNumeric)
            },
            {
                BuiltinFunctionsCore.IsToday,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.IsToday.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnFalseIfAnyArgIsBlank,
                    targetFunction: IsToday)
            },
            {
                BuiltinFunctionsCore.Language,
                NoErrorHandling(Language)
            },
            {
                BuiltinFunctionsCore.Last,
                StandardErrorHandling<TableValue>(
                    BuiltinFunctionsCore.Last.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Last)
            },
            {
                BuiltinFunctionsCore.LastN,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.LastN.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: ReplaceBlankWithFloatZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: LastN)
            },
            {
                BuiltinFunctionsCore.Last_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.Last_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: UntypedObjectArrayChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Last_UO)
            },
            {
                BuiltinFunctionsCore.LastN_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.LastN.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithFloatZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: ExactSequence(
                        UntypedObjectArrayChecker,
                        DeferRuntimeValueChecking),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: LastN_UO)
            },
            {
                BuiltinFunctionsCore.Left,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Left.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: PositiveNumericNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Left)
            },
            {
                BuiltinFunctionsCore.Len,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Len.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Len)
            },
            {
                BuiltinFunctionsCore.Ln,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Ln.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Ln)
            },
            {
                BuiltinFunctionsCore.Log,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Log.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 10)),
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Log)
            },
            {
                BuiltinFunctionsCore.LookUp,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.LookUp.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: LookUp)
            },
            {
                BuiltinFunctionsCore.Lower,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Lower.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Lower)
            },
            {
                BuiltinFunctionsCore.Max,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Max.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Max)
            },
            {
                BuiltinFunctionsCore.MaxT,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.MaxT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: MaxTable)
            },
            {
                BuiltinFunctionsCore.Mid,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Mid.Name,
                    expandArguments: MidFunctionExpandArgs,
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Mid)
            },
            {
                BuiltinFunctionsCore.Min,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Min.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Min)
            },
            {
                BuiltinFunctionsCore.MinT,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.MinT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: MinTable)
            },
            {
                BuiltinFunctionsCore.Minute,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Minute.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: TimeOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Minute)
            },
            {
                BuiltinFunctionsCore.Mod,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Mod.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: NumberOrDecimal,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Mod)
            },
            {
                BuiltinFunctionsCore.Month,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Month.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Month)
            },
            {
                BuiltinFunctionsCore.Not,
                StandardErrorHandling<BooleanValue>(
                    BuiltinFunctionsCore.Not.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false)),
                    checkRuntimeTypes: ExactValueType<BooleanValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Not)
            },
            {
                BuiltinFunctionsCore.Now,
                NoErrorHandling(Now)
            },
            {
                BuiltinFunctionsCore.Or,
                Or
            },
            {
                BuiltinFunctionsCore.PatchRecord,
                StandardErrorHandling<RecordValue>(
                    BuiltinFunctionsCore.PatchRecord.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<RecordValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: PatchRecord)
            },
            {
                BuiltinFunctionsCore.Proper,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Proper.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Proper)
            },
            {
                BuiltinFunctionsCore.Pi,
                NoErrorHandling(Pi)
            },
            {
                BuiltinFunctionsCore.PlainText,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.PlainText.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank,
                    targetFunction: PlainText)
            },
            {
                BuiltinFunctionsCore.Power,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Power.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Power)
            },
            {
                BuiltinFunctionsCore.Radians,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Radians.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(x => x * Math.PI / 180.0))
            },
            {
                BuiltinFunctionsCore.Rand,
                Rand
            },
            {
                BuiltinFunctionsCore.RandBetween,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.RandBetween.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: NumberOrDecimal,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RandBetween)
            },
            {
                BuiltinFunctionsCore.Refresh,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Refresh.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Refresh)
            },
            {
                BuiltinFunctionsCore.RenameColumns,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.RenameColumns.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ShowDropRenameColumnsTypeChecker,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: RenameColumns)
            },
            {
                BuiltinFunctionsCore.Replace,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Replace.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueType<NumberValue>,
                        ExactValueType<NumberValue>,
                        ExactValueType<StringValue>),
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Replace)
            },
            {
                BuiltinFunctionsCore.RGBA,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.RGBA.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RGBA)
            },
            {
                BuiltinFunctionsCore.Right,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Right.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: PositiveNumericNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Right)
            },
            {
                BuiltinFunctionsCore.Round,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Round.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactSequence(
                        NumberOrDecimal,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Round)
            },
            {
                BuiltinFunctionsCore.RoundUp,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.RoundUp.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactSequence(
                        NumberOrDecimal,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundUp)
            },
            {
                BuiltinFunctionsCore.RoundDown,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.RoundDown.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactSequence(
                        NumberOrDecimal,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundDown)
            },
            {
                BuiltinFunctionsCore.Search,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.Search.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: SearchTypeChecker,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SearchImpl)
            },
            {
                BuiltinFunctionsCore.Second,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Second.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: TimeOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Second)
            },
            {
                BuiltinFunctionsCore.Sequence,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Sequence.Name,
                    expandArguments: SequenceFunctionExpandArgs,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<NumberValue>,
                        NumberOrDecimal,
                        NumberOrDecimal),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Sequence)
            },
            {
                BuiltinFunctionsCore.Shuffle,
                StandardErrorHandling<TableValue>(
                    BuiltinFunctionsCore.Shuffle.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Shuffle)
            },
            {
                BuiltinFunctionsCore.ShowColumns,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.ShowColumns.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ShowColumnsTypeChecker,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ShowColumns)
            },
            {
                BuiltinFunctionsCore.Sin,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Sin.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(Math.Sin))
            },
            {
                BuiltinFunctionsCore.Sort,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.Sort.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new StringValue(IRContext.NotInSource(FormulaType.String), "Ascending")),
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>,
                        StringOrOptionSetBackedByString),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: SortTable)
            },
            {
                BuiltinFunctionsCore.SortByColumns,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.SortByColumns.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: SortByColumnsTypeChecker,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: SortByColumns)
            },
            {
                BuiltinFunctionsCore.Split,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Split.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    Split)
            },
            {
                BuiltinFunctionsCore.Sqrt,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Sqrt.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Sqrt)
            },
            {
                BuiltinFunctionsCore.StartsWith,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.StartsWith.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: StartsWith)
            },
            {
                BuiltinFunctionsCore.StdevP,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.StdevP.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Stdev)
            },
            {
                BuiltinFunctionsCore.StdevPT,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.StdevPT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: StdevTable)
            },
            {
                BuiltinFunctionsCore.Substitute,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Substitute.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueType<StringValue>,
                        ExactValueType<StringValue>,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: StrictArgumentPositiveNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Substitute)
            },
            {
                BuiltinFunctionsCore.Sum,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Sum.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Sum)
            },
            {
                BuiltinFunctionsCore.Summarize,
                NoErrorHandling(Summarize)
            },
            {
                BuiltinFunctionsCore.SumT,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.SumT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: SumTable)
            },
            {
                BuiltinFunctionsCore.Switch,
                Switch
            },
            {
                BuiltinFunctionsCore.Table,
                NoErrorHandling(Table)
            },
            {
                BuiltinFunctionsCore.Table_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.Table_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: UntypedObjectArrayChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Table_UO)
            },
            {
                BuiltinFunctionsCore.Tan,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Tan.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(Math.Tan))
            },
            {
                BuiltinFunctionsCore.Text,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Text.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Text)
            },
            {
                BuiltinFunctionsCore.Text_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Text_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Text_UO)
            },
            {
                BuiltinFunctionsCore.Time,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Time.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 4, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Time)
            },
            {
                BuiltinFunctionsCore.TimeValue,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.TimeValue.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TimeParse)
            },
            {
                BuiltinFunctionsCore.TimeValue_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.TimeValue_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TimeValue_UO)
            },
            {
                BuiltinFunctionsCore.TimeZoneOffset,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.TimeZoneOffset.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch)),
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: TimeZoneOffset)
            },
            {
                BuiltinFunctionsCore.Today,
                NoErrorHandling(Today)
            },
            {
                BuiltinFunctionsCore.Trace,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.Trace.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<StringValue>,
                        NumberOrDecimalOrOptionSetBackedByNumber,
                        ExactValueTypeOrBlank<RecordValue>,
                        StringOrOptionSetBackedByString), /* add check */
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: TraceFunction)
            },
            {
                BuiltinFunctionsCore.Trim,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Trim.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Trim)
            },
            {
                BuiltinFunctionsCore.TrimEnds,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.TrimEnds.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: TrimEnds)
            },
            {
                BuiltinFunctionsCore.Trunc,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Trunc.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactSequence(
                        NumberOrDecimal,
                        ExactValueType<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundDown)
            },
            {
                BuiltinFunctionsCore.UniChar,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.UniChar.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: UniChar)
            },
            {
                BuiltinFunctionsCore.Upper,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Upper.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: NoOpAlreadyHandledByIR,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Upper)
            },
            {
                BuiltinFunctionsCore.Value,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Value.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Value)
            },
            {
                BuiltinFunctionsCore.Value_UO,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Value_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Value_UO)
            },
            {
                BuiltinFunctionsCore.VarP,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.VarP.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Var)
            },
            {
                BuiltinFunctionsCore.VarPT,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.VarPT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: VarTable)
            },
            {
                BuiltinFunctionsCore.Weekday,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Weekday.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: ReplaceBlankWith(
                        new DateTimeValue(IRContext.NotInSource(FormulaType.DateTime), _epoch),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    checkRuntimeTypes: ExactSequence(
                        DateOrTimeOrDateTime,
                        NumberOrBlankOrOptionSetBackedByNumber),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Weekday)
            },
            {
                BuiltinFunctionsCore.WeekNum,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.WeekNum.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        DateOrTimeOrDateTime,
                        NumberOrBlankOrOptionSetBackedByNumber),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: WeekNum)
            },
            {
                BuiltinFunctionsCore.With,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.With.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: With)
            },
            {
                BuiltinFunctionsCore.Year,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Year.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Year)
            }
        };

        // Tabular overloads for functions in SimpleFunctionImplementations
        private static IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> SimpleFunctionTabularOverloadImplementations { get; } = new Dictionary<TexlFunction, AsyncFunctionPtr>
        {
            {
                BuiltinFunctionsCore.AbsT,
                StandardErrorHandlingTabularOverload<FormulaValue>(BuiltinFunctionsCore.AbsT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Abs], ReplaceBlankWithCallZero)
            },
            {
                BuiltinFunctionsCore.AcosT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.AcosT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Acos], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.AcotT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.AcotT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Acot], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.AsinT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.AsinT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Asin], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.AtanT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.AtanT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Atan], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.Boolean_T,
                StandardErrorHandlingTabularOverload<StringValue>(BuiltinFunctionsCore.Boolean_T.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Boolean], DoNotReplaceBlank)
            },
            {
                BuiltinFunctionsCore.BooleanN_T,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.BooleanN_T.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.BooleanN], DoNotReplaceBlank)
            },
            {
                BuiltinFunctionsCore.BooleanW_T,
                StandardErrorHandlingTabularOverload<DecimalValue>(BuiltinFunctionsCore.BooleanW_T.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.BooleanW], DoNotReplaceBlank)
            },
            {
                BuiltinFunctionsCore.BooleanL_T,
                StandardErrorHandlingTabularOverload<OptionSetValue>(BuiltinFunctionsCore.BooleanL_T.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.BooleanL], DoNotReplaceBlank)
            },

            // This implementation is not actually used for this as this is handled at IR level. 
            // This is a placeholder, so that RecalcEngine._interpreterSupportedFunctions can add it for txt tests.
            {
                BuiltinFunctionsCore.BooleanB_T,
                StandardErrorHandlingTabularOverload<StringValue>(BuiltinFunctionsCore.BooleanB_T.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.BooleanB], DoNotReplaceBlank)
            },
            {
                BuiltinFunctionsCore.CharT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.CharT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Char], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.CosT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.CosT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Cos], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.CotT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.CotT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Cot], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.DegreesT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.DegreesT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Degrees], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.ExpT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.ExpT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Exp], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.Hex2DecT,
                StandardErrorHandlingTabularOverload<StringValue>(BuiltinFunctionsCore.Hex2DecT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Hex2Dec], ReplaceBlankWithEmptyString)
            },
            {
                BuiltinFunctionsCore.IntT,
                StandardErrorHandlingTabularOverload<FormulaValue>(BuiltinFunctionsCore.IntT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Int], ReplaceBlankWithCallZero)
            },
            {
                BuiltinFunctionsCore.LenT,
                StandardErrorHandlingTabularOverload<StringValue>(BuiltinFunctionsCore.LenT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Len], ReplaceBlankWithEmptyString)
            },
            {
                BuiltinFunctionsCore.LnT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.LnT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Ln], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.RadiansT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.RadiansT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Radians], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.SinT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.SinT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Sin], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.SqrtT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.SqrtT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Sqrt], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.TanT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.TanT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Tan], ReplaceBlankWithFloatZero)
            },
            {
                BuiltinFunctionsCore.UniCharT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.UniCharT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.UniChar], ReplaceBlankWithFloatZero)
            },
        };

        private static IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> SimpleFunctionMultiArgsTabularOverloadImplementations { get; } = new Dictionary<TexlFunction, AsyncFunctionPtr>
        {
            {
                BuiltinFunctionsCore.ColorFadeT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.ColorFade], DoNotReplaceBlank))
            },
            {
                BuiltinFunctionsCore.ConcatenateT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.Concatenate], DoNotReplaceBlank))
            },
            {
                BuiltinFunctionsCore.Dec2HexT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.Dec2Hex], ReplaceBlankWithFloatZero))
            },
            {
                BuiltinFunctionsCore.FindT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.Find], DoNotReplaceBlank))
            },
            {
                BuiltinFunctionsCore.LeftST,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Left],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.LeftTS,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Left],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.LeftTT,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Left],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.LogT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.Log], ReplaceBlankWithFloatZero))
            },
            {
                BuiltinFunctionsCore.MidT,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Mid],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.ModT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.Mod], ReplaceBlankWithCallZero))
            },
            {
                BuiltinFunctionsCore.PowerT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.Power], ReplaceBlankWithFloatZero))
            },
            {
                BuiltinFunctionsCore.RightST,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Right],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.RightTS,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Right],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.RightTT,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Right],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.RoundT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.Round], ReplaceBlankWithCallZeroButFloatZeroForSpecificIndices(1)))
            },
            {
                BuiltinFunctionsCore.RoundUpT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.RoundUp], ReplaceBlankWithCallZeroButFloatZeroForSpecificIndices(1)))
            },
            {
                BuiltinFunctionsCore.RoundDownT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.RoundDown], ReplaceBlankWithCallZeroButFloatZeroForSpecificIndices(1)))
            },
            {
                BuiltinFunctionsCore.SubstituteT,
                NoErrorHandling(
                    MultiSingleColumnTable(
                        SimpleFunctionImplementations[BuiltinFunctionsCore.Substitute],
                        ReplaceBlankWith(
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                            new NumberValue(IRContext.NotInSource(FormulaType.Number), 0))))
            },
            {
                BuiltinFunctionsCore.TruncT,
                NoErrorHandling(MultiSingleColumnTable(SimpleFunctionImplementations[BuiltinFunctionsCore.RoundDown], ReplaceBlankWithCallZeroButFloatZeroForSpecificIndices(1)))
            },
        };

        public static IEnumerable<DValue<RecordValue>> StandardTableNodeRecords(IRContext irContext, FormulaValue[] args, bool forceSingleColumn)
        {
            var tableType = (TableType)irContext.ResultType;
            var recordType = tableType.ToRecord();
            return args.Select(arg =>
            {
                if (!forceSingleColumn && arg is RecordValue rv)
                {
                    return DValue<RecordValue>.Of(rv);
                }
                else if (!forceSingleColumn && arg is BlankValue bv && bv.Type._type.IsRecord)
                {
                    return DValue<RecordValue>.Of(bv);
                }

                // Handle the single-column-table case. 
                var columnName = tableType.SingleColumnFieldName;
                var defaultField = new NamedValue(columnName, arg);
                return DValue<RecordValue>.Of(new InMemoryRecordValue(IRContext.NotInSource(recordType), new List<NamedValue>() { defaultField }));
            });
        }

        public static FormulaValue Table(IRContext irContext, FormulaValue[] args)
        {
            // Table literal 
            var table = new List<DValue<RecordValue>>();

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case TableValue t:
                        table.AddRange(t.Rows);
                        break;
                    case RecordValue r:
                        table.Add(DValue<RecordValue>.Of(r));
                        break;
                    case BlankValue b when b.Type._type.IsTableNonObjNull:
                        break;
                    case BlankValue b:
                        table.Add(DValue<RecordValue>.Of(b));
                        break;
                    case ErrorValue e when e.Type._type.IsTableNonObjNull:
                        return e;
                    default:
                        table.Add(DValue<RecordValue>.Of((ErrorValue)args[i]));
                        break;
                }
            }

            // Returning List to ensure that the returned table is mutable
            return new InMemoryTableValue(irContext, table);
        }

        public static ValueTask<FormulaValue> Blank(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var result = new BlankValue(irContext);
            return new ValueTask<FormulaValue>(result);
        }

        public static FormulaValue IsBlank(IRContext irContext, FormulaValue[] args)
        {
            // Blank or empty. 
            var arg0 = args[0];
            return new BooleanValue(irContext, IsBlankOrEmpty(arg0));
        }

        internal static bool IsBlank(this FormulaValue arg)
        {
            if ((arg is BlankValue) || (arg is UntypedObjectValue uo && uo.Impl.Type == FormulaType.Blank))
            {
                return true;
            }

            return false;
        }

        private static bool IsBlankOrEmpty(FormulaValue arg)
        {
            if (arg is StringValue str)
            {
                return str.Value.Length == 0;
            }

            if (arg is UntypedObjectValue uo && uo.Impl.Type == FormulaType.String)
            {
                return uo.Impl.GetString().Length == 0;
            }

            return arg.IsBlank();
        }

        private static FormulaValue IsEmpty(IRContext irContext, FormulaValue[] args)
        {
            if (args[0] is BlankValue)
            {
                return new BooleanValue(irContext, true);
            }

            if (args[0] is TableValue tv)
            {
                return new BooleanValue(irContext, !tv.Rows.Any());
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        public static FormulaValue IsNumeric(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = args[0];
            switch (arg0)
            {
                case NumberValue _:
                case DecimalValue _:
                case DateValue _:
                case DateTimeValue _:
                case TimeValue _:
                    return new BooleanValue(irContext, true);
                case StringValue _:
                    var nv = Value(runner, context, IRContext.NotInSource(FormulaType.Number), args);
                    return new BooleanValue(irContext, nv is NumberValue);
                default:
                    return new BooleanValue(irContext, false);
            }
        }

        public static async ValueTask<FormulaValue> With(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (RecordValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var childContext = context.SymbolContext.WithScopeValues(arg0);

            return await arg1.EvalInRowScopeAsync(context.NewScope(childContext)).ConfigureAwait(false);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-if
        // If(Condition, Then)
        // If(Condition, Then, Else)
        // If(Condition, Then, Condition2, Then2)
        // If(Condition, Then, Condition2, Then2, Default)
        public static async ValueTask<FormulaValue> If(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            for (var i = 0; i < args.Length - 1; i += 2)
            {
                runner.CheckCancel();

                var res = await runner.EvalArgAsync<BooleanValue>(args[i], context, args[i].IRContext).ConfigureAwait(false);

                if (res.IsValue)
                {
                    var test = res.Value;
                    if (test.Value)
                    {
                        var trueBranch = args[i + 1];

                        var trueBranchResult = (await runner.EvalArgAsync<ValidFormulaValue>(trueBranch, context, trueBranch.IRContext).ConfigureAwait(false)).ToFormulaValue();
                        return MaybeAdjustToCompileTimeType(trueBranchResult, irContext);
                    }
                }

                if (res.IsError)
                {
                    // Update error "type" to the type of the If function
                    var resultContext = new IRContext(res.Error.IRContext.SourceContext, irContext.ResultType);
                    return new ErrorValue(resultContext, res.Error.Errors.ToList());
                }

                // False branch
                // If it's the last value in the list, it's the false-value
                if (i + 2 == args.Length - 1)
                {
                    var falseBranch = args[i + 2];
                    var falseBranchResult = (await runner.EvalArgAsync<ValidFormulaValue>(falseBranch, context, falseBranch.IRContext).ConfigureAwait(false)).ToFormulaValue();

                    return MaybeAdjustToCompileTimeType(falseBranchResult, irContext);
                }

                // Else, if there are more values, this is another conditional.
                // It's another condition. Loop around
            }

            // If there's no value here, then use blank or void (for example, "If(false,Set(x,5))")
            return irContext.ResultType == FormulaType.Void ? new VoidValue(irContext) : new BlankValue(irContext);
        }

        private static FormulaValue MaybeAdjustToCompileTimeType(FormulaValue result, IRContext irContext)
        {
            if (irContext.ResultType is Types.Void)
            {
                if (result is ErrorValue ev)
                {
                    return new ErrorValue(IRContext.NotInSource(FormulaType.Void), (List<ExpressionError>)ev.Errors);
                }

                return new VoidValue(irContext);
            }
            else if (result is BlankValue && result.IRContext.ResultType._type.Kind == DKind.ObjNull)
            {
                return new BlankValue(irContext); // Convert the untyped blank to a typed blank value
            }
            else if (result is RecordValue recordValue && irContext.ResultType is RecordType compileTimeType)
            {
                return CompileTimeTypeWrapperRecordValue.AdjustType(compileTimeType, recordValue);
            }
            else if (result is TableValue tableValue && irContext.ResultType is TableType compileTimeTableType)
            {
                return CompileTimeTypeWrapperTableValue.AdjustType(compileTimeTableType, tableValue);
            }

            return result;
        }

        public static async ValueTask<FormulaValue> IfError(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            for (var i = 0; i < args.Length - 1; i += 2)
            {
                runner.CheckCancel();

                var res = await runner.EvalArgAsync<ValidFormulaValue>(args[i], context, args[i].IRContext).ConfigureAwait(false);

                if (res.IsError)
                {
                    var errorHandlingBranch = args[i + 1];
                    var allErrors = new List<RecordValue>();
                    foreach (var error in res.Error.Errors)
                    {
                        runner.CheckCancel();

                        var kindProperty = new NamedValue("Kind", FormulaValue.New((double)error.Kind));
                        var messageProperty = new NamedValue(
                            "Message",
                            error.Message == null ? FormulaValue.NewBlank(FormulaType.String) : FormulaValue.New(error.Message));
                        var errorScope = new InMemoryRecordValue(
                            IRContext.NotInSource(new KnownRecordType(ErrorType.ReifiedError())),
                            new[] { kindProperty, messageProperty });
                        allErrors.Add(errorScope);
                    }

                    var scopeVariables = new NamedValue[]
                    {
                        new NamedValue("FirstError", allErrors.First()),
                        new NamedValue(
                            "AllErrors",
                            new InMemoryTableValue(
                                IRContext.NotInSource(new TableType(ErrorType.ReifiedErrorTable())),
                                allErrors.Select(e => DValue<RecordValue>.Of(e))))
                    };

                    var ifErrorScopeParamType = new KnownRecordType(DType.CreateRecord(
                        new[]
                        {
                            new TypedName(ErrorType.ReifiedError(), new DName("FirstError")),
                            new TypedName(ErrorType.ReifiedErrorTable(), new DName("AllErrors")),
                        }));

                    var childContext = context.SymbolContext.WithScopeValues(new InMemoryRecordValue(IRContext.NotInSource(ifErrorScopeParamType), scopeVariables));

                    var errorHandlingBranchResult = (await runner.EvalArgAsync<ValidFormulaValue>(errorHandlingBranch, context.NewScope(childContext), errorHandlingBranch.IRContext).ConfigureAwait(false)).ToFormulaValue();
                    return MaybeAdjustToCompileTimeType(errorHandlingBranchResult, irContext);
                }

                if (i + 1 == args.Length - 1)
                {
                    return MaybeAdjustToCompileTimeType(res.ToFormulaValue(), irContext);
                }

                if (i + 2 == args.Length - 1)
                {
                    var otherwiseBranch = args[i + 2];
                    var otherwiseBranchResult = (await runner.EvalArgAsync<ValidFormulaValue>(otherwiseBranch, context, otherwiseBranch.IRContext).ConfigureAwait(false)).ToFormulaValue();
                    return MaybeAdjustToCompileTimeType(otherwiseBranchResult, irContext);
                }
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        // Error({Kind:<error kind>,Message:<error message>})
        // Error(Table({Kind:<error kind 1>,Message:<error message 1>}, {Kind:<error kind 2>,Message:<error message 2>}))
        // Error(<error message>)
        public static FormulaValue Error(IRContext irContext, FormulaValue[] args)
        {
            var result = new ErrorValue(irContext);

            var errorRecords = new List<RecordValue>();
            if (args[0] is RecordValue singleErrorRecord)
            {
                errorRecords.Add(singleErrorRecord);
            }
            else if (args[0] is TableValue errorTable)
            {
                foreach (var errorRow in errorTable.Rows)
                {
                    if (errorRow.IsValue)
                    {
                        errorRecords.Add(errorRow.Value);
                    }
                }
            }
            else if (args[0] is StringValue stringValue)
            {
                //Error("Custom error message").This is equivalent to Error({ Kind: ErrorKind.Custom, Message: "Custom error message"}).
                return CommonErrors.CustomError(irContext, stringValue.Value);
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            foreach (var errorRecord in errorRecords)
            {
                var kindField = errorRecord.GetField(ErrorType.KindFieldName);
                if (kindField is ErrorValue error)
                {
                    return error;
                }

                ErrorKind errorKind;
                switch (kindField)
                {
                    case NumberValue nv:
                        errorKind = (ErrorKind)(int)nv.Value;
                        break;
                    case DecimalValue dv:
                        errorKind = (ErrorKind)(int)dv.Value;
                        break;
                    case OptionSetValue osv:
                        errorKind = (ErrorKind)Convert.ToInt32(osv.ExecutionValue, CultureInfo.InvariantCulture);
                        break;
                    default:
                        return CommonErrors.RuntimeTypeMismatch(irContext);
                }

                var message = errorRecord.GetField(ErrorType.MessageFieldName) is StringValue messageField ? messageField.Value : GetDefaultErrorMessage(errorKind);
                result.Add(new ExpressionError { Kind = errorKind, Message = message });
            }

            return result;
        }

        private static string GetDefaultErrorMessage(ErrorKind errorKind)
        {
            switch (errorKind)
            {
                case ErrorKind.None:
                    // Default message that is shown to users when an error was produced, but with kind = 'none'
                    return "System no error";
                case ErrorKind.Sync:
                case ErrorKind.Unknown:
                case ErrorKind.Internal:
                    // Default message that is shown to users when a system error was produced
                    return "System error";
                case ErrorKind.MissingRequired:
                    // Default message that is shown to users when they try to insert / update a record without all the required fields
                    return "Missing required field";
                case ErrorKind.CreatePermission:
                    // Default message that is shown to users when they try to create a record without the appropriate permissions
                    return "Create record permission denied";
                case ErrorKind.EditPermissions:
                    // Default message that is shown to users when they try to update a record without the appropriate permissions
                    return "Update record permission denied";
                case ErrorKind.DeletePermissions:
                    // Default message that is shown to users when they try to remove a record without the appropriate permissions
                    return "Delete record permission denied";
                case ErrorKind.GeneratedValue:
                    // Default message that is shown to users when they try to insert / update a record with a column that is generated on the server
                    return "Column is generated by the server and cannot be modified";
                case ErrorKind.Conflict:
                    // Default message that is shown to users when they try to update a record but there is a conflict on the server
                    return "Record update conflict, refresh record and reapply your change";
                case ErrorKind.NotFound:
                    // Default message that is shown to users when they try to access a record that does not exist
                    return "Record could not be found";
                case ErrorKind.ConstraintViolated:
                    // Default message that is shown to users when they try to create or update a record but it validates some constraints set on the server
                    return "Validation error";
                case ErrorKind.ReadOnlyValue:
                    // Default message that is shown to users when they try to update a column in a record that is read-only
                    return "Column is read-only";
                case ErrorKind.Validation:
                    // Default message that is shown to users when they try to send a record to the server with invalid properties
                    return "Record is invalid";
                case ErrorKind.Div0:
                    // Default message that is shown to users when they try to divide a number by zero
                    return "Division by zero";
                case ErrorKind.BadLanguageCode:
                    // Default message that is shown to users when they try to pass a language code to a function that is invalid
                    return "Bad langauge code or invalid value";
                case ErrorKind.BadRegex:
                    // Default message that is shown to users when they use an invalid regular expression in one of their formulas
                    return "Syntax error in regular expression";
                case ErrorKind.InvalidFunctionUsage:
                    // Default message that is shown to users when they try to use a function in an invalid way
                    return "Invalid function usage";
                case ErrorKind.FileNotFound:
                    // Default message that is shown to users when they try to access a file that does not exist
                    return "File not found";
                case ErrorKind.AnalysisError:
                    // Default message that is shown to users when they encounter an analysis error from Power Apps
                    return "System analysis error";
                case ErrorKind.ReadPermission:
                    // Default message that is shown to users when they try to read a record for which they do not have permissions
                    return "Read record permission denied";
                case ErrorKind.NotSupported:
                    // Default message that is shown to users when they create try to call a function that is not supported in their current environment
                    return "Operation not supported by this player or device";
                case ErrorKind.InsufficientMemory:
                    // Default message that is shown to users when they try to perform an operation that exhausts the memory/storage in their device
                    return "Insufficient memory or device storage";
                case ErrorKind.QuotaExceeded:
                    // Default message that is shown to users when they try to use more storage quota than they have access to
                    return "Storage quota exceeded";
                case ErrorKind.Network:
                    // Default message that is shown to users when they receive an error over the network
                    return "Network error";
                case ErrorKind.Numeric:
                    // Default message that is shown to users when they try to use a numerical function in an erroneous way
                    return "Numeric error";
                case ErrorKind.InvalidArgument:
                    // Default message that is shown to users when they pass an invalid argument to a function
                    return "Invalid argument";
                case ErrorKind.NotApplicable:
                    // Default message that is shown to users when they try to combine tables of different lenghts in a tabular function
                    return "Not applicable";
                case ErrorKind.Timeout:
                    // Default message that is shown to users when they execute an operation that was cancelled because of a timeout
                    return "Timeout error";
                case ErrorKind.ServiceUnavailable:
                    // Default message that is shown to users when they execute an operation requires a online service connection that is not available
                    return "Online service connection not available";
                case ErrorKind.Custom:
                    // Default message that is shown to users when they create an error with a custom kind
                    return "Custom error";
                default:
                    var intKind = (int)errorKind;
                    if (intKind < 1000)
                    {
                        // Default message that is shown to users when they create an error with a custom kind within a range of reserved values. The argument is the error kind, a number
                        return $"Reserved error ({intKind})";
                    }

                    // Default message that is shown to users when they create an error with a custom kind. The argument is the error kind, a number
                    return $"Custom error ({intKind})";
            }
        }

        // Switch( Formula, Match1, Result1 [, Match2, Result2, ... [, DefaultResult ] ] )
        // Switch(Formula, Match1, Result1, Match2,Result2)
        // Switch(Formula, Match1, Result1, DefaultResult)
        // Switch(Formula, Match1, Result1)
        public static async ValueTask<FormulaValue> Switch(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var test = args[0];

            var errors = new List<ErrorValue>();

            if (test is ErrorValue te)
            {
                errors.Add(te);
            }

            for (var i = 1; i < args.Length - 1; i += 2)
            {
                runner.CheckCancel();

                var match = (LambdaFormulaValue)args[i];
                var matchValue = await match.EvalAsync().ConfigureAwait(false);

                if (matchValue is ErrorValue mve)
                {
                    errors.Add(mve);
                }

                var equal = RuntimeHelpers.AreEqual(test, matchValue);

                // Comparison? 

                if (equal)
                {
                    var lambda = (LambdaFormulaValue)args[i + 1];
                    var result = await lambda.EvalAsync().ConfigureAwait(false);
                    if (errors.Count != 0)
                    {
                        return ErrorValue.Combine(irContext, errors);
                    }
                    else
                    {
                        return MaybeAdjustToCompileTimeType(result, irContext);
                    }
                }
            }

            // Min length is 3. 
            // 4,6,8.. mean we have an unpaired (match,result), which is a
            // a default result at the end
            if ((args.Length - 4) % 2 == 0)
            {
                var lambda = (LambdaFormulaValue)args[args.Length - 1];
                var result = await lambda.EvalAsync().ConfigureAwait(false);
                if (errors.Count != 0)
                {
                    return ErrorValue.Combine(irContext, errors);
                }
                else
                {
                    return MaybeAdjustToCompileTimeType(result, irContext);
                }
            }

            // No match
            if (errors.Count != 0)
            {
                return ErrorValue.Combine(irContext, errors);
            }
            else
            {
                // If there's no value here, then use blank or void (for example, "Switch(4,5,Set(x,6))")
                return irContext.ResultType == FormulaType.Void ? new VoidValue(irContext) : new BlankValue(irContext);
            }
        }

        // ForAll([1,2,3,4,5], Value * Value)
        public static async ValueTask<FormulaValue> ForAll(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            // Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var rowsAsync = LazyForAll(runner, context, arg0.Rows, arg1);

            var rows = new List<FormulaValue>();

            foreach (var row in rowsAsync)
            {
                runner.CheckCancel();
                rows.Add(await row.ConfigureAwait(false));
            }

            var errorRows = rows.OfType<ErrorValue>();
            if (errorRows.Any())
            {
                return ErrorValue.Combine(irContext, errorRows);
            }

            if (irContext.ResultType is Types.Void)
            {
                return new VoidValue(irContext);
            }

            return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray(), forceSingleColumn: false));
        }

        private static IEnumerable<Task<FormulaValue>> LazyForAll(
            EvalVisitor runner,
            EvalVisitorContext context,
            IEnumerable<DValue<RecordValue>> sources,
            LambdaFormulaValue filter)
        {
            foreach (var row in sources)
            {
                runner.CheckCancel();

                SymbolContext childContext = context.SymbolContext.WithScopeValues(row.ToFormulaValue());

                // Filter evals to a boolean
                var result = filter.EvalInRowScopeAsync(context.NewScope(childContext)).AsTask();

                yield return result;
            }
        }

        private static IEnumerable<Task<FormulaValue>> LazyForAll(
            EvalVisitor runner,
            EvalVisitorContext context,
            IEnumerable<DValue<UntypedObjectValue>> sources,
            LambdaFormulaValue filter)
        {
            foreach (var row in sources)
            {
                runner.CheckCancel();

                SymbolContext childContext = context.SymbolContext.WithScopeValues(row.ToFormulaValue());

                // Filter evals to a boolean
                var result = filter.EvalInRowScopeAsync(context.NewScope(childContext)).AsTask();

                yield return result;
            }
        }

        public static FormulaValue IsError(IRContext irContext, FormulaValue[] args)
        {
            var result = args[0] is ErrorValue;
            return new BooleanValue(irContext, result);
        }

        public static FormulaValue IsBlankOrError(IRContext irContext, FormulaValue[] args)
        {
            if (IsBlankOrEmpty(args[0]) || args[0] is ErrorValue)
            {
                return new BooleanValue(irContext, true);
            }

            return new BooleanValue(irContext, false);
        }

        public static async ValueTask<FormulaValue> TraceFunction(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var tracer = runner.FunctionServices.GetService<ITracer>();

            if (tracer == null)
            {
                return FormulaValue.New(false);
            }

            // the null case here handles Blanks
            var message = (args[0] as StringValue)?.Value ?? string.Empty;

            TraceSeverity sev;
            if (args.Length < 2 || args[1] is BlankValue)
            {
                sev = TraceSeverity.Information;
            }
            else
            {
                switch (args[1])
                {
                    case NumberValue nv:
                        sev = (TraceSeverity)nv.Value;
                        break;
                    case OptionSetValue osv:
                        sev = (TraceSeverity)(double)osv.ExecutionValue;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            RecordValue customRecord;
            if (args.Length < 3 || args[2] is BlankValue)
            {
                customRecord = RecordValue.Empty();
            }
            else
            {
                customRecord = args[2] as RecordValue;
            }

            try
            {
                await tracer.LogAsync(message, sev, customRecord, runner.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new CustomFunctionErrorException(ex.Message);
            }

            return FormulaValue.New(true);
        }
    }
}
