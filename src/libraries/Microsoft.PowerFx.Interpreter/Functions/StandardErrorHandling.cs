// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        private enum ReturnBehavior
        {
            AlwaysEvaluateAndReturnResult,
            ReturnBlankIfAnyArgIsBlank,
            ReturnEmptyStringIfAnyArgIsBlank,
            ReturnFalseIfAnyArgIsBlank
        }

        private static bool IsInvalidDouble(double number)
        {
            return double.IsNaN(number) || double.IsInfinity(number);
        }

        /// <summary>
        /// A pipeline that maps blanks to a value, checks
        /// runtime types, and possibly map values to errors
        /// before filtering errors and possibly returning
        /// an ErrorValue instead of executing.
        /// </summary>
        /// <typeparam name="T">The specific FormulaValue type that the implementation of the builtin expects, for exmaple NumberValue for math functions.</typeparam>
        /// <param name="functionName">The name of the Power Fx function, which is used in a possible error message.</param>
        /// <param name="expandArguments">This stage of the pipeline can be used to expand an argument list if some of the arguments are optional and missing.</param>
        /// <param name="replaceBlankValues">This stage can be used to transform Blank() into something else, for example the number 0.</param>
        /// <param name="checkRuntimeTypes">This stage can be used to check to that all the arguments have type T, or check that all arguments have type T | Blank(), etc.</param>
        /// <param name="checkRuntimeValues">This stage can be used to generate errors if specific values occur in the arguments, for example infinity, NaN, etc.</param>
        /// <param name="returnBehavior">A flag that can be used to activate pre-defined early return behavior, such as returning Blank() if any argument is Blank().</param>
        /// <param name="targetFunction">The implementation of the builtin function.</param>
        /// <param name="isMultiArgTabularOverload">If True returns error table in case of error args.</param>
        /// <returns></returns>
        private static AsyncFunctionPtr StandardErrorHandlingAsync<T>(
                string functionName,
                Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> expandArguments,
                Func<IRContext, int, FormulaValue> replaceBlankValues,
                Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeTypes,
                Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeValues,
                ReturnBehavior returnBehavior,
                Func<EvalVisitor, EvalVisitorContext, IRContext, T[], ValueTask<FormulaValue>> targetFunction,
                bool isMultiArgTabularOverload = false)
            where T : FormulaValue
        {
            return async (runner, context, irContext, args) =>
            {
                var nonFiniteArgError = FiniteArgumentCheck(functionName, irContext, args);
                if (nonFiniteArgError != null)
                {
                    (var maxSize, var minsSize, var emptyTablePresent, _, var errorTable) = AnalyzeTableArguments(args, irContext);

                    // In case Tabular overload has one scalar error arg and another Table arg we want to
                    // return table of error. else If error arg is table then return table error args.
                    if (isMultiArgTabularOverload && maxSize > 0 && errorTable == null)
                    {
                        var tableType = (TableType)irContext.ResultType;
                        var resultType = tableType.ToRecord();
                        var namedValue = new NamedValue(tableType.SingleColumnFieldName, nonFiniteArgError);
                        var record = DValue<RecordValue>.Of(new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue }));
                        var resultRows = Enumerable.Repeat(record, maxSize).ToList();

                        return new InMemoryTableValue(irContext, resultRows);
                    }
                    else if (isMultiArgTabularOverload)
                    {
                        return errorTable;
                    }

                    return nonFiniteArgError;
                }

                var argumentsExpanded = expandArguments(irContext, args);

                var blankValuesReplaced = argumentsExpanded.Select((arg, i) =>
                {
                    if (arg is BlankValue)
                    {
                        return replaceBlankValues(arg.IRContext, i);
                    }
                    else
                    {
                        return arg;
                    }
                });

                var runtimeTypesChecked = blankValuesReplaced.Select((arg, i) => checkRuntimeTypes(irContext, i, arg));

                var runtimeValuesChecked = runtimeTypesChecked.Select((arg, i) =>
                {
                    if (arg is T t)
                    {
                        return checkRuntimeValues(arg.IRContext, i, t);
                    }
                    else
                    {
                        return arg;
                    }
                });

                var errors = runtimeValuesChecked.OfType<ErrorValue>();
                if (errors.Any())
                {
                    return ErrorValue.Combine(irContext, errors);
                }

                var anyValueBlank = runtimeValuesChecked.Any(arg => arg is BlankValue || (arg is UntypedObjectValue uov && uov.Impl.Type == FormulaType.Blank));

                switch (returnBehavior)
                {
                    case ReturnBehavior.ReturnBlankIfAnyArgIsBlank:
                        if (anyValueBlank)
                        {
                            return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                        }

                        break;
                    case ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank:
                        if (anyValueBlank)
                        {
                            return new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty);
                        }

                        break;
                    case ReturnBehavior.ReturnFalseIfAnyArgIsBlank:
                        if (anyValueBlank)
                        {
                            return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false);
                        }

                        break;
                    case ReturnBehavior.AlwaysEvaluateAndReturnResult:
                        break;
                }

                var result = await targetFunction(runner, context, irContext, runtimeValuesChecked.Select(arg => arg as T).ToArray());
                var finiteError = FiniteResultCheck(functionName, irContext, result);
                return finiteError ?? result;
            };
        }

        // A wrapper that allows standard error handling to apply to
        // sync functions which accept the simpler parameter list of
        // an array of arguments, ignoring context, runner etc.
        private static AsyncFunctionPtr StandardErrorHandling<T>(
            string functionName,
            Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> expandArguments,
            Func<IRContext, int, FormulaValue> replaceBlankValues,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeTypes,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeValues,
            ReturnBehavior returnBehavior,
            Func<IRContext, T[], FormulaValue> targetFunction)
            where T : FormulaValue
        {
            return StandardErrorHandlingAsync<T>(functionName, expandArguments, replaceBlankValues, checkRuntimeTypes, checkRuntimeValues, returnBehavior, (runner, context, irContext, args) =>
            {
                var result = targetFunction(irContext, args);
                return new ValueTask<FormulaValue>(result);
            });
        }

        // A wrapper that allows standard error handling to apply to
        // sync functions which accept the simpler parameter list of
        // an array of arguments, ignoring context, runner etc.
        private static AsyncFunctionPtr StandardErrorHandling<T>(
            string functionName,
            Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> expandArguments,
            Func<IRContext, int, FormulaValue> replaceBlankValues,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeTypes,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeValues,
            ReturnBehavior returnBehavior,
            Func<IServiceProvider, IRContext, T[], FormulaValue> targetFunction)
            where T : FormulaValue
        {
            return StandardErrorHandlingAsync<T>(functionName, expandArguments, replaceBlankValues, checkRuntimeTypes, checkRuntimeValues, returnBehavior, (runner, context, irContext, args) =>
            {
                var result = targetFunction(runner.FunctionServices, irContext, args);
                return new ValueTask<FormulaValue>(result);
            });
        }

        // A wrapper that allows standard error handling to apply to
        // sync functions with the full parameter list
        private static AsyncFunctionPtr StandardErrorHandling<T>(
            string functionName,
            Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> expandArguments,
            Func<IRContext, int, FormulaValue> replaceBlankValues,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeTypes,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeValues,
            ReturnBehavior returnBehavior,
            Func<EvalVisitor, EvalVisitorContext, IRContext, T[], FormulaValue> targetFunction)
            where T : FormulaValue
        {
            return StandardErrorHandlingAsync<T>(functionName, expandArguments, replaceBlankValues, checkRuntimeTypes, checkRuntimeValues, returnBehavior, (runner, context, irContext, args) =>
            {
                var result = targetFunction(runner, context, irContext, args);
                return new ValueTask<FormulaValue>(result);
            });
        }

        // Wraps a scalar function into its tabular overload
        private static AsyncFunctionPtr StandardErrorHandlingTabularOverload<TScalar>(string functionName, AsyncFunctionPtr targetFunction)
            where TScalar : FormulaValue => StandardErrorHandlingAsync<TableValue>(
                functionName: functionName,
                expandArguments: NoArgExpansion,
                replaceBlankValues: DoNotReplaceBlank,
                checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                checkRuntimeValues: DeferRuntimeValueChecking,
                returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                targetFunction: StandardSingleColumnTable<TScalar>(targetFunction));

        // A wrapper for a function with no error handling behavior whatsoever.
        private static AsyncFunctionPtr NoErrorHandling(
            Func<IRContext, FormulaValue[], FormulaValue> targetFunction)
        {
            return (_, _, irContext, args) =>
            {
                var result = targetFunction(irContext, args);
                return new ValueTask<FormulaValue>(result);
            };
        }

        // A wrapper for a function with no error handling behavior whatsoever.
        private static AsyncFunctionPtr NoErrorHandling(
            Func<EvalVisitor, EvalVisitorContext, IRContext, FormulaValue[], ValueTask<FormulaValue>> targetFunction)
        {
            return (visitor, context, irContext, args) =>
            {
                return targetFunction(visitor, context, irContext, args);
            };
        }

        #region Single Column Table Functions
        public static Func<EvalVisitor, EvalVisitorContext, IRContext, TableValue[], ValueTask<FormulaValue>> StandardSingleColumnTable<T>(AsyncFunctionPtr targetFunction)
            where T : FormulaValue
        {
            return async (runner, context, irContext, args) =>
            {
                var inputTableType = (TableType)args[0].Type;
                var inputColumnNameStr = inputTableType.SingleColumnFieldName;
                var inputItemType = inputTableType.GetFieldType(inputColumnNameStr);

                var outputTableType = (TableType)irContext.ResultType;
                var resultType = outputTableType.ToRecord();
                var outputColumnNameStr = outputTableType.SingleColumnFieldName;
                var outputItemType = outputTableType.GetFieldType(outputColumnNameStr);
                var resultRows = new List<DValue<RecordValue>>();
                foreach (var row in args[0].Rows)
                {
                    if (row.IsValue)
                    {
                        var value = row.Value.GetField(inputColumnNameStr);
                        NamedValue namedValue;
                        namedValue = value switch
                        {
                            T t => new NamedValue(outputColumnNameStr, await targetFunction(runner, context, IRContext.NotInSource(outputItemType), new T[] { t })),
                            BlankValue bv => new NamedValue(outputColumnNameStr, await targetFunction(runner, context, IRContext.NotInSource(outputItemType), new FormulaValue[] { bv })),
                            ErrorValue ev => new NamedValue(outputColumnNameStr, ev),
                            _ => new NamedValue(outputColumnNameStr, CommonErrors.RuntimeTypeMismatch(IRContext.NotInSource(inputItemType)))
                        };
                        var record = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue });
                        resultRows.Add(DValue<RecordValue>.Of(record));
                    }
                    else if (row.IsBlank)
                    {
                        resultRows.Add(DValue<RecordValue>.Of(row.Blank));
                    }
                    else
                    {
                        resultRows.Add(DValue<RecordValue>.Of(row.Error));
                    }
                }

                var result = new InMemoryTableValue(irContext, resultRows);
                return result;
            };
        }

        public static Func<EvalVisitor, EvalVisitorContext, IRContext, TableValue[], ValueTask<FormulaValue>> StandardSingleColumnTable<T>(Func<IRContext, T[], FormulaValue> targetFunction)
            where T : FormulaValue
        {
            return StandardSingleColumnTable<T>(async (runner, context, irContext, args) => targetFunction(irContext, args.OfType<T>().ToArray()));
        }

        private static (int maxTableSize, int minTableSize, bool emptyTablePresent, bool blankTablePresent, ErrorValue errorTable) AnalyzeTableArguments(FormulaValue[] args, IRContext irContext)
        {
            var maxTableSize = 0;
            var emptyTablePresent = false;
            var minTableSize = int.MaxValue;
            var blankTablePresent = false;

            List<ErrorValue> errors = null;

            foreach (var arg in args)
            {
                if (arg is TableValue tv)
                {
                    var tableSize = tv.Rows.Count();
                    maxTableSize = Math.Max(maxTableSize, tableSize);

                    // Empty tables are not considered for count.
                    minTableSize = tableSize != 0 ? Math.Min(minTableSize, tableSize) : minTableSize;

                    emptyTablePresent |= tableSize == 0;
                }
                else if (arg is BlankValue bv && bv.IRContext.ResultType._type.IsTable)
                {
                    blankTablePresent = true;
                }
                else if (arg is ErrorValue ev && ev.IRContext.ResultType._type.IsTable)
                {
                    if (errors == null)
                    {
                        errors = new List<ErrorValue>();
                    }

                    errors.Add(ev);
                }
            }

            var errorTable = errors != null ? ErrorValue.Combine(irContext, errors) : null;
            return (maxTableSize, minTableSize, emptyTablePresent, blankTablePresent, errorTable);
        }

        private class ExpandToSizeResult
        {
            public readonly string Name;
            public readonly IEnumerable<DValue<RecordValue>> Rows;

            public ExpandToSizeResult(string name, IEnumerable<DValue<RecordValue>> rows)
            {
                Name = name;
                Rows = rows;
            }
        }

        private static ExpandToSizeResult ShrinkToSize(FormulaValue arg, int size)
        {
            if (arg is TableValue tv)
            {
                var tvType = (TableType)tv.Type;
                var name = tvType.SingleColumnFieldName;

                var count = tv.Rows.Count();
                if (count > size)
                {
                    return new ExpandToSizeResult(name, tv.Rows.Take(size));
                }
                else if (count == 0)
                {
                    var inputRecordType = tvType.ToRecord();
                    var inputRecordNamedValue = new NamedValue(name, new BlankValue(IRContext.NotInSource(FormulaType.Blank)));
                    var inputRecord = new InMemoryRecordValue(IRContext.NotInSource(inputRecordType), new List<NamedValue>() { inputRecordNamedValue });
                    var inputDValue = DValue<RecordValue>.Of(inputRecord);

                    var repeated = Enumerable.Repeat(inputDValue, size - count);
                    var rows = tv.Rows.Concat(repeated);
                    return new ExpandToSizeResult(name, rows);
                }
                else
                {
                    return new ExpandToSizeResult(name, tv.Rows);
                }
            }
            else
            {
                var name = BuiltinFunction.ColumnName_ValueStr;
                var inputRecordType = RecordType.Empty().Add(name, arg.Type);
                var inputRecordNamedValue = new NamedValue(name, arg);
                var inputRecord = new InMemoryRecordValue(IRContext.NotInSource(inputRecordType), new List<NamedValue>() { inputRecordNamedValue });
                var inputDValue = DValue<RecordValue>.Of(inputRecord);
                var rows = Enumerable.Repeat(inputDValue, size);
                return new ExpandToSizeResult(name, rows);
            }
        }

        // Transpose a matrix (list of lists) so that the rows become columns and the columns become rows
        // The column length is uniform and known
        private static List<List<T>> Transpose<T>(List<List<T>> columns, int columnSize)
        {
            var rows = new List<List<T>>();

            for (var i = 0; i < columnSize; i++)
            {
                rows.Add(columns.Select(column => column[i]).ToList());
            }

            return rows;
        }

        /*
         * A standard error handling wrapper function that handles functions that can accept one or more table values.
         * The standard behavior for this type of function is to expand all scalars and tables into a set of tables
         * with the same size, where that size is the length of the longest table, if any. The result is always a table
         * where some operation has been performed on the transpose of the input tables.
         * 
         * For example given the table function F and the operation F' and inputs [a, b] and [c, d], the transpose is [a, c], [b, d]
         * F([a, b], [c, d]) => [F'([a, c]), F'([b, d])]
         * As a concrete example, Concatenate(["a", "b"], ["1", "2"]) => ["a1", "b2"]
        */
        public static Func<EvalVisitor, EvalVisitorContext, IRContext, FormulaValue[], ValueTask<FormulaValue>> MultiSingleColumnTable(
            AsyncFunctionPtr targetFunction,
            bool transposeEmptyTable)
        {
            return async (runner, context, irContext, args) =>
            {
                var resultRows = new List<DValue<RecordValue>>();

                (var maxSize, var minSize, var emptyTablePresent, var blankTablePresent, _) = AnalyzeTableArguments(args, irContext);

                // If one arg is blank table and among all other args, return blank.
                // e.g. Concatenate(Blank(), []), Concatenate(Blank(), "test"), Concatenate(Blank(), ["test"], []) => Blank()
                if (blankTablePresent)
                {
                    return FormulaValue.NewBlank();
                }

                if (maxSize == 0 || (emptyTablePresent && !transposeEmptyTable))
                {
                    // maxSize == 0 means there are no tables with rows. This can happen when we expect a Table at compile time but we recieve Blank() at runtime,
                    // or all tables in args are empty. Additionally, among non-empty tables (in which case maxSize > 0) there may be an empty table,
                    // which also means empty result if transposeEmptyTable == false.
                    // Return an empty table (with the correct type).
                    // e.g. Concatenate([], "test") => []
                    return new InMemoryTableValue(irContext, resultRows);
                }

                var allResults = args.Select(arg => ShrinkToSize(arg, minSize));

                var tableType = (TableType)irContext.ResultType;
                var resultType = tableType.ToRecord();
                var columnNameStr = tableType.SingleColumnFieldName;
                var itemType = resultType.GetFieldType(columnNameStr);

                var transposed = Transpose(allResults.Select(result => result.Rows.ToList()).ToList(), minSize);
                var names = allResults.Select(result => result.Name).ToList();
                foreach (var list in transposed)
                {
                    var errorRow = list.FirstOrDefault(dv => dv.IsError);
                    if (errorRow != null)
                    {
                        resultRows.Add(DValue<RecordValue>.Of(errorRow.Error));
                        continue;
                    }

                    var targetArgs = list.Select((dv, i) => dv.IsValue ? dv.Value.GetField(names[i]) : dv.ToFormulaValue()).ToArray();
                    var namedValue = new NamedValue(tableType.SingleColumnFieldName, await targetFunction(runner, context, IRContext.NotInSource(itemType), targetArgs));
                    var record = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue });
                    resultRows.Add(DValue<RecordValue>.Of(record));
                }

                // Add error nodes for different table length
                // e.g. Concatenate(["a"],["1","2"] => ["a1", <error>]
                var namedErrorValue = new NamedValue(columnNameStr, FormulaValue.NewError(new ExpressionError()
                {
                    Kind = ErrorKind.NotApplicable,
                    Severity = ErrorSeverity.Critical,
                    Message = "Not Applicable"
                }));
                var errorRecord = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedErrorValue });
                var errorRowCount = maxSize - minSize;
                resultRows.AddRange(Enumerable.Repeat(DValue<RecordValue>.Of(errorRecord), errorRowCount));
                return new InMemoryTableValue(irContext, resultRows);
            };
        }
        #endregion

        #region Common Arg Expansion Pipeline Stages
        private static Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> InsertDefaultValues(int outputArgsCount, FormulaValue fillWith)
        {
            return (irContext, args) =>
            {
                var res = new List<FormulaValue>(args);
                while (res.Count < outputArgsCount)
                {
                    res.Add(fillWith);
                }

                return res.ToArray();
            };
        }

        private static IEnumerable<FormulaValue> MidFunctionExpandArgs(IRContext irContext, IEnumerable<FormulaValue> args)
        {
            var res = new List<FormulaValue>(args);
            while (res.Count < 3)
            {
                // The third argument to Mid can only ever be used if the first argument is a string
                if (args.First() is StringValue stringValue)
                {
                    var count = new NumberValue(IRContext.NotInSource(FormulaType.Number), stringValue.Value.Length);
                    res.Add(count);
                }
                else
                {
                    break;
                }
            }

            return res.ToArray();
        }
        #endregion

        #region Common Blank Replacement Pipeline Stages
        private static FormulaValue ReplaceBlankWithZero(IRContext irContext, int index)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), 0.0);
        }

        private static FormulaValue ReplaceBlankWithEmptyString(IRContext irContext, int index)
        {
            return new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty);
        }

        private static Func<IRContext, int, FormulaValue> ReplaceBlankWith(params FormulaValue[] values)
        {
            return (irContext, index) =>
            {
                return values[index];
            };
        }

        private static Func<IRContext, int, FormulaValue> ReplaceBlankWithZeroForSpecificIndices(params int[] indices)
        {
            var indicesToReplace = new HashSet<int>(indices);
            return (irContext, index) =>
            {
                if (indicesToReplace.Contains(index))
                {
                    return new NumberValue(IRContext.NotInSource(FormulaType.Number), 0.0);
                }

                return new BlankValue(irContext);
            };
        }
        #endregion

        #region Common Type Checking Pipeline Stages
        private static FormulaValue ExactValueType<T>(IRContext irContext, int index, FormulaValue arg)
            where T : FormulaValue
        {
            if (arg is T || arg is ErrorValue)
            {
                return arg;
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        private static FormulaValue ExactValueTypeOrBlank<T>(IRContext irContext, int index, FormulaValue arg)
            where T : FormulaValue
        {
            if (arg is BlankValue)
            {
                return arg;
            }
            else
            {
                return ExactValueType<T>(irContext, index, arg);
            }
        }

        private static FormulaValue ExactValueTypeOrTableOrBlank<T>(IRContext irContext, int index, FormulaValue arg)
            where T : FormulaValue
        {
            if (arg is TableValue)
            {
                return arg;
            }
            else
            {
                return ExactValueTypeOrBlank<T>(irContext, index, arg);
            }
        }

        private static Func<IRContext, int, FormulaValue, FormulaValue> ExactSequence(params Func<IRContext, int, FormulaValue, FormulaValue>[] runtimeChecks)
        {
            return (irContext, index, arg) =>
            {
                return runtimeChecks[index](irContext, index, arg);
            };
        }

        private static FormulaValue AddColumnsTypeChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (index == 0)
            {
                return ExactValueTypeOrBlank<TableValue>(irContext, index, arg);
            }
            else if (index % 2 == 1)
            {
                return ExactValueTypeOrBlank<StringValue>(irContext, index, arg);
            }
            else
            {
                return ExactValueTypeOrBlank<LambdaFormulaValue>(irContext, index, arg);
            }
        }

        private static FormulaValue DateOrDateTime(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is DateValue || arg is DateTimeValue || arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        private static FormulaValue TimeOrDateTime(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is TimeValue || arg is DateTimeValue || arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        private static FormulaValue DateOrTimeOrDateTime(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is DateValue || arg is TimeValue || arg is DateTimeValue || arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        private static FormulaValue DateNumberTimeOrDateTime(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is DateValue || arg is DateTimeValue || arg is TimeValue || arg is NumberValue || arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }
        #endregion

        #region Common Runtime Value Checking Pipeline Stages
        private static ErrorValue FiniteArgumentCheck(string functionName, IRContext irContext, FormulaValue[] args)
        {
            List<ErrorValue> errors = null;
            foreach (var arg in args)
            {
                if (arg is ErrorValue ev)
                {
                    if (errors == null)
                    {
                        errors = new List<ErrorValue>();
                    }

                    errors.Add(ev);
                }

                if (arg is NumberValue nv && IsInvalidDouble(nv.Value))
                {
                    if (errors == null)
                    {
                        errors = new List<ErrorValue>();
                    }

                    errors.Add(new ErrorValue(irContext, new ExpressionError()
                    {
                        Message = $"Arguments to the {functionName} function must be finite.",
                        Span = irContext.SourceContext,
                        Kind = ErrorKind.Numeric
                    }));
                }
            }

            return errors != null ? ErrorValue.Combine(irContext, errors) : null;
        }

        private static ErrorValue FiniteResultCheck(string functionName, IRContext irContext, FormulaValue value)
        {
            if (value is ErrorValue ev)
            {
                return ev;
            }

            if (value is NumberValue nv && IsInvalidDouble(nv.Value))
            {
                return new ErrorValue(irContext, new ExpressionError()
                {
                    Message = $"The function {functionName} returned a non-finite number.",
                    Span = irContext.SourceContext,
                    Kind = ErrorKind.Numeric
                });
            }

            return null;
        }
        #endregion

        #region Common Runtime Value Checking Pipeline Stages
        private static FormulaValue PositiveNumericNumberChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is NumberValue numberArg)
            {
                var number = numberArg.Value;
                if (number < 0)
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue StrictArgumentPositiveNumberChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is NumberValue numberArg)
            {
                var number = numberArg.Value;
                if (number <= 0)
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue StrictNumericPositiveNumberChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is NumberValue numberArg)
            {
                var number = numberArg.Value;
                if (number <= 0)
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }
            }

            return arg;
        }
        #endregion

        #region No Op Pipeline Stages
        private static IEnumerable<FormulaValue> NoArgExpansion(IRContext irContext, IEnumerable<FormulaValue> args)
        {
            return args;
        }

        private static FormulaValue DoNotReplaceBlank(IRContext irContext, int index)
        {
            return new BlankValue(irContext);
        }

        // This function should be used when type checking is too unique and needs to be located
        // within the body of the builtin function itself
        private static FormulaValue DeferRuntimeTypeChecking(IRContext irContext, int index, FormulaValue arg)
        {
            return arg;
        }

        private static FormulaValue DeferRuntimeValueChecking<T>(IRContext irContext, int index, T arg)
            where T : FormulaValue
        {
            return arg;
        }
        #endregion
    }
}
