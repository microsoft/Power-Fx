// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Texl;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        public delegate FormulaValue FunctionPtr(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args);

        public static IEnumerable<TexlFunction> FunctionList => _funcsByName.Keys;

        // Some TexlFunctions are overloaded
        private static readonly Dictionary<TexlFunction, FunctionPtr> _funcsByName = new Dictionary<TexlFunction, FunctionPtr>
        {
            {
                BuiltinFunctionsCore.Abs,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Abs)
            },
            {
                BuiltinFunctionsCore.AddColumns,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: AddColumnsTypeChecker,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: AddColumns)
            },
            { BuiltinFunctionsCore.And, And },
            {
                BuiltinFunctionsCore.Average,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Average)
            },
            {
                BuiltinFunctionsCore.AverageT,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: AverageTable)
            },
            { BuiltinFunctionsCore.Blank, Blank },
            {
                BuiltinFunctionsCore.Boolean,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Boolean)
            },
            {
                BuiltinFunctionsCore.Boolean_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Boolean_UO)
            },
            {
                BuiltinFunctionsCore.Concat,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Concat)
            },
            { BuiltinFunctionsCore.Coalesce, Coalesce },
            {
                BuiltinFunctionsCore.Char,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Char)
            },
            {
                BuiltinFunctionsCore.CharT,
                StandardErrorHandling(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: StandardSingleColumnTable<NumberValue>(Char))
            },
            {
                BuiltinFunctionsCore.Concatenate,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Concatenate)
            },
            {
                BuiltinFunctionsCore.ConcatenateT,
                StandardErrorHandling(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrTableOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: MultiSingleColumnTable(
                            StandardErrorHandling<StringValue>(
                                expandArguments: NoArgExpansion,
                                replaceBlankValues: ReplaceBlankWithEmptyString,
                                checkRuntimeTypes: ExactValueType<StringValue>,
                                checkRuntimeValues: DeferRuntimeValueChecking,
                                returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                                targetFunction: Concatenate),
                            transposeEmptyTable: true))
            },
            {
                BuiltinFunctionsCore.CountIf,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<LambdaFormulaValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: CountIf)
            },
            {
                BuiltinFunctionsCore.CountRows,
                StandardErrorHandling<TableValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: CountRows)
            },
            {
                BuiltinFunctionsCore.Date,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: ExactSequence(
                        PositiveNumberChecker,
                        FiniteChecker,
                        FiniteChecker),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Date)
            },
            {
                BuiltinFunctionsCore.DateAdd,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                    replaceBlankValues: ReplaceBlankWith(
                        new BlankValue(IRContext.NotInSource(FormulaType.Blank)),
                        new NumberValue(IRContext.NotInSource(FormulaType.Number), 0),
                        new StringValue(IRContext.NotInSource(FormulaType.String), "days")),
                    checkRuntimeTypes: ExactSequence(
                        DateOrDateTime,
                        ExactValueTypeOrBlank<NumberValue>,
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateAdd)
            },
            {
                BuiltinFunctionsCore.DateDiff,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new BlankValue(IRContext.NotInSource(FormulaType.Blank))),
                    replaceBlankValues: ReplaceBlankWith(
                        new BlankValue(IRContext.NotInSource(FormulaType.Blank)),
                        new BlankValue(IRContext.NotInSource(FormulaType.Blank)),
                        new StringValue(IRContext.NotInSource(FormulaType.String), "days")),
                    checkRuntimeTypes: ExactSequence(
                        DateOrDateTime,
                        DateOrDateTime,
                        ExactValueTypeOrBlank<StringValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateDiff)
            },
            {
                BuiltinFunctionsCore.DateValue,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateParse)
            },
            {
                BuiltinFunctionsCore.DateTimeValue,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: DateTimeParse)
            },
            {
                BuiltinFunctionsCore.Day,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Day)
            },
            {
                BuiltinFunctionsCore.EndsWith,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnFalseIfAnyArgIsBlank,
                    targetFunction: EndsWith)
            },
            {
                BuiltinFunctionsCore.Exp,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Exp)
            },
            {
                BuiltinFunctionsCore.Filter,
                StandardErrorHandling<FormulaValue>(
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
                        StrictPositiveNumberChecker),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Find)
            },
            {
                BuiltinFunctionsCore.FindT,
                StandardErrorHandling<FormulaValue>(
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
                                    StrictPositiveNumberChecker),
                                returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                                targetFunction: Find),
                            transposeEmptyTable: false))
            },
            {
                BuiltinFunctionsCore.First,
                StandardErrorHandling<TableValue>(
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
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: FirstN)
            },
            {
                BuiltinFunctionsCore.ForAll,
                StandardErrorHandling<FormulaValue>(
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
                BuiltinFunctionsCore.IsBlank,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: IsBlank)
            },
            {
                BuiltinFunctionsCore.IsError,
                IsError
            },
            {
                BuiltinFunctionsCore.IsNumeric,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnFalseIfAnyArgIsBlank,
                    targetFunction: IsToday)
            },
            { BuiltinFunctionsCore.If, If },
            { BuiltinFunctionsCore.IfError, IfError },
            {
                BuiltinFunctionsCore.Int,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Int)
            },
            {
                BuiltinFunctionsCore.Hour,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: TimeOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Hour)
            },
            {
                BuiltinFunctionsCore.Index_UO,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<UntypedObjectValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: ExactSequence(
                        UntypedObjectArrayChecker,
                        StrictPositiveNumberChecker),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Index_UO)
            },
            {
                BuiltinFunctionsCore.Last,
                StandardErrorHandling<TableValue>(
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
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<TableValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: LastN)
            },
            {
                BuiltinFunctionsCore.Left,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: PositiveNumberChecker,
                    returnBehavior: ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank,
                    targetFunction: Left)
            },
            {
                BuiltinFunctionsCore.Len,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Len)
            },
            {
                BuiltinFunctionsCore.LenT,
                StandardErrorHandling(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: StandardSingleColumnTable<StringValue>(Len))
            },
            {
                BuiltinFunctionsCore.Ln,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: StrictPositiveNumberChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Ln)
            },
            {
                BuiltinFunctionsCore.Log,
                StandardErrorHandling<NumberValue>(
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 10)),
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: StrictPositiveNumberChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Log)
            },
            {
                BuiltinFunctionsCore.Lower,
                StandardErrorHandling<StringValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Max)
            },
            {
                BuiltinFunctionsCore.MaxT,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Min)
            },
            {
                BuiltinFunctionsCore.MinT,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: DivideByZeroChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Mod)
            },
            {
                BuiltinFunctionsCore.Month,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWith(new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false)),
                    checkRuntimeTypes: ExactValueType<BooleanValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Not)
            },
            {
                BuiltinFunctionsCore.Now,
                Now
            },
            { BuiltinFunctionsCore.Or, Or },
            {
                BuiltinFunctionsCore.ParseJson,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: ParseJson)
            },
            {
                BuiltinFunctionsCore.Power,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Power)
            },
            {
                BuiltinFunctionsCore.Rand,
                Rand
            },
            {
                BuiltinFunctionsCore.RandBetween,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RandBetween)
            },
            {
                BuiltinFunctionsCore.Replace,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactSequence(
                        ExactValueTypeOrBlank<StringValue>,
                        ExactValueTypeOrBlank<NumberValue>),
                    checkRuntimeValues: PositiveNumberChecker,
                    returnBehavior: ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank,
                    targetFunction: Right)
            },
            {
                BuiltinFunctionsCore.Round,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Round)
            },
            {
                BuiltinFunctionsCore.RoundUp,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundUp)
            },
            {
                BuiltinFunctionsCore.RoundDown,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundDown)
            },
            {
                BuiltinFunctionsCore.Second,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: InsertDefaultValues(outputArgsCount: 3, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 1)),
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Sequence)
            },
            {
                BuiltinFunctionsCore.Sort,
                StandardErrorHandling<FormulaValue>(
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
                BuiltinFunctionsCore.StartsWith,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnFalseIfAnyArgIsBlank,
                    targetFunction: StartsWith)
            },
            {
                BuiltinFunctionsCore.StringInterpolation,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Concatenate)
            },
            {
                BuiltinFunctionsCore.Sum,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Sum)
            },
            {
                BuiltinFunctionsCore.SumT,
                StandardErrorHandling<FormulaValue>(
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
                BuiltinFunctionsCore.Split,
                StandardErrorHandling<StringValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: PositiveNumberChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Sqrt)
            },
            {
                BuiltinFunctionsCore.Substitute,
                StandardErrorHandling<FormulaValue>(
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
                    checkRuntimeValues: StrictPositiveNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Substitute)
            },
            { BuiltinFunctionsCore.Switch, Switch },
            {
                BuiltinFunctionsCore.Table,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Table)
            },
            {
                BuiltinFunctionsCore.Table_UO,
                StandardErrorHandling<UntypedObjectValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: UntypedObjectArrayChecker,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Table_UO)
            },
            {
                BuiltinFunctionsCore.Text,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank,
                    targetFunction: Text_UO)
            },
            {
                BuiltinFunctionsCore.Time,
                StandardErrorHandling<NumberValue>(
                    expandArguments: InsertDefaultValues(outputArgsCount: 4, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Time)
            },
            {
                BuiltinFunctionsCore.TimeValue,
                StandardErrorHandling<StringValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TimeParse)
            },
            {
                BuiltinFunctionsCore.TimeZoneOffset,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: TimeZoneOffset)
            },
            { BuiltinFunctionsCore.Today, Today },
            {
                BuiltinFunctionsCore.Trim,
                StandardErrorHandling<StringValue>(
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
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueType<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: RoundDown)
            },
            {
                BuiltinFunctionsCore.Upper,
                StandardErrorHandling<StringValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Value_UO)
            },
            {
                BuiltinFunctionsCore.With,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: DateOrDateTime,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Year)
            }
        };

        public static IReadOnlyDictionary<TexlFunction, FunctionPtr> FuncsByName => _funcsByName;

        public static IEnumerable<DValue<RecordValue>> StandardTableNodeRecords(IRContext irContext, FormulaValue[] args)
        {
            return StandardTableNodeRecordsCore(irContext, args);
        }

        public static IEnumerable<DValue<RecordValue>> StandardSingleColumnTableFromValues(IRContext irContext, FormulaValue[] args, string columnName)
        {
            return StandardTableNodeRecordsCore(irContext, args, columnName);
        }

        private static IEnumerable<DValue<RecordValue>> StandardTableNodeRecordsCore(IRContext irContext, FormulaValue[] args, string columnName = BuiltinFunction.ColumnName_ValueStr)
        {
            var tableType = (TableType)irContext.ResultType;
            var recordType = tableType.ToRecord();
            return args.Select(arg =>
            {
                if (arg is RecordValue record)
                {
                    return DValue<RecordValue>.Of(record);
                }

                // Handle the single-column-table case. 
                var defaultField = new NamedValue(columnName, arg);
                return DValue<RecordValue>.Of(new InMemoryRecordValue(IRContext.NotInSource(recordType), new List<NamedValue>() { defaultField }));
            });
        }

        public static FormulaValue Table(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
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
            return new InMemoryTableValue(irContext, records);
        }

        public static FormulaValue Blank(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            return new BlankValue(irContext);
        }

        public static FormulaValue IsBlank(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
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

        public static FormulaValue IsNumeric(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = args[0];
            return new BooleanValue(irContext, arg0 is NumberValue);
        }

        public static FormulaValue With(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var arg0 = (RecordValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var childContext = symbolContext.WithScopeValues(arg0);

            return arg1.Eval(runner, childContext);
        }

        // https://docs.microsoft.com/en-us/powerapps/maker/canvas-apps/functions/function-if
        // If(Condition, Then)
        // If(Condition, Then, Else)
        // If(Condition, Then, Condition2, Then2)
        // If(Condition, Then, Condition2, Then2, Default)
        public static FormulaValue If(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            for (var i = 0; i < args.Length - 1; i += 2)
            {
                var res = runner.EvalArg<BooleanValue>(args[i], symbolContext, args[i].IRContext);

                if (res.IsValue)
                {
                    var test = res.Value;
                    if (test.Value)
                    {
                        var trueBranch = args[i + 1];

                        return runner.EvalArg<ValidFormulaValue>(trueBranch, symbolContext, trueBranch.IRContext).ToFormulaValue();
                    }
                }

                if (res.IsError)
                {
                    return res.Error;
                }

                // False branch
                // If it's the last value in the list, it's the false-value
                if (i + 2 == args.Length - 1)
                {
                    var falseBranch = args[i + 2];
                    return runner.EvalArg<ValidFormulaValue>(falseBranch, symbolContext, falseBranch.IRContext).ToFormulaValue();
                }

                // Else, if there are more values, this is another conditional.
                // It's another condition. Loop around
            }

            // If there's no value here, then use blank. 
            return new BlankValue(irContext);
        }

        public static FormulaValue IfError(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            for (var i = 0; i < args.Length - 1; i += 2)
            {
                var res = runner.EvalArg<ValidFormulaValue>(args[i], symbolContext, args[i].IRContext);

                if (res.IsError)
                {
                    var trueBranch = args[i + 1];

                    return runner.EvalArg<ValidFormulaValue>(trueBranch, symbolContext, trueBranch.IRContext).ToFormulaValue();
                }

                if (i + 1 == args.Length - 1)
                {
                    return res.ToFormulaValue();
                }

                if (i + 2 == args.Length - 1)
                {
                    var falseBranch = args[i + 2];
                    return runner.EvalArg<ValidFormulaValue>(falseBranch, symbolContext, falseBranch.IRContext).ToFormulaValue();
                }
            }

            return CommonErrors.UnreachableCodeError(irContext);
        }

        // Switch( Formula, Match1, Result1 [, Match2, Result2, ... [, DefaultResult ] ] )
        // Switch(Formula, Match1, Result1, Match2,Result2)
        // Switch(Formula, Match1, Result1, DefaultResult)
        // Switch(Formula, Match1, Result1)
        public static FormulaValue Switch(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
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
                var matchValue = match.Eval(runner, symbolContext);

                if (matchValue is ErrorValue mve)
                {
                    errors.Add(mve);
                }

                var equal = RuntimeHelpers.AreEqual(test, matchValue);

                // Comparison? 

                if (equal)
                {
                    var lambda = (LambdaFormulaValue)args[i + 1];
                    var result = lambda.Eval(runner, symbolContext);
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
                var result = lambda.Eval(runner, symbolContext);
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
        public static FormulaValue ForAll(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {// Streaming 
            var arg0 = (TableValue)args[0];
            var arg1 = (LambdaFormulaValue)args[1];

            var rows = LazyForAll(runner, symbolContext, arg0.Rows, arg1);

            // TODO: verify semantics in the case of heterogeneous record lists
            return new InMemoryTableValue(irContext, StandardTableNodeRecords(irContext, rows.ToArray()));
        }

        private static IEnumerable<FormulaValue> LazyForAll(
            EvalVisitor runner,
            SymbolContext context,
            IEnumerable<DValue<RecordValue>> sources,
            LambdaFormulaValue filter)
        {
            foreach (var row in sources)
            {
                SymbolContext childContext;
                if (row.IsValue)
                {
                    childContext = context.WithScopeValues(row.Value);
                }
                else
                {
                    childContext = context.WithScopeValues(RecordValue.Empty());
                }

                // Filter evals to a boolean 
                var result = filter.Eval(runner, childContext);

                yield return result;
            }
        }

        public static FormulaValue IsError(EvalVisitor runner, SymbolContext symbolContext, IRContext irContext, FormulaValue[] args)
        {
            var result = args[0] is ErrorValue;
            return new BooleanValue(irContext, result);
        }
    }
}
