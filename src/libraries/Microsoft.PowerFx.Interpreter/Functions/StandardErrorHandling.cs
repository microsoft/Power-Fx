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
        /// <returns></returns>
        private static AsyncFunctionPtr StandardErrorHandlingAsync<T>(
                string functionName,
                Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> expandArguments,
                Func<IRContext, int, FormulaValue> replaceBlankValues,
                Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeTypes,
                Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeValues,
                ReturnBehavior returnBehavior,
                Func<EvalVisitor, EvalVisitorContext, IRContext, T[], ValueTask<FormulaValue>> targetFunction)
            where T : FormulaValue
        {
            return async (runner, context, irContext, args) =>
            {
                var nonFiniteArgError = FiniteArgumentCheck(functionName, irContext, args);
                if (nonFiniteArgError != null)
                {
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

        /// <summary>
        /// Wraps a scalar function into its tabular overload.
        /// </summary>
        /// <param name="functionName">Function name.</param>
        /// <param name="targetFunction">Target function to execute.</param>
        /// <param name="replaceBlankValues">Only supply this if its scalar function has <see cref="NoOpAlreadyHandledByIR(IRContext, int)"/>, meaning scalar has handled this via IR.</param>
        /// <returns></returns>
        private static AsyncFunctionPtr StandardErrorHandlingTabularOverload<TScalar>(
            string functionName, 
            AsyncFunctionPtr targetFunction,
            Func<IRContext, int, FormulaValue> replaceBlankValues)
            where TScalar : FormulaValue => StandardErrorHandlingAsync<TableValue>(
                functionName: functionName,
                expandArguments: NoArgExpansion,
                replaceBlankValues: DoNotReplaceBlank,
                checkRuntimeTypes: ExactValueTypeOrBlank<TableValue>,
                checkRuntimeValues: DeferRuntimeValueChecking,
                returnBehavior: ReturnBehavior.ReturnBlankIfAnyArgIsBlank,
                targetFunction: StandardSingleColumnTable<TScalar>(targetFunction, replaceBlankValues));

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

        /// <summary>
        /// Wrapper for single column table argument functions.
        /// </summary>
        /// <param name="targetFunction">Target function to execute.</param>
        /// <param name="replaceBlankValues">Only supply this if its scalar function has <see cref="NoOpAlreadyHandledByIR(IRContext, int)"/>, meaning scalar has handled this via IR.</param>
        /// <returns></returns>
        public static Func<EvalVisitor, EvalVisitorContext, IRContext, TableValue[], ValueTask<FormulaValue>> StandardSingleColumnTable<T>(
            AsyncFunctionPtr targetFunction,
            Func<IRContext, int, FormulaValue> replaceBlankValues)
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

                        if (value is BlankValue)
                        {
                            // since this is for single column table arg, index is passed as 0
                            value = replaceBlankValues(value.IRContext, 0);
                        }

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
            return StandardSingleColumnTable<T>(async (runner, context, irContext, args) => targetFunction(irContext, args.OfType<T>().ToArray()), DoNotReplaceBlank);
        }

        private static (int minTableSize, int maxTableSize, FormulaValue errorOrBlankTable) AnalyzeTableArguments(FormulaValue[] args, IRContext irContext)
        {
            var maxTableSize = 0;
            var minTableSize = int.MaxValue;

            foreach (var arg in args)
            {
                if (arg is TableValue tv)
                {
                    var tableSize = tv.Rows.Count();
                    maxTableSize = Math.Max(maxTableSize, tableSize);
                    minTableSize = Math.Min(minTableSize, tableSize);
                }
                else if (arg is BlankValue bv && bv.IRContext.ResultType._type.IsTable)
                {
                    return (minTableSize, maxTableSize, new BlankValue(irContext));
                }
                else if (arg is ErrorValue ev && ev.IRContext.ResultType._type.IsTable)
                {
                    return (minTableSize, maxTableSize, ev);
                }
            }

            return (minTableSize, maxTableSize, null);
        }

        /// <summary>
        /// A standard error handling wrapper function that handles functions that can accept one or more table values.
        /// The standard behavior for this type of function is to expand all scalars and tables into a set of tables
        /// with the same size, where that size is the length of the longest table, if any.The result is always a table
        /// where some operation has been performed on the transpose of the input tables.
        ///
        /// For example given the table function F and the operation F' and inputs [a, b] and [c, d], the transpose is [a, c], [b, d]
        /// F([a, b], [c, d]) => [F'([a, c]), F'([b, d])]
        /// As a concrete example, Concatenate(["a", "b"], ["1", "2"]) => ["a1", "b2"].
        /// </summary>
        /// <param name="targetFunction">Target function to execute.</param>
        /// <param name="replaceBlankValues">Only supply this if its scalar function has <see cref="NoOpAlreadyHandledByIR(IRContext, int)"/>, meaning scalar has handled this via IR.</param>
        /// <returns></returns>
        public static Func<EvalVisitor, EvalVisitorContext, IRContext, FormulaValue[], ValueTask<FormulaValue>> MultiSingleColumnTable(
            AsyncFunctionPtr targetFunction,
            Func<IRContext, int, FormulaValue> replaceBlankValues)
        {
            return async (runner, context, irContext, args) =>
            {
                (var minTableSize, var maxTableSize, var errorOrBlankTable) = AnalyzeTableArguments(args, irContext);

                // If one of the arguments is a blank or error table, return it.
                // e.g. Concatenate(If(1<0,["this is a blank table"]), "hello") ==> Blank() or Concatenate(["test"], If(Sqrt(-1)<0,["this is an error table"])) ==> Error
                if (errorOrBlankTable != null)
                {
                    return errorOrBlankTable;
                }

                var resultRows = new List<DValue<RecordValue>>();

                if (maxTableSize == 0)
                {
                    // maxSize == 0 means there are no tables with rows. This can happen if we receive a Filter expression where no rows were return,
                    // or all tables in args are empty. 
                    // Return an empty table (with the correct type).
                    // e.g. Concatenate(Filter([1,2], Value<>Value, "test") => []
                    return new InMemoryTableValue(irContext, resultRows);
                }

                var tableType = (TableType)irContext.ResultType;
                var resultType = tableType.ToRecord();
                var columnNameStr = tableType.SingleColumnFieldName;
                var itemType = resultType.GetFieldType(columnNameStr);

                var tabularArgRows = new DValue<RecordValue>[args.Length][];
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] is TableValue tv)
                    {
                        tabularArgRows[i] = tv.Rows
                            .Take(Math.Min(tv.Count(), minTableSize))
                            .ToArray();
                    }
                }

                for (var i = 0; i < minTableSize; i++)
                {
                    var functionArgs = new FormulaValue[args.Length];
                    ErrorValue errorRow = null;
                    for (var j = 0; j < args.Length; j++)
                    {
                        var arg = args[j];
                        if (arg is TableValue tv)
                        {
                            var argRow = tabularArgRows[j][i];
                            if (argRow.IsError)
                            {
                                errorRow = argRow.Error;
                                break;
                            }
                            else if (argRow.IsBlank)
                            {
                                functionArgs[j] = argRow.Blank;
                            }
                            else
                            {
                                functionArgs[j] = argRow.Value.Fields.First().Value;
                            }
                        }
                        else
                        {
                            functionArgs[j] = arg;
                        }
                    }

                    if (errorRow != null)
                    {
                        resultRows.Add(DValue<RecordValue>.Of(errorRow));
                    }
                    else
                    {
                        var blankValuesReplaced = functionArgs.Select((arg, i) =>
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

                        var rowResult = await targetFunction(runner, context, IRContext.NotInSource(itemType), blankValuesReplaced.ToArray());
                        var namedValue = new NamedValue(columnNameStr, rowResult);
                        var record = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedValue });
                        resultRows.Add(DValue<RecordValue>.Of(record));
                    }    
                }

                if (maxTableSize != minTableSize)
                {
                    // Add error nodes for different table length
                    // e.g. Concatenate(["a"],["1","2"] => ["a1", <error>]
                    var namedErrorValue = new NamedValue(columnNameStr, FormulaValue.NewError(new ExpressionError()
                    {
                        Kind = ErrorKind.NotApplicable,
                        Severity = ErrorSeverity.Critical,
                        Message = "Not Applicable"
                    }));
                    var errorRecord = new InMemoryRecordValue(IRContext.NotInSource(resultType), new List<NamedValue>() { namedErrorValue });
                    var errorRowCount = maxTableSize - minTableSize;
                    resultRows.AddRange(Enumerable.Repeat(DValue<RecordValue>.Of(errorRecord), errorRowCount));
                }

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

        private static FormulaValue DropColumnsTypeChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (index == 0)
            {
                return ExactValueTypeOrBlank<TableValue>(irContext, index, arg);
            }

            return ExactValueTypeOrBlank<StringValue>(irContext, index, arg);
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

        private static FormulaValue NoOpAlreadyHandledByIR(IRContext irContext, int index)
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
