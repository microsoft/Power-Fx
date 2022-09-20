﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        private static readonly DateTime _epoch = new DateTime(1899, 12, 30, 0, 0, 0, 0);

        // Helper to get a service or fallback to a default if the service is missing.
        private static T GetService<T>(this IServiceProvider services, T defaultService)
        {
            var service = (T)services.GetService(typeof(T));
            return service ?? defaultService;
        }

        // Sync FunctionPtr - all args are evaluated before invoking this function.  
        public delegate FormulaValue FunctionPtr(SymbolContext symbolContext, IRContext irContext, FormulaValue[] args);

        // Async - can invoke lambads.
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

            FunctionImplementations = allFunctions;
        }

        // Some TexlFunctions are overloaded
        private static IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> SimpleFunctionImplementations { get; } = new Dictionary<TexlFunction, AsyncFunctionPtr>
        {
            {
                BuiltinFunctionsCore.Abs,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Abs.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Abs)
            },
            {
                BuiltinFunctionsCore.Acos,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Acos.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(Math.Asin))
            },
            {
                BuiltinFunctionsCore.Atan,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Atan.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
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
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Char)
            },
            {
                BuiltinFunctionsCore.Concatenate,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Concatenate.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Concatenate)
            },
            {
                BuiltinFunctionsCore.ConcatenateT,
                StandardErrorHandlingAsync(
                    BuiltinFunctionsCore.ConcatenateT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrTableOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: MultiSingleColumnTable(
                            StandardErrorHandling<StringValue>(
                                BuiltinFunctionsCore.Concatenate.Name,
                                expandArguments: NoArgExpansion,
                                replaceBlankValues: ReplaceBlankWithEmptyString,
                                checkRuntimeTypes: ExactValueType<StringValue>,
                                checkRuntimeValues: DeferRuntimeValueChecking,
                                returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                                targetFunction: Concatenate),
                            transposeEmptyTable: true))
            },
            {
                BuiltinFunctionsCore.Cos,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Cos.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                        DateOrDateTime,
                        ExactValueTypeOrBlank<NumberValue>,
                        ExactValueTypeOrBlank<StringValue>),
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
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateDiff)
            },
            {
                BuiltinFunctionsCore.DateTime,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.DateTime.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 7, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig(x => x * 180.0 / Math.PI))
            },
            {
                BuiltinFunctionsCore.EndsWith,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.EndsWith.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnFalseIfAnyArgIsBlank,
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                        new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueType<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: ExactSequence(
                        DeferRuntimeValueChecking,
                        DeferRuntimeValueChecking,
                        StrictArgumentPositiveNumberChecker),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Find)
            },
            {
                BuiltinFunctionsCore.FindT,
                StandardErrorHandlingAsync<FormulaValue>(
                    BuiltinFunctionsCore.FindT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrTableOrBlank<StringValue>,
                        ExactValueTypeOrTableOrBlank<StringValue>,
                        ExactValueTypeOrTableOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: MultiSingleColumnTable(
                            StandardErrorHandling<FormulaValue>(
                                BuiltinFunctionsCore.Find.Name,
                                expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                                replaceBlankValues: ReplaceBlankWith(
                                    new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                                    new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                                    new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                                checkRuntimeTypes: ExactSequence(
                                    ExactValueType<StringValue>,
                                    ExactValueType<StringValue>,
                                    ExactValueTypeOrBlank<NumberValue>),
                                checkRuntimeValues: ExactSequence(
                                    DeferRuntimeValueChecking,
                                    DeferRuntimeValueChecking,
                                    StrictArgumentPositiveNumberChecker),
                                returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                                targetFunction: Find),
                            transposeEmptyTable: false))
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
                    replaceBlankValues: ReplaceBlankWithZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: FirstN)
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
                BuiltinFunctionsCore.GUIDPure,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.GUIDPure.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Guid)
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
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Int.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Int)
            },
            {
                BuiltinFunctionsCore.Index,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Index.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
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
                    replaceBlankValues: DoNotReplaceBlank,
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
                BuiltinFunctionsCore.IsError,
                NoErrorHandling(IsError)
            },
            {
                BuiltinFunctionsCore.IsNumeric,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.IsNumeric.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
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
                    replaceBlankValues: ReplaceBlankWithZeroForSpecificIndices(1),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: LastN)
            },
            {
                BuiltinFunctionsCore.Left,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Left.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: PositiveNumericNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Left)
            },
            {
                BuiltinFunctionsCore.Len,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Len.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithEmptyString,
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
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Mod.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
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
                BuiltinFunctionsCore.ParseJSON,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.ParseJSON.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ParseJSON)
            },
            {
                BuiltinFunctionsCore.Proper,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Proper.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
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
                BuiltinFunctionsCore.Power,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Power.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
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
                    replaceBlankValues: ReplaceBlankWithZero,
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
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.RandBetween.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RandBetween)
            },
            {
                BuiltinFunctionsCore.Replace,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Replace.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new BlankValue(IRContext.NotInSource(FormulaType.Blank)),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty)),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>,
                        ExactValueType<NumberValue>,
                        ExactValueType<StringValue>),
                    checkRuntimeValues: ReplaceChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Replace)
            },
            {
                BuiltinFunctionsCore.Right,
                StandardErrorHandling<FormulaValue>(
                    BuiltinFunctionsCore.Right.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: PositiveNumericNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Right)
            },
            {
                BuiltinFunctionsCore.Round,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Round.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Round)
            },
            {
                BuiltinFunctionsCore.RoundT,
                StandardErrorHandlingAsync(
                    BuiltinFunctionsCore.RoundT.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrTableOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: MultiSingleColumnTable(
                            StandardErrorHandling<NumberValue>(
                                BuiltinFunctionsCore.Round.Name,
                                expandArguments: NoArgExpansion,
                                replaceBlankValues: ReplaceBlankWithEmptyString,
                                checkRuntimeTypes: ExactValueType<NumberValue>,
                                checkRuntimeValues: DeferRuntimeValueChecking,
                                returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                                targetFunction: Round),
                            transposeEmptyTable: true))
            },
            {
                BuiltinFunctionsCore.RoundUp,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.RoundUp.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundUp)
            },
            {
                BuiltinFunctionsCore.RoundDown,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.RoundDown.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundDown)
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
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Sequence.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
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
                BuiltinFunctionsCore.Sin,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Sin.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
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
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: SortTable)
            },
            {
                BuiltinFunctionsCore.Split,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Split.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
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
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeTypeChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Sqrt)
            },
            {
                BuiltinFunctionsCore.StartsWith,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.StartsWith.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnFalseIfAnyArgIsBlank,
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
                    expandArguments: InsertDefaultValues(outputArgsCount: 4, fillWith: new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                    replaceBlankValues: ReplaceBlankWith(
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new BlankValue(IRContext.NotInSource(FormulaType.Blank)),
                        new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty),
                        new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                    checkRuntimeTypes: ExactSequence(
                        ExactValueType<StringValue>,
                        ExactValueTypeOrBlank<StringValue>,
                        ExactValueType<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>),
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
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Sum)
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
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
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
                    returnBehavior: ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank,
                    targetFunction: Text)
            },
            {
                BuiltinFunctionsCore.Text_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.Text_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Text_UO)
            },
            {
                BuiltinFunctionsCore.Time,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Time.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 4, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
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
                    replaceBlankValues: DoNotReplaceBlank,
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
                BuiltinFunctionsCore.Trim,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Trim.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank,
                    targetFunction: Trim)
            },
            {
                BuiltinFunctionsCore.TrimEnds,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.TrimEnds.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank,
                    targetFunction: TrimEnds)
            },
            {
                BuiltinFunctionsCore.Trunc,
                StandardErrorHandling<NumberValue>(
                    BuiltinFunctionsCore.Trunc.Name,
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundDown)
            },
            {
                BuiltinFunctionsCore.Upper,
                StandardErrorHandling<StringValue>(
                    BuiltinFunctionsCore.Upper.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
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
                StandardErrorHandling<UntypedObjectValue>(
                    BuiltinFunctionsCore.Value_UO.Name,
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
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
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.AbsT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Abs])
            },
            {
                BuiltinFunctionsCore.Boolean_T,
                StandardErrorHandlingTabularOverload<StringValue>(BuiltinFunctionsCore.Boolean_T.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Boolean])
            },
            {
                BuiltinFunctionsCore.BooleanN_T,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.BooleanN_T.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.BooleanN])
            },
            {
                BuiltinFunctionsCore.CharT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.CharT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Char])
            },
            {
                BuiltinFunctionsCore.ExpT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.ExpT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Exp])
            },
            {
                BuiltinFunctionsCore.IntT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.IntT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Int])
            },
            {
                BuiltinFunctionsCore.LenT,
                StandardErrorHandlingTabularOverload<StringValue>(BuiltinFunctionsCore.LenT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Len])
            },
            {
                BuiltinFunctionsCore.LnT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.LnT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Ln])
            },
            {
                BuiltinFunctionsCore.SqrtT,
                StandardErrorHandlingTabularOverload<NumberValue>(BuiltinFunctionsCore.SqrtT.Name, SimpleFunctionImplementations[BuiltinFunctionsCore.Sqrt])
            },
        };

        public static IEnumerable<DValue<RecordValue>> StandardTableNodeRecords(IRContext irContext, FormulaValue[] args, bool forceSingleColumn)
        {
            var tableType = (TableType)irContext.ResultType;
            var recordType = tableType.ToRecord();
            return args.Select(arg =>
            {
                if (!forceSingleColumn && arg is RecordValue record)
                {
                    return DValue<RecordValue>.Of(record);
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
            var records = Array.ConvertAll(
                args,
                arg => arg switch
                {
                    RecordValue r => DValue<RecordValue>.Of(r),
                    BlankValue b => DValue<RecordValue>.Of(b),
                    _ => DValue<RecordValue>.Of((ErrorValue)arg),
                });

            // Returning List to ensure that the returned table is mutable
            return new InMemoryTableValue(irContext, records);
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
            return new BooleanValue(irContext, IsBlank(arg0));
        }

        public static bool IsBlank(FormulaValue arg)
        {
            if (arg is BlankValue)
            {
                return true;
            }

            if (arg is StringValue str)
            {
                return str.Value.Length == 0;
            }

            return false;
        }

        public static FormulaValue IsNumeric(IRContext irContext, FormulaValue[] args)
        {
            var arg0 = args[0];
            return new BooleanValue(irContext, arg0 is NumberValue);
        }

        public static async ValueTask<FormulaValue> With(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (RecordValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var childContext = context.SymbolContext.WithScopeValues(arg0);

            return await arg1.EvalAsync(runner, context.NewScope(childContext));
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

                var res = await runner.EvalArgAsync<BooleanValue>(args[i], context, args[i].IRContext);

                if (res.IsValue)
                {
                    var test = res.Value;
                    if (test.Value)
                    {
                        var trueBranch = args[i + 1];

                        return (await runner.EvalArgAsync<ValidFormulaValue>(trueBranch, context, trueBranch.IRContext)).ToFormulaValue();
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
                    return (await runner.EvalArgAsync<ValidFormulaValue>(falseBranch, context, falseBranch.IRContext)).ToFormulaValue();
                }

                // Else, if there are more values, this is another conditional.
                // It's another condition. Loop around
            }

            // If there's no value here, then use blank. 
            return new BlankValue(irContext);
        }

        public static async ValueTask<FormulaValue> IfError(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {
            for (var i = 0; i < args.Length - 1; i += 2)
            {
                runner.CheckCancel();

                var res = await runner.EvalArgAsync<ValidFormulaValue>(args[i], context, args[i].IRContext);

                if (res.IsError)
                {
                    var errorHandlingBranch = args[i + 1];
                    var allErrors = new List<RecordValue>();
                    foreach (var error in res.Error.Errors)
                    {
                        var kindProperty = new NamedValue("Kind", FormulaValue.New((int)error.Kind));
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

                    return (await runner.EvalArgAsync<ValidFormulaValue>(errorHandlingBranch, context.NewScope(childContext), errorHandlingBranch.IRContext)).ToFormulaValue();
                }

                if (i + 1 == args.Length - 1)
                {
                    return res.ToFormulaValue();
                }

                if (i + 2 == args.Length - 1)
                {
                    var falseBranch = args[i + 2];
                    return (await runner.EvalArgAsync<ValidFormulaValue>(falseBranch, context, falseBranch.IRContext)).ToFormulaValue();
                }
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        // Error({Kind:<error kind>,Message:<error message>})
        // Error(Table({Kind:<error kind 1>,Message:<error message 1>}, {Kind:<error kind 2>,Message:<error message 2>}))
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
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }

            foreach (var errorRecord in errorRecords)
            {
                var messageField = errorRecord.GetField(ErrorType.MessageFieldName) as StringValue;

                if (errorRecord.GetField(ErrorType.KindFieldName) is not NumberValue kindField)
                {
                    return CommonErrors.RuntimeTypeMismatch(irContext);
                }

                result.Add(new ExpressionError { Kind = (ErrorKind)kindField.Value, Message = messageField?.Value as string });
            }

            return result;
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
                var match = (LambdaFormulaValue)args[i];
                var matchValue = await match.EvalAsync(runner, context);

                if (matchValue is ErrorValue mve)
                {
                    errors.Add(mve);
                }

                var equal = RuntimeHelpers.AreEqual(test, matchValue);

                // Comparison? 

                if (equal)
                {
                    var lambda = (LambdaFormulaValue)args[i + 1];
                    var result = await lambda.EvalAsync(runner, context);
                    if (errors.Count != 0)
                    {
                        return ErrorValue.Combine(irContext, errors);
                    }
                    else
                    {
                        return result;
                    }
                }
            }

            // Min length is 3. 
            // 4,6,8.. mean we have an unpaired (match,result), which is a
            // a default result at the end
            if ((args.Length - 4) % 2 == 0)
            {
                var lambda = (LambdaFormulaValue)args[args.Length - 1];
                var result = await lambda.EvalAsync(runner, context);
                if (errors.Count != 0)
                {
                    return ErrorValue.Combine(irContext, errors);
                }
                else
                {
                    return result;
                }
            }

            // No match
            if (errors.Count != 0)
            {
                return ErrorValue.Combine(irContext, errors);
            }
            else
            {
                return new BlankValue(irContext);
            }
        }

        // ForAll([1,2,3,4,5], Value * Value)
        public static async ValueTask<FormulaValue> ForAll(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args)
        {// Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var rowsAsync = LazyForAll(runner, context, arg0.Rows, arg1);

            // TODO: verify semantics in the case of heterogeneous record lists
            var rows = await Task.WhenAll(rowsAsync);

            var errorRows = rows.OfType<ErrorValue>();
            if (errorRows.Any())
            {
                return ErrorValue.Combine(irContext, errorRows);
            }

            return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows, forceSingleColumn: false));
        }

        private static IEnumerable<Task<FormulaValue>> LazyForAll(
            EvalVisitor runner,
            EvalVisitorContext context,
            IEnumerable<DValue<RecordValue>> sources,
            LambdaFormulaValue filter)
        {
            foreach (var row in sources)
            {
                SymbolContext childContext;
                if (row.IsValue)
                {
                    childContext = context.SymbolContext.WithScopeValues(row.Value);
                }
                else if (row.IsError)
                {
                    childContext = context.SymbolContext.WithScopeValues(row.Error);
                }
                else
                {
                    childContext = context.SymbolContext.WithScopeValues(RecordValue.Empty());
                }

                // Filter evals to a boolean 
                var result = filter.EvalAsync(runner, context.NewScope(childContext)).AsTask();

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
            if (IsBlank(args[0]) || args[0] is ErrorValue)
            {
                return new BooleanValue(irContext, true);
            }

            return new BooleanValue(irContext, false);
        }
    }
}
