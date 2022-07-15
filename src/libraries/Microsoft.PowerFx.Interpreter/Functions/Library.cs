// Copyright (c) Microsoft Corporation.
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
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        // Sync FunctionPtr - all args are evaluated before invoking this function.  
        public delegate FormulaValue FunctionPtr(SymbolContext symbolContext, IRContext irContext, FormulaValue[] args);

        // Async - can invoke lambads.
        public delegate ValueTask<FormulaValue> AsyncFunctionPtr(EvalVisitor runner, EvalVisitorContext context, IRContext irContext, FormulaValue[] args);

        public static IEnumerable<TexlFunction> FunctionList => FuncsByName.Keys;

        // Some TexlFunctions are overloaded
        public static IReadOnlyDictionary<TexlFunction, AsyncFunctionPtr> FuncsByName { get; } = new Dictionary<TexlFunction, AsyncFunctionPtr>
        {
            {
                BuiltinFunctionsCore.Abs,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Abs)
            },
            {
                BuiltinFunctionsCore.Acos,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Acos", Math.Acos))
            },
            {
                BuiltinFunctionsCore.Acot,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Acot)
            },
            {
                BuiltinFunctionsCore.AddColumns,
                StandardErrorHandlingAsync<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Asin", Math.Asin))
            },
            {
                BuiltinFunctionsCore.Atan,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Atan", Math.Atan))
            },
            {
                BuiltinFunctionsCore.Atan2,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Atan2)
            },
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
                StandardErrorHandlingAsync<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: TextToBoolean)
            },
            {
                BuiltinFunctionsCore.Boolean_T,
                StandardErrorHandlingAsync<TableValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: StandardSingleColumnTable<StringValue>(TextToBoolean))
            },
            {
                BuiltinFunctionsCore.BooleanN,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: NumberToBoolean)
            },
            {
                BuiltinFunctionsCore.BooleanN_T,
                StandardErrorHandlingAsync<TableValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: StandardSingleColumnTable<NumberValue>(NumberToBoolean))
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
                StandardErrorHandlingAsync<FormulaValue>(
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
                Coalesce 
            },
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
                StandardErrorHandlingAsync(
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
                StandardErrorHandlingAsync(
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
                BuiltinFunctionsCore.Cos,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Cos", Math.Cos))
            },
            {
                BuiltinFunctionsCore.Cot,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Cot)
            },
            {
                BuiltinFunctionsCore.Count,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: ExactSequence(
                        PositiveNumericNumberChecker,
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
                BuiltinFunctionsCore.DateTime,
                StandardErrorHandling<NumberValue>(
                    expandArguments: InsertDefaultValues(outputArgsCount: 7, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 0)),
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: DateTimeFunction)
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
                BuiltinFunctionsCore.DateValue_UO,
                StandardErrorHandling<UntypedObjectValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Degrees", x => x * 180.0 / Math.PI))
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
                BuiltinFunctionsCore.Error,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Exp)
            },
            {
                BuiltinFunctionsCore.Filter,
                StandardErrorHandlingAsync<FormulaValue>(
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
                        StrictArgumentPositiveNumberChecker),
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Find)
            },
            {
                BuiltinFunctionsCore.FindT,
                StandardErrorHandlingAsync<FormulaValue>(
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
                                    StrictArgumentPositiveNumberChecker),
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<UntypedObjectValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: Guid_UO)
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
                // Implementation 100% shared with IsBlank() for the interpreter
                BuiltinFunctionsCore.IsBlankOptionSetValue,
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
                NoErrorHandling(IsError)
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
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
                BuiltinFunctionsCore.Index,
                StandardErrorHandling<FormulaValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithEmptyString,
                    checkRuntimeTypes: ExactValueType<StringValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Len)
            },
            {
                BuiltinFunctionsCore.LenT,
                StandardErrorHandlingAsync(
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
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: StrictNumericPositiveNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Ln)
            },
            {
                BuiltinFunctionsCore.Log,
                StandardErrorHandling<NumberValue>(
                    expandArguments: InsertDefaultValues(outputArgsCount: 2, fillWith: new NumberValue(IRContext.NotInSource(FormulaType.Number), 10)),
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: StrictNumericPositiveNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Log)
            },
            {
                BuiltinFunctionsCore.LookUp,
                StandardErrorHandlingAsync<FormulaValue>(
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
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Max)
            },
            {
                BuiltinFunctionsCore.MaxT,
                StandardErrorHandlingAsync<FormulaValue>(
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
                    checkRuntimeTypes: DeferRuntimeTypeChecking,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Min)
            },
            {
                BuiltinFunctionsCore.MinT,
                StandardErrorHandlingAsync<FormulaValue>(
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
                NoErrorHandling(Now)
            },
            { 
                BuiltinFunctionsCore.Or, 
                Or 
            },
            {
                BuiltinFunctionsCore.ParseJSON,
                StandardErrorHandling<StringValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Power)
            },
            {
                BuiltinFunctionsCore.Radians,
                StandardErrorHandling<NumberValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Radians", x => x * Math.PI / 180.0))
            },
            {
                BuiltinFunctionsCore.Rand,
                NoErrorHandling(Rand)
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
                    checkRuntimeValues: PositiveNumericNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
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
                BuiltinFunctionsCore.Shuffle,
                StandardErrorHandling<TableValue>(
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Sin", Math.Sin))
            },
            {
                BuiltinFunctionsCore.Sort,
                StandardErrorHandlingAsync<FormulaValue>(
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
                BuiltinFunctionsCore.StdevP,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Stdev)
            },
            {
                BuiltinFunctionsCore.StdevPT,
                StandardErrorHandlingAsync<FormulaValue>(
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
                StandardErrorHandlingAsync<FormulaValue>(
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
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: PositiveNumericNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Sqrt)
            },
            {
                BuiltinFunctionsCore.SqrtT,
                StandardErrorHandlingAsync<TableValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                    checkRuntimeValues: DeferRuntimeValueChecking,
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                    targetFunction: StandardSingleColumnTable<NumberValue>(Sqrt))
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
                    checkRuntimeValues: StrictArgumentPositiveNumberChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Substitute)
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
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: ReplaceBlankWithZero,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: SingleArgTrig("Tan", Math.Tan))
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
                    returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
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
                BuiltinFunctionsCore.TimeValue_UO,
                StandardErrorHandling<UntypedObjectValue>(
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
                BuiltinFunctionsCore.VarP,
                StandardErrorHandling<FormulaValue>(
                    expandArguments: NoArgExpansion,
                    replaceBlankValues: DoNotReplaceBlank,
                    checkRuntimeTypes: ExactValueTypeOrBlank<NumberValue>,
                    checkRuntimeValues: FiniteChecker,
                    returnBehavior: ReturnBehavior.AlwaysEvaluateAndReturnResult,
                    targetFunction: Var)
            },
            {
                BuiltinFunctionsCore.VarPT,
                StandardErrorHandlingAsync<FormulaValue>(
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

        public static IEnumerable<DValue<RecordValue>> StandardTableNodeRecords(IRContext irContext, FormulaValue[] args, bool forceSingleColumn)
        {            
            var tableType = (TableType)irContext.ResultType;
            var columnName = tableType.SingleColumnFieldName;
            var recordType = tableType.ToRecord();
            return args.Select(arg =>
            {
                if (!forceSingleColumn && arg is RecordValue record)
                {
                    return DValue<RecordValue>.Of(record);
                }

                // Handle the single-column-table case. 
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

            return await arg1.EvalAsync(runner, new EvalVisitorContext(childContext, context.StackDepthCounter));
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
                            IRContext.NotInSource(new RecordType(ErrorType.ReifiedError())),
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

                    var ifErrorScopeParamType = new RecordType(DType.CreateRecord(
                        new[]
                        {
                            new TypedName(ErrorType.ReifiedError(), new DName("FirstError")),
                            new TypedName(ErrorType.ReifiedErrorTable(), new DName("AllErrors")),
                        }));
                    var childContext = context.SymbolContext.WithScopeValues(
                        new InMemoryRecordValue(
                            IRContext.NotInSource(ifErrorScopeParamType),
                            scopeVariables));
                    return (await runner.EvalArgAsync<ValidFormulaValue>(errorHandlingBranch, new EvalVisitorContext(childContext, context.StackDepthCounter), errorHandlingBranch.IRContext)).ToFormulaValue();
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
                else
                {
                    childContext = context.SymbolContext.WithScopeValues(RecordValue.Empty());
                }

                // Filter evals to a boolean 
                var result = filter.EvalAsync(runner, new EvalVisitorContext(childContext, context.StackDepthCounter)).AsTask();

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
